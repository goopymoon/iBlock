using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class PivotInfo
{
    public enum SelectMode
    {
        NA,
        SELECTED,
        CONFIRMED,
    };

    const float pivotPickInterval = 0.7f;

    public Vector3 pivotPos { get; private set; }
    public SelectMode mode { get; private set; }

    GameObject pickedObj;
    float pickedTime;

    public PivotInfo()
    {
        GameObject go = new GameObject("Cam Target");
        go.transform.position = Vector3.zero;
        pivotPos = go.transform.position;

        pickedObj = null;
        Clear();
    }

    public void Clear()
    {
        mode = SelectMode.NA;
    }

    public float GetPivotDistanceToMove()
    {
        return pickedObj ? Vector3.Distance(pickedObj.transform.position, pivotPos) : 0.0f;
    }

    public void SetPivotCandidate(GameObject obj, float curTime, SelectMode mode_)
    {
        if (pickedObj)
            pickedObj.GetComponent<Brick>().RestoreMaterial();

        pickedObj = obj;
        pickedTime = curTime;
        mode = mode_;

        if (mode == SelectMode.SELECTED)
            pickedObj.GetComponent<Brick>().ShowSilhouette();
    }

    public void SelectPivotCandidate(GameObject obj)
    {
        float curTime = Time.time;

        switch (mode)
        {
            case SelectMode.NA:
                Debug.Log(string.Format("SelectMode.NA: {0}", obj.ToString()));
                Debug.Assert(pickedObj == null, "PickObj must be null.");
                SetPivotCandidate(obj, curTime, SelectMode.SELECTED);
                break;
            case SelectMode.SELECTED:
                Debug.Log(string.Format("SelectMode.SELECTED: {0}", obj.ToString()));
                Debug.Assert(pickedObj != null, "PickObj must not be null.");
                if (pickedObj == obj)
                {
                    if (curTime - pickedTime > pivotPickInterval)
                    {
                        Debug.Log(string.Format("Time over: {0} sec", curTime - pickedTime));
                        Clear();
                    }
                    SetPivotCandidate(obj, curTime, SelectMode.CONFIRMED);
                }
                break;
            case SelectMode.CONFIRMED:
                Debug.Log(string.Format("SelectMode.CONFIRMED: {0}", obj.ToString()));
                break;
            default:
                break;
        }
    }

    public void MoveToCandidate(float dist)
    {
        var vecPos = (pickedObj.transform.position - pivotPos).normalized * dist;
        pivotPos = pivotPos + vecPos;
    }
}

