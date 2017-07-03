using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PivotInfo
{
    public GameObject candObj { get; set; }
    public Vector3 pivotPos { get; private set; }
    GameObject pickedObj;

    public PivotInfo()
    {
        // Initialize pivotPos
        GameObject go = new GameObject("Cam Target");
        go.transform.position = Vector3.zero;
        pivotPos = go.transform.position;

        // Initialize pickedObj
        pickedObj = null;
    }

    public void Clear()
    {
        Debug.Log(string.Format("Reset: {0}", pickedObj.ToString()));
        RestoreSilhouette();
        pickedObj = null;
    }

    public void Select(out bool isConfirm)
    {
        isConfirm = false;

        RestoreSilhouette();

        if (!pickedObj || pickedObj != candObj)
        {
            pickedObj = candObj;
            ShowSilhouette();
            return;
        }

        isConfirm = true;
    }

    public void RestoreSilhouette()
    {
        if (pickedObj)
            pickedObj.GetComponent<Brick>().RestoreMaterial();
    }

    public void ShowSilhouette()
    { 
        if (pickedObj)
            pickedObj.GetComponent<Brick>().ShowSilhouette();
    }

    public float GetDistanceToMove()
    {
        return pickedObj ? Vector3.Distance(pickedObj.transform.position, pivotPos) : 0.0f;
    }

    public void ApproachToPivot(float dist)
    {
        var vecPos = (pickedObj.transform.position - pivotPos).normalized * dist;
        pivotPos = pivotPos + vecPos;
    }
}

