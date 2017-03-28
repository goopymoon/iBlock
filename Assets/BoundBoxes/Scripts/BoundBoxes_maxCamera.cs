using UnityEngine;
using System.Collections;
 
public class BoundBoxes_maxCamera : MonoBehaviour
{
    public VirtualJoyStick joyStickRight;

    // angle
    const float yMinLimit = -20.0f;
    const float yMaxLimit = 70.0f;
    const float rotDampening = 5.0f;

    private float xSpeed = 2.0f;
    private float ySpeed = 2.0f;
    private float xDeg = 0.0f;
    private float yDeg = 0.0f;
    private Quaternion desiredRotation;
    private Quaternion rotation;

    // zoom
    const float pinchRatio = 2;
    const float minPinchDistance = 0;
    const float zoomRate = 1.0f;
    const float zoomDampening = 0.2f;

    public Transform target;
    public Vector3 targetOffset;
    public float distance = 5.0f;

    [System.ComponentModel.DefaultValue(20f)]
    public float maxDistance { get; set; }
    [System.ComponentModel.DefaultValue(1f)]
    public float minDistance { get; set; }

    private float currentDistance;
    private float desiredDistance;

    public void Init()
    {
        //If there is no target, create a temporary target
        if (!target)
        {
            GameObject go = new GameObject("Cam Target");
            go.transform.position = Vector3.zero;
            target = go.transform;
        }
 
        distance = Vector3.Distance(transform.position, target.position);
        currentDistance = distance;
        desiredDistance = distance;
 
        //be sure to grab the current rotations as starting points.
		transform.LookAt(target);
		
        rotation = transform.rotation;
        desiredRotation = transform.rotation;
 
        xDeg = Vector3.Angle(Vector3.right, transform.right);
        yDeg = Vector3.Angle(Vector3.up, transform.up);
		if (transform.position.y < target.position.y)
            yDeg *= -1;
    }

    void ZoomInOut()
    {
        if (Input.touchCount != 2)
            return;

        float pinchDistanceDelta = 0;

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
            if (Mathf.Abs(pinchDistanceDelta) > minPinchDistance)
                pinchDistanceDelta *= (pinchRatio / Screen.width);
            else
                pinchDistanceDelta = 0;

            if (pinchDistanceDelta != 0.0f)
            {
                var delta = pinchDistanceDelta * zoomRate * Mathf.Abs(desiredDistance);
                desiredDistance = Mathf.Clamp(desiredDistance - delta, minDistance, maxDistance);
            }
        }

        if (currentDistance != desiredDistance)
        {
            currentDistance = Mathf.Lerp(currentDistance, desiredDistance, zoomDampening);
            transform.position -= (rotation * Vector3.forward * currentDistance + targetOffset);
        }
    }

    private static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360)
            angle += 360;
        if (angle > 360)
            angle -= 360;

        return Mathf.Clamp(angle, min, max);
    }
	
    void Rotate()
    {
        if (joyStickRight.InputDirection.x != 0 || joyStickRight.InputDirection.y != 0)
        {
            xDeg += joyStickRight.InputDirection.x * xSpeed;
            yDeg -= joyStickRight.InputDirection.y * ySpeed;

            yDeg = ClampAngle(yDeg, yMinLimit, yMaxLimit);

            // set camera rotation 
            var desiredRotation = Quaternion.Euler(yDeg, xDeg, 0);
            var rotation = Quaternion.Lerp(transform.rotation, desiredRotation, Time.deltaTime * rotDampening);
            transform.rotation = rotation;

            target.position -= (rotation * Vector3.forward * currentDistance + targetOffset);
        }
    }

    void Start() {}

    void OnEnable()
    {
        Init();
    }

    void LateUpdate()
    {
        //ZoomInOut();
        Rotate();
    }
}
