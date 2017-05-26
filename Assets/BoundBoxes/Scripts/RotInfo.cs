using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class RotationInfo
{
    public enum RotMode
    {
        IDLE,
        PREPARED,
        ROTATING,
    };

    const float virtualTrackballDistance = 0.25f;
    const float minPitchInDegree = -80.0f;
    const float aboveMinY = 0.8f;

    public Vector2? lastTouchPosition { get; private set; }
    public RotMode mode { get; private set; }

    public RotationInfo()
    {
        Clear();
    }

    public void Clear()
    {
        lastTouchPosition = null;
        mode = RotMode.IDLE;
    }

    public void PrepareRotation(Vector2 pos)
    {
        if (mode != RotMode.IDLE) return;

        UpdateRot(pos);
        mode = RotMode.PREPARED;
    }

    public void TriggerRotation()
    {
        if (mode != RotMode.PREPARED) return;

        mode = RotMode.ROTATING;
    }

    public void UpdateRot(Vector2 pos)
    {
        lastTouchPosition = pos;
    }

    float AdjustPitch(Vector3 candPos, Vector3 pivotPos, float dist, float minAngle, float maxAngle)
    {
        Plane basePlaine = new Plane(Vector3.up, candPos);

        float height = basePlaine.GetDistanceToPoint(pivotPos);
        float angle = Mathf.Rad2Deg * Mathf.Asin(height / dist);
        float delta = (angle < minAngle) ? (angle - minAngle) : (angle > maxAngle) ? (maxAngle - angle) : 0;

        return delta;
    }

    public Quaternion FigureOutAxisAngleRotation(Vector3 pivotPos, Vector3 vecPos, Vector2 curTouch)
    {
        if (!lastTouchPosition.HasValue || lastTouchPosition == curTouch)
            return Quaternion.identity;

        Vector3 p1 = Camera.main.ScreenToWorldPoint(new Vector3(lastTouchPosition.Value.x, lastTouchPosition.Value.y, Camera.main.nearClipPlane));
        Vector3 p2 = Camera.main.ScreenToWorldPoint(new Vector3(curTouch.x, curTouch.y, Camera.main.nearClipPlane));
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
}
