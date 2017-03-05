using UnityEngine;
using System.Collections;
 
[AddComponentMenu("Camera-Control/3dsMax Camera Modified")]
public class BoundBoxes_maxCamera : MonoBehaviour
{
    const float aboveYmin = 0.8f;
    const float yMaxLimit = 80.0f;

    const float pinchRatio = 2;
    const float minPinchDistance = 0;

    const float zoomRate = 1.0f;
    const float panSpeed = 5.0f;
    const float rotDampening = 5.0f;
    const float zoomDampening = 0.2f;

    public Transform target;
	public GameObject terrainMesh;
    public Vector3 targetOffset;
    public float distance = 5.0f;

    [System.ComponentModel.DefaultValue(20f)]
    public float maxDistance { get; set; }
    [System.ComponentModel.DefaultValue(1f)]
    public float minDistance { get; set; }

    private float xSpeed = 4.0f;
    private float ySpeed = 4.0f;
    private float xDeg = 0.0f;
    private float yDeg = 0.0f;
    private float currentDistance;
    private float desiredDistance;
    private Quaternion desiredRotation;
    private Quaternion rotation;
    private Vector3 position;
	private float yMinLimit;
	private float viewerYmin;
	private Vector2 desiredInputPosition;
	private Vector2 currentInputPosition;
	private Vector3 hitPoint = Vector3.zero;
	private bool dragging = false;

    void Start() { Init(); }
    void OnEnable() { Init(); }
 
    public void Init()
    {
        //If there is no target, create a temporary target at 'distance' from the camera's current viewpoint
		viewerYmin = aboveYmin;
        if (!target)
        {
            GameObject go = new GameObject("Cam Target");
            go.transform.position = transform.position + (transform.forward * distance);
            target = go.transform;
        }
 
        distance = Vector3.Distance(transform.position, target.position);
        currentDistance = distance;
        desiredDistance = distance;
 
        //be sure to grab the current rotations as starting points.
		transform.LookAt(target);
		
        position = transform.position;
        rotation = transform.rotation;
        desiredRotation = transform.rotation;
 
        xDeg = Vector3.Angle(Vector3.right, transform.right);
        yDeg = Vector3.Angle(Vector3.up, transform.up);
		if (transform.position.y < target.position.y) yDeg *= -1;
    }

    void UpdateByTouch()
    {
        float pinchDistanceDelta = 0;

        if (Input.touchCount == 1)
        {
            Touch touch1 = Input.touches[0];

            desiredInputPosition = touch1.position;
            if (touch1.phase == TouchPhase.Began)
                currentInputPosition = desiredInputPosition;

            RaycastHit[] hits;
            hits = Physics.RaycastAll(GetComponent<Camera>().ScreenPointToRay(currentInputPosition), 100);
            if (hits.Length == 0) return;

            bool prevDrag = dragging;
            dragging = false;
            foreach (RaycastHit hit in hits)
            {
                if (hit.transform.gameObject == terrainMesh)
                {
                    hitPoint = hit.point;
                    dragging = true;
                    break;
                }
            }
            if (!prevDrag && dragging)
                StartCoroutine("DragObject", hitPoint);

            // calculate position based on the new currentDistance 	
            currentInputPosition = Vector2.Lerp(currentInputPosition, desiredInputPosition, Time.deltaTime * panSpeed);
        }
        else if (Input.touchCount == 2)
        {
            Touch touch1 = Input.touches[0];
            Touch touch2 = Input.touches[1];

            // ... if at least one of them moved ...
            if (touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved)
            {
                // ... check the delta distance between them ...
                float pinchDistance = Vector2.Distance(touch1.position, touch2.position);
                float prevDistance = Vector2.Distance(touch1.position - touch1.deltaPosition,
                                                      touch2.position - touch2.deltaPosition);

                Vector2 delta = touch1.deltaPosition - touch2.deltaPosition;
                pinchDistanceDelta = pinchDistance - prevDistance;

                // ... if it's greater than a minimum threshold, it's a pinch!
                if (Mathf.Abs(pinchDistanceDelta) > minPinchDistance)
                    pinchDistanceDelta *= (pinchRatio / Screen.width);
                else
                    pinchDistanceDelta = 0;
            }
        }

        if (pinchDistanceDelta != 0.0f)
        {
            var delta = pinchDistanceDelta * zoomRate * Mathf.Abs(desiredDistance);
            desiredDistance = Mathf.Clamp(desiredDistance - delta, minDistance, maxDistance);
        }
        if (currentDistance != desiredDistance)
        {
            currentDistance = Mathf.Lerp(currentDistance, desiredDistance, zoomDampening);

            position = target.position - (rotation * Vector3.forward * currentDistance + targetOffset);
            transform.position = position;
        }
    }

    void UpdateByMouse()
    {
        if (Input.GetMouseButton(1))
        {
            xDeg += Input.GetAxis("Mouse X") * xSpeed;
            yDeg -= Input.GetAxis("Mouse Y") * ySpeed;

            //Clamp the vertical axis for the orbit
            yMinLimit = -Mathf.Rad2Deg * Mathf.Asin((target.position.y - viewerYmin) / currentDistance);
            yDeg = ClampAngle(yDeg, yMinLimit, yMaxLimit);

            // set camera rotation 
            desiredRotation = Quaternion.Euler(yDeg, xDeg, 0);
            rotation = Quaternion.Lerp(transform.rotation, desiredRotation, Time.deltaTime * rotDampening);
            transform.rotation = rotation;

            position = target.position - (rotation * Vector3.forward * currentDistance + targetOffset);
            transform.position = position;
        }
        else if (Input.GetMouseButton(0))
        {
            desiredInputPosition = Input.mousePosition;
            if (Input.GetMouseButtonDown(0))
                currentInputPosition = desiredInputPosition;

            RaycastHit[] hits;
            hits = Physics.RaycastAll(GetComponent<Camera>().ScreenPointToRay(currentInputPosition), 100);
            if (hits.Length == 0) return;

            bool prevDrag = dragging;
            dragging = false;
            foreach (RaycastHit hit in hits)
            {
                if (hit.transform.gameObject == terrainMesh)
                {
                    hitPoint = hit.point;
                    dragging = true;
                    break;
                }
            }
            if (!prevDrag && dragging)
                StartCoroutine("DragObject", hitPoint);

            // calculate position based on the new currentDistance 	
            currentInputPosition = Vector2.Lerp(currentInputPosition, desiredInputPosition, Time.deltaTime * panSpeed);
        }

        var scrollVal = Input.GetAxis("Mouse ScrollWheel");
        if (scrollVal != 0.0f)
        {
            // affect the desired Zoom distance if we roll the scrollwheel
            var delta = scrollVal * zoomRate * Mathf.Abs(desiredDistance);
            desiredDistance = Mathf.Clamp(desiredDistance - delta, minDistance, maxDistance);
        }
        if (currentDistance != desiredDistance)
        {
            currentDistance = Mathf.Lerp(currentDistance, desiredDistance, zoomDampening);

            position = target.position - (rotation * Vector3.forward * currentDistance + targetOffset);
            transform.position = position;
        }
    }

    void LateUpdate()
    {
#if UNITY_STANDALONE_WIN
        UpdateByMouse();
#else
        UpdateByTouch();
#endif
    }

    private static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360)
            angle += 360;
        if (angle > 360)
            angle -= 360;

        return Mathf.Clamp(angle, min, max);
    }
	
	void Update()
    {
	}

	IEnumerator DragObject (Vector3 startingHit)
    {
        float dragSpeed = Time.deltaTime * panSpeed;

#if UNITY_STANDALONE_WIN
        while (Input.GetMouseButton(0) && dragging)
#else
        while (Input.touchCount == 1 && dragging)
#endif
        {	
			var translation = startingHit - hitPoint;

            translation.x = Mathf.Clamp(translation.x, -dragSpeed, dragSpeed);
            translation.z = Mathf.Clamp(translation.z, -dragSpeed, dragSpeed);

            transform.position = transform.position + translation;
            target.position = target.position + translation;

			yield return null;
		}

		dragging = false;
	}
}
