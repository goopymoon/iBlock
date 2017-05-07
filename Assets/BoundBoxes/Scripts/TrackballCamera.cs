using UnityEngine;
using System.Collections;

public class TrackballCamera : CameraWithTouchFilter
{
    // Zoom
    const float pinchRatio = 2f;
    const float pinchMinDistance = 0f;
    const float zoomDampening = 0.2f;
    // Rotation
    const float minPitchInDegree = -80.0f;
    const float aboveMinY = 0.8f;
    // New pivot
    const float pivotPickInterval = 0.7f;

    // Ground
    public GameObject terrainMesh;

    // Zoom
    public float maxDistance { get; set; }
    public float minDistance { get; set; }

    // Zoom
    private Vector2? lastTouchPosition = null;
    private float curDistance;
    private float destDistance;
    // Rotation
    private float virtualTrackballDistance = 0.25f;
    private Transform pivot;
    // New pivot
    private GameObject pickedObj = null;
    private float pickedTime;
    private bool movePivot = false;

    void Start()
    {
        // Make reference pivot object
        GameObject go = new GameObject("Cam Target");
        go.transform.position = Vector3.zero;
        pivot = go.transform;

        //be sure to grab the current rotations as starting points.
        transform.LookAt(pivot);

        float distance = Vector3.Distance(transform.position, pivot.position);
        curDistance = distance;
        destDistance = distance;

        maxDistance = 12.0f;
        minDistance = 1.0f;
    }

    public IEnumerator Rotate(Touch touch1)
    {
        // Wait and draw rotation indicator such as circle
        yield return null;

        if (lastTouchPosition.HasValue)
        {
            // Rotate
            var startPos = (this.transform.position - pivot.position).normalized * curDistance;
            var rotation = FigureOutAxisAngleRotation(pivot.position, startPos, lastTouchPosition.Value, touch1.position);
            var position = rotation * startPos + pivot.position;

            this.transform.position = position;
            this.transform.LookAt(pivot.position);
        }

        // Update last touch position
        lastTouchPosition = touch1.position;
    }

    void ZoomInOut(Touch touch1, Touch touch2)
    {
        // check the delta distance between them ...
        float pinchDistance = Vector2.Distance(touch1.position, touch2.position);
        float prevDistance = Vector2.Distance(touch1.position - touch1.deltaPosition,
            touch2.position - touch2.deltaPosition);

        float pinchDistanceDelta = pinchDistance - prevDistance;

        // if it's greater than a minimum threshold, it's a pinch!
        if (Mathf.Abs(pinchDistanceDelta) > pinchMinDistance)
            pinchDistanceDelta *= (pinchRatio / Screen.width);
        else
            pinchDistanceDelta = 0;

        if (pinchDistanceDelta != 0.0f)
        {
            var delta = pinchDistanceDelta * Mathf.Abs(destDistance);
            destDistance = Mathf.Clamp(destDistance - delta, minDistance, maxDistance);
        }

        if (Mathf.Abs(curDistance - destDistance) > zoomDampening)
        {
            curDistance = Mathf.Lerp(curDistance, destDistance, zoomDampening);

            var vecPos = (this.transform.position - pivot.position).normalized * curDistance;
            this.transform.position = vecPos + pivot.position;
        }
    }

    void ChangePivot(GameObject obj)
    {
        float curTime = Time.time;

        if (!movePivot)
        {
            if (pickedObj == null)
            {
                Debug.Log(string.Format("try picking: {0}", obj.ToString()));

                pickedObj = obj;
                pickedTime = curTime;
                return;
            }

            if (pickedObj != obj || (curTime - pickedTime > pivotPickInterval))
            {
                Debug.Log(string.Format("retry picking: {0}: {1}", obj.ToString(), curTime - pickedTime));

                pickedObj = obj;
                pickedTime = curTime;
                return;
            }

            Debug.Log(string.Format("confirm picking: {0}: {1}", obj.ToString(), curTime - pickedTime));
            movePivot = true;
        }
        else
        {
            Debug.Log(string.Format("Reset picking: {0}", obj.ToString()));

            pickedObj = obj;
            pickedTime = curTime;
            movePivot = false;
        }
    }

    IEnumerator RefreshPivot()
    {
        float targetDist = Vector3.Distance(pickedObj.transform.position, pivot.position);

        while (pickedObj && (targetDist > zoomDampening))
        {
            float localDist = targetDist < zoomDampening ? targetDist : zoomDampening;
            var vecPos = (pickedObj.transform.position - pivot.position).normalized * localDist;

            pivot.position = pivot.position + vecPos;

            transform.LookAt(pivot.position);

            targetDist = Vector3.Distance(pickedObj.transform.position, pivot.position);
            yield return null;
        }

        pickedObj = null;
        movePivot = false;
    }

    GameObject GetPickedBrick(Touch touch)
    {
        RaycastHit[] hits = Physics.RaycastAll(Camera.main.ScreenPointToRay(touch.position));

        GameObject obj = null;

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.gameObject == terrainMesh)
                continue;

            obj = hit.transform.gameObject;
        }

        return obj;
    }

    Quaternion FigureOutAxisAngleRotation(Vector3 pivotPos, Vector3 vecPos, Vector3 lastTouch, Vector3 curTouch)
    {
        if (lastTouch == curTouch)
            return Quaternion.identity;

        float maxPitchInDegree = Mathf.Rad2Deg * Mathf.Asin((pivotPos.y - aboveMinY) / curDistance);

        Vector3 near = new Vector3(0, 0, Camera.main.nearClipPlane);
        Vector3 p1 = Camera.main.ScreenToWorldPoint(curTouch + near);
        Vector3 p2 = Camera.main.ScreenToWorldPoint(lastTouch + near);

        Vector3 axisOfRotation = Vector3.Cross(p1, p2);
        float twist = (p2 - p1).magnitude / (2.0f * virtualTrackballDistance);
        float phi = Mathf.Rad2Deg * Mathf.Asin(Mathf.Clamp(twist, -1.0f, 1.0f));
        Quaternion rotation = Quaternion.AngleAxis(phi, axisOfRotation);

        Vector3 candPos = rotation * vecPos + pivotPos;

        phi += AdjustPitch(candPos, pivotPos, minPitchInDegree, maxPitchInDegree);

        return Quaternion.AngleAxis(phi, axisOfRotation);
    }

    float AdjustPitch(Vector3 candPos, Vector3 pivotPos, float minAngle, float maxAngle)
    {
        Plane basePlaine = new Plane(Vector3.up, candPos);

        float height = basePlaine.GetDistanceToPoint(pivotPos);
        float angle = Mathf.Rad2Deg * Mathf.Asin(height / curDistance);
        float delta = (angle < minAngle) ? (angle - minAngle) : (angle > maxAngle) ? (maxAngle - angle) : 0;

        return delta;
    }

    void LateUpdate()
    {
        if (!CheckTouchOperation())
        {
            lastTouchPosition = null;
            movePivot = false;

            return;
        }

        if (Input.touchCount == 1)
        {
            Touch touch1 = Input.touches[0];

            if (touch1.phase == TouchPhase.Began)
            {
                lastTouchPosition = touch1.position;

                GameObject obj = GetPickedBrick(touch1);
                if (obj)
                    ChangePivot(obj);
            }
            else if (touch1.phase == TouchPhase.Moved)
            {
                if (lastTouchPosition.HasValue)
                    StartCoroutine(Rotate(touch1));
            }
        }
        else if (Input.touchCount == 2)
        {
            lastTouchPosition = null;
            movePivot = false;

            Touch touch1 = Input.touches[0];
            Touch touch2 = Input.touches[1];

            // if at least one of them moved
            if (touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved)
                ZoomInOut(touch1, touch2);
        }

        if (movePivot)
            StartCoroutine(RefreshPivot());
    }
}
