using UnityEngine;
using System.Collections;

public class DetectTouchMovement : MonoBehaviour
{
    const float pinchRatio = 2;
    const float minPinchDistance = 0;

    static public float pinchDistanceDelta;

    static public void Calculate()
    {
        pinchDistanceDelta = 0;

        // if two fingers are touching the screen at the same time ...
        if (Input.touchCount == 2)
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
    }
}