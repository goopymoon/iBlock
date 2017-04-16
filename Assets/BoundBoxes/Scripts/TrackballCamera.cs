using UnityEngine;
using System.Collections;

public class TrackballCamera : CameraWithTouchFilter
{
    const float pinchRatio = 2f;
    const float pinchMinDistance = 0f;
    const float zoomDampening = 0.2f;
    const float minPitchInDegree = -80.0f;
    const float aboveMinY = 0.8f;

    public GameObject target;
    public float virtualTrackballDistance = 0.25f;

    [System.ComponentModel.DefaultValue(20f)]
    public float maxDistance { get; set; }
    [System.ComponentModel.DefaultValue(1f)]
    public float minDistance { get; set; }

    private Vector2? lastTouchPosition = null;
    private float curDistance = 15f;
    private float destDistance;

    void Start()
    {
        var startPos = (this.transform.position - target.transform.position).normalized * curDistance;
        var position = startPos + target.transform.position;
        transform.position = position;
        transform.LookAt(target.transform.position);

        destDistance = curDistance;
    }

    void LateUpdate()
    {
        CheckTouchOperation();
        if (!IsTouchAvailable())
        {
            lastTouchPosition = null;
            return;
        }

        float pinchDistanceDelta = 0;

        if (Input.touchCount == 1)
        {
            Touch touch1 = Input.touches[0];

            if (touch1.phase == TouchPhase.Began)
            {
                lastTouchPosition = touch1.position;
            }
            else if (lastTouchPosition.HasValue && touch1.phase == TouchPhase.Moved)
            {
                var lastPos = this.transform.position;
                var pivotPos = target.transform.position;

                var rotation = FigureOutAxisAngleRotation(pivotPos, lastPos, lastTouchPosition.Value, touch1.position);
                var vecPos = (lastPos - pivotPos).normalized * curDistance;

                this.transform.position = rotation * vecPos + pivotPos;
                this.transform.LookAt(pivotPos);

                lastTouchPosition = touch1.position;
            }
        }
        else if (Input.touchCount == 2)
        {
            Touch touch1 = Input.touches[0];
            Touch touch2 = Input.touches[1];

            // if at least one of them moved
            if (touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved)
            {
                // check the delta distance between them ...
                float pinchDistance = Vector2.Distance(touch1.position, touch2.position);
                float prevDistance = Vector2.Distance(touch1.position - touch1.deltaPosition,
                    touch2.position - touch2.deltaPosition);

                pinchDistanceDelta = pinchDistance - prevDistance;

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

                    var lastPos = this.transform.position;
                    var pivotPos = target.transform.position;

                    var vecPos = (lastPos - pivotPos).normalized * curDistance;

                    this.transform.position = vecPos + pivotPos;
                }
            }
        }
    }

    Quaternion FigureOutAxisAngleRotation(Vector3 pivotPos, Vector3 lastPos, Vector3 lastTouch, Vector3 curTouch)
    {
        if (lastTouch.x == curTouch.x && lastTouch.y == curTouch.y)
            return Quaternion.identity;

        Vector3 near = new Vector3(0, 0, Camera.main.nearClipPlane);
        Vector3 p1 = Camera.main.ScreenToWorldPoint(curTouch + near);
        Vector3 p2 = Camera.main.ScreenToWorldPoint(lastTouch + near);

        var axisOfRotation = Vector3.Cross(p1, p2);

        var twist = (p2 - p1).magnitude / (2.0f * virtualTrackballDistance);
        if (twist > 1.0f)
            twist = 1.0f;
        if (twist < -1.0f)
            twist = -1.0f;

        var phi = Mathf.Rad2Deg * Mathf.Asin(twist);
        var rotation = Quaternion.AngleAxis(phi, axisOfRotation);
        var vecPos = (lastPos - pivotPos).normalized * curDistance;
        var candPos = rotation * vecPos + pivotPos;
        float maxPitchInDegree = Mathf.Rad2Deg * Mathf.Asin((pivotPos.y - aboveMinY) / curDistance);

        phi += AdjustPitch(candPos, pivotPos, minPitchInDegree, maxPitchInDegree);

        return Quaternion.AngleAxis(phi, axisOfRotation);
    }

    float AdjustPitch(Vector3 candPos, Vector3 pivotPos, float minAngle, float maxAngle)
    {
        Plane basePlaine = new Plane(Vector3.up, candPos);

        float height = basePlaine.GetDistanceToPoint(pivotPos);
        float angle = Mathf.Rad2Deg * Mathf.Asin(height/ curDistance);
        float delta = 0;

        if (angle < minAngle)
            delta = angle - minAngle;
        else if (angle > maxAngle)
            delta = maxAngle - angle;

        return delta;
    }
}