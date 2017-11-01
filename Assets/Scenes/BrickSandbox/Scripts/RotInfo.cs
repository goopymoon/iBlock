using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationInfo
{
    const float virtualTrackballDistance = 0.25f;
    const float minPitchInDegree = -80.0f;
    const float aboveMinY = 0.8f;

    public Vector2? curTouchPos { get; private set; }
    public Vector2? lastTouchPos { get; private set; }

    public void Clear()
    {
        curTouchPos = null;
        lastTouchPos = null;
    }

    public void SetCurTouchPos(Vector2 pos)
    {
        curTouchPos = pos;
    }

    public void RefreshLastTouchPos()
    {
        lastTouchPos = curTouchPos;
    }

    public Quaternion FigureOutAxisAngleRotation(Vector3 pivotPos, Vector3 vecPos)
    {
        if (!lastTouchPos.HasValue || !curTouchPos.HasValue || lastTouchPos == curTouchPos)
            return Quaternion.identity;

        Vector3 p1 = Camera.main.ScreenToWorldPoint(new Vector3(lastTouchPos.Value.x, lastTouchPos.Value.y, Camera.main.nearClipPlane));
        Vector3 p2 = Camera.main.ScreenToWorldPoint(new Vector3(curTouchPos.Value.x, curTouchPos.Value.y, Camera.main.nearClipPlane));
        Vector3 axisOfRotation = Vector3.Cross(p2, p1);

        float twist = (p2 - p1).magnitude / (2.0f * virtualTrackballDistance);
        float phi = Mathf.Rad2Deg * Mathf.Asin(Mathf.Clamp(twist, -1.0f, 1.0f));
        Quaternion rotation = Quaternion.AngleAxis(phi, axisOfRotation);

        Vector3 candPos = rotation * vecPos + pivotPos;
        float dist = Vector3.Distance(candPos, pivotPos);

        float maxPitchInDegree = Mathf.Rad2Deg * Mathf.Asin((pivotPos.y - aboveMinY) / dist);
        phi += AdjustPitch(candPos, pivotPos, dist, minPitchInDegree, maxPitchInDegree);

        return Quaternion.AngleAxis(phi, axisOfRotation);
    }

    private float AdjustPitch(Vector3 candPos, Vector3 pivotPos, float dist, float minAngle, float maxAngle)
    {
        Plane basePlaine = new Plane(Vector3.up, candPos);

        float height = basePlaine.GetDistanceToPoint(pivotPos);
        float angle = Mathf.Rad2Deg * Mathf.Asin(height / dist);
        float delta = (angle < minAngle) ? (angle - minAngle) : (angle > maxAngle) ? (maxAngle - angle) : 0;

        return delta;
    }
}
