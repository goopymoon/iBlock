using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TrackballCamera : CameraWithTouchFilter
{
    public GameObject terrainMesh;
    public Material lineMaterial;

    public CameraOpMgr opMgr;

    void Start()
    {
        opMgr = new CameraOpMgr(this, terrainMesh);

        // Be sure to grab the current rotations as starting points.
        transform.LookAt(opMgr.pivotInfo.pivotPos);
    }

    public void ApplyZoom(float dist)
    {
        var vecPos = (this.transform.position - opMgr.pivotInfo.pivotPos).normalized * dist;
        this.transform.position = vecPos + opMgr.pivotInfo.pivotPos;
    }

    public void ApplyRotation()
    {
        var startPos = (this.transform.position - opMgr.pivotInfo.pivotPos).normalized * opMgr.zoomInfo.curDistance;
        var rotation = opMgr.rotInfo.FigureOutAxisAngleRotation(opMgr.pivotInfo.pivotPos, startPos);
        var position = rotation * startPos + opMgr.pivotInfo.pivotPos;

        this.transform.position = position;
        this.transform.LookAt(opMgr.pivotInfo.pivotPos);
    }

    IEnumerator RefreshPivot()
    {
        while (opMgr.pivotInfo.GetDistanceToMove() >= ZoomInfo.zoomDampening)
        {
            opMgr.pivotInfo.ApproachToPivot(ZoomInfo.zoomDampening);
            transform.LookAt(opMgr.pivotInfo.pivotPos);
            yield return null;
        }

        float targetDist = opMgr.pivotInfo.GetDistanceToMove();
        float dist = ZoomInfo.ClampZoomingDistancePerTick(targetDist);
        if (dist > 0.0f)
        { 
            opMgr.pivotInfo.ApproachToPivot(dist);
            transform.LookAt(opMgr.pivotInfo.pivotPos);
        }

        opMgr.pivotInfo.Clear();
    }

    public void ApplyRefreshingPivot()
    {
        StartCoroutine(RefreshPivot());
    }

    void OnPostRender()
    {
        if (opMgr.CanRotate() && opMgr.rotInfo.lastTouchPos.HasValue)
        {
            const float radius = 100;
            const int delta = 10;

            Vector2 pos = opMgr.rotInfo.lastTouchPos.Value;

            GL.PushMatrix();
            GL.Begin(GL.LINES);
            lineMaterial.SetPass(0);
            GL.Color(Color.white);
            for (int i = 0; i < 360; i+=delta)
            {
                float degInRad1 = i * Mathf.Deg2Rad;
                float degInRad2 = ((i + delta) % 360) * Mathf.Deg2Rad;
                Vector3 p1 = new Vector3(pos.x + Mathf.Cos(degInRad1) * radius, pos.y + Mathf.Sin(degInRad1) * radius, Camera.main.nearClipPlane);
                Vector3 p2 = new Vector3(pos.x + Mathf.Cos(degInRad2) * radius, pos.y + Mathf.Sin(degInRad2) * radius, Camera.main.nearClipPlane);
                GL.Vertex(Camera.main.ScreenToWorldPoint(p1));
                GL.Vertex(Camera.main.ScreenToWorldPoint(p2));
            }
            GL.End();
            GL.PopMatrix();
        }
    }

    void LateUpdate()
    {
        if (!CheckTouchOperation())
        {
            opMgr.StopRotation();
            opMgr.StopZoom();
            return;
        }

        if (Input.touchCount == 1)
        {
            opMgr.HandleOneTouch(Input.touches[0]);
        }
        else if (Input.touchCount == 2)
        {
            opMgr.HandleTwoTouch(Input.touches[0], Input.touches[1]);
        }

        opMgr.Update(Time.deltaTime);
    }
}
