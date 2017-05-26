using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
