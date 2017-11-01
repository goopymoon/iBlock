using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoomInfo
{
    public const float pinchRatio = 2f;
    public const float zoomDampening = 0.2f;

    public float maxDistance { get; set; }
    public float minDistance { get; set; }
    public float curDistance { get; private set; }
    public float destDistance { private get; set; }

    public Touch? touch1;
    public Touch? touch2;

    public ZoomInfo(float distance)
    {
        curDistance = distance;
        destDistance = distance;

        maxDistance = 12.0f;
        minDistance = 1.0f;

        ClearTouch();
    }

    public void ClearTouch()
    {
        touch1 = null;
        touch2 = null;
    }

    public void SetTouch(Touch t1, Touch t2)
    {
        touch1 = t1;
        touch2 = t2;
    }

    static public float ClampZoomingDistancePerTick(float dist)
    {
        return dist < zoomDampening ? dist : zoomDampening;
    }

    public bool CalZoomDist(out float dist)
    {
        dist = 0.0f;

        if (!touch1.HasValue || !touch2.HasValue)
            return false;

        float pinchDistance = Vector2.Distance(touch1.Value.position, touch2.Value.position);
        float prevDistance = Vector2.Distance(touch1.Value.position - touch1.Value.deltaPosition, touch2.Value.position - touch2.Value.deltaPosition);
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

        if (Mathf.Abs(curDistance - destDistance) > zoomDampening)
        {
            curDistance = Mathf.Lerp(curDistance, destDistance, zoomDampening);
            dist = curDistance;
            return true;
        }

        return false;
    }
}
