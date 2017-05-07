using UnityEngine;
using System.Collections;

public class TrackballCamera : CameraWithTouchFilter
{
    class PivotInfo
    {
        const float pivotPickInterval = 0.7f;

        // current pivot;
        public Vector3 pivotPos;

        // under moving pivot
        public bool movePivot;

        // pivot candidate
        public GameObject pickedObj;
        public float pickedTime;

        public PivotInfo()
        {
            GameObject go = new GameObject("Cam Target");
            go.transform.position = Vector3.zero;
            pivotPos = go.transform.position;

            Clear();
        }

        public void Clear()
        {
            pickedObj = null;
            movePivot = false;
        }

        public bool IsMovingToCandidate()
        {
            return movePivot;
        }

        public float GetPivotDistanceToMove()
        {
            return pickedObj ? Vector3.Distance(pickedObj.transform.position, pivotPos) : 0.0f;
        }

        public void SetPivotCandidate(GameObject obj, float curTime)
        {
            pickedObj = obj;
            pickedTime = curTime;
        }

        public void SelectPivotCandidate(GameObject obj)
        {
            float curTime = Time.time;

            if (!movePivot)
            {
                if (pickedObj == null || pickedObj != obj || (curTime - pickedTime > pivotPickInterval))
                    SetPivotCandidate(obj, curTime);
                else
                    movePivot = true;
            }
            else
            {
                SetPivotCandidate(obj, curTime);
                movePivot = false;
            }
        }

        public void MoveToCandidate(float dist)
        {
            var vecPos = (pickedObj.transform.position - pivotPos).normalized * dist;
            pivotPos = pivotPos + vecPos;
        }
    }

    class ZoomInfo
    {
        public const float pinchRatio = 2f;
        public const float zoomDampening = 0.2f;

        public float maxDistance { get; set; }
        public float minDistance { get; set; }

        public float curDistance { get; private set; }
        public float destDistance { private get; set; }

        public ZoomInfo(float distance)
        {
            curDistance = distance;
            destDistance = distance;

            maxDistance = 12.0f;
            minDistance = 1.0f;
        }

        static public float ClampZoomingDistancePerTick(float dist)
        {
            return dist < zoomDampening ? dist : zoomDampening;
        }

        public bool Zoom(Touch touch1, Touch touch2, out float dist)
        {
            float pinchDistance = Vector2.Distance(touch1.position, touch2.position);
            float prevDistance = Vector2.Distance(touch1.position - touch1.deltaPosition, touch2.position - touch2.deltaPosition);
            float pinchDistanceDelta = pinchDistance - prevDistance;

            if (Mathf.Abs(pinchDistanceDelta) > 0.0f)
                pinchDistanceDelta *= (pinchRatio / Screen.width);
            else
                pinchDistanceDelta = 0;

            if (pinchDistanceDelta != 0.0f)
            {
                var delta = pinchDistanceDelta * Mathf.Abs(destDistance);
                destDistance = Mathf.Clamp(destDistance - delta, minDistance, maxDistance);
            }

            dist = 0.0f;
            if (Mathf.Abs(curDistance - destDistance) > zoomDampening)
            {
                curDistance = Mathf.Lerp(curDistance, destDistance, zoomDampening);
                dist = curDistance;
                return true;
            }

            return false;
        }
    }

    class RotationInfo
    {
        const float virtualTrackballDistance = 0.25f;
        const float minPitchInDegree = -80.0f;
        const float aboveMinY = 0.8f;

        Vector2? lastTouchPosition = null;

        public void Clear()
        {
            lastTouchPosition = null;
        }

        public void PrepareRot(Vector2 pos)
        {
            lastTouchPosition = pos;
        }

        public bool CanRot()
        {
            return lastTouchPosition.HasValue;
        }

        public void UpdateRot(Vector2 pos)
        {
            lastTouchPosition = pos;
        }

        public bool TryGetLastTouchPosition(out Vector2 val)
        {
            if (CanRot())
            {
                val = lastTouchPosition.Value;
                return true;
            }
            else
            {
                val = Vector2.zero;
                return false;
            }
        }

        float AdjustPitch(Vector3 candPos, Vector3 pivotPos, float dist, float minAngle, float maxAngle)
        {
            Plane basePlaine = new Plane(Vector3.up, candPos);

            float height = basePlaine.GetDistanceToPoint(pivotPos);
            float angle = Mathf.Rad2Deg * Mathf.Asin(height / dist);
            float delta = (angle < minAngle) ? (angle - minAngle) : (angle > maxAngle) ? (maxAngle - angle) : 0;

            return delta;
        }

        public Quaternion CalculateAngle(Vector3 pivotPos, Vector3 vecPos, Vector3 p1, Vector3 p2)
        {
            Vector3 axisOfRotation = Vector3.Cross(p1, p2);

            float twist = (p2 - p1).magnitude / (2.0f * virtualTrackballDistance);
            float phi = Mathf.Rad2Deg * Mathf.Asin(Mathf.Clamp(twist, -1.0f, 1.0f));
            Quaternion rotation = Quaternion.AngleAxis(phi, axisOfRotation);

            Vector3 candPos = rotation * vecPos + pivotPos;
            float dist = Vector3.Distance(candPos, pivotPos);

            float maxPitchInDegree = Mathf.Rad2Deg * Mathf.Asin((pivotPos.y - aboveMinY) / dist);
            phi += AdjustPitch(candPos, pivotPos, dist, minPitchInDegree, maxPitchInDegree);

            return Quaternion.AngleAxis(phi, axisOfRotation);
        }
    }

    public GameObject terrainMesh;
    public Material lineMaterial;

    private PivotInfo pivotInfo;
    private ZoomInfo zoomInfo;
    private RotationInfo rotInfo;

    void Start()
    {
        pivotInfo = new PivotInfo();
        // Be sure to grab the current rotations as starting points.
        transform.LookAt(pivotInfo.pivotPos);

        float distance = Vector3.Distance(transform.position, pivotInfo.pivotPos);
        zoomInfo = new ZoomInfo(distance);

        rotInfo = new RotationInfo();
    }

    void ZoomInOut(Touch touch1, Touch touch2)
    {
        float dist;
        if (zoomInfo.Zoom(touch1, touch2, out dist))
        {
            var vecPos = (this.transform.position - pivotInfo.pivotPos).normalized * dist;
            this.transform.position = vecPos + pivotInfo.pivotPos;
        }
    }

    Quaternion FigureOutAxisAngleRotation(Vector3 pivotPos, Vector3 vecPos, Vector3 lastTouch, Vector3 curTouch)
    {
        if (lastTouch == curTouch)
            return Quaternion.identity;

        Vector3 near = new Vector3(0, 0, Camera.main.nearClipPlane);
        Vector3 p1 = Camera.main.ScreenToWorldPoint(curTouch + near);
        Vector3 p2 = Camera.main.ScreenToWorldPoint(lastTouch + near);

        return rotInfo.CalculateAngle(pivotPos, vecPos, p1, p2);
    }

    IEnumerator Rotate(Touch touch1)
    {
        Vector2 lastPos;
        if (!rotInfo.TryGetLastTouchPosition(out lastPos))
            yield break;

        // Rotate
        var startPos = (this.transform.position - pivotInfo.pivotPos).normalized * zoomInfo.curDistance;
        var rotation = FigureOutAxisAngleRotation(pivotInfo.pivotPos, startPos, lastPos, touch1.position);
        var position = rotation * startPos + pivotInfo.pivotPos;

        this.transform.position = position;
        this.transform.LookAt(pivotInfo.pivotPos);

        // Update last touch position
        rotInfo.UpdateRot(touch1.position);
    }

    IEnumerator RefreshPivot()
    {
        while (pivotInfo.GetPivotDistanceToMove() >= ZoomInfo.zoomDampening)
        {
            pivotInfo.MoveToCandidate(ZoomInfo.zoomDampening);
            transform.LookAt(pivotInfo.pivotPos);
            yield return null;
        }

        float targetDist = pivotInfo.GetPivotDistanceToMove();
        float dist = ZoomInfo.ClampZoomingDistancePerTick(targetDist);
        if (dist > 0.0f)
        { 
            pivotInfo.MoveToCandidate(dist);
            transform.LookAt(pivotInfo.pivotPos);
        }

        pivotInfo.Clear();
    }

    GameObject GetPickedBrick(Touch touch)
    {
        RaycastHit[] hits = Physics.RaycastAll(Camera.main.ScreenPointToRay(touch.position));

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.gameObject != terrainMesh)
                return hit.transform.gameObject;
        }

        return null;
    }

    void OnPostRender()
    {
        Vector2 pos;

        if (rotInfo.TryGetLastTouchPosition(out pos))
        {
            const float radius = 100;
            const int delta = 10;

            GL.PushMatrix();
            GL.Begin(GL.LINES);
            lineMaterial.SetPass(0);
            GL.Color(Color.white);
            for (int i = 0; i < 360; i+=delta)
            {
                float degInRad1 = i * Mathf.Deg2Rad;
                float degInRad2 = ((i + delta) % 360) * Mathf.Deg2Rad;
                Vector3 p1 = new Vector3(pos.x + Mathf.Cos(degInRad1) * radius, pos.y + Mathf.Sin(degInRad1) * radius, Camera.main.nearClipPlane);
                Vector3 p2 = new Vector3(pos.x + Mathf.Cos(degInRad2) * radius, pos.y + Mathf.Sin(degInRad2) * radius, Camera.main.nearClipPlane);
                GL.Vertex(Camera.main.ScreenToWorldPoint(p1));
                GL.Vertex(Camera.main.ScreenToWorldPoint(p2));
            }
            GL.End();
            GL.PopMatrix();
        }
    }

    void LateUpdate()
    {
        if (!CheckTouchOperation())
        {
            rotInfo.Clear();
            pivotInfo.Clear();

            return;
        }

        if (Input.touchCount == 1)
        {
            Touch touch1 = Input.touches[0];

            if (touch1.phase == TouchPhase.Began)
            {
                rotInfo.PrepareRot(touch1.position);

                GameObject obj = GetPickedBrick(touch1);
                if (obj)
                    pivotInfo.SelectPivotCandidate(obj);
            }
            else if (touch1.phase == TouchPhase.Moved)
            {
                if (rotInfo.CanRot())
                    StartCoroutine(Rotate(touch1));
            }
        }
        else if (Input.touchCount == 2)
        {
            rotInfo.Clear();
            pivotInfo.Clear();

            Touch touch1 = Input.touches[0];
            Touch touch2 = Input.touches[1];

            // if at least one of them moved
            if (touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved)
                ZoomInOut(touch1, touch2);
        }

        if (pivotInfo.IsMovingToCandidate())
            StartCoroutine(RefreshPivot());
    }
}
