using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TrackballCamera : CameraWithTouchFilter
{
    public GameObject terrainMesh;
    public Material lineMaterial;

    private PivotInfo pivotInfo;
    private RotationInfo rotInfo;
    private ZoomInfo zoomInfo;

    void Start()
    {
        pivotInfo = new PivotInfo();
        // Be sure to grab the current rotations as starting points.
        transform.LookAt(pivotInfo.pivotPos);

        float distance = Vector3.Distance(transform.position, pivotInfo.pivotPos);
        zoomInfo = new ZoomInfo(distance);

        rotInfo = new RotationInfo();
    }

    void ZoomInOut(Touch touch1, Touch touch2)
    {
        float dist;
        if (zoomInfo.Zoom(touch1, touch2, out dist))
        {
            var vecPos = (this.transform.position - pivotInfo.pivotPos).normalized * dist;
            this.transform.position = vecPos + pivotInfo.pivotPos;
        }
    }

    void Rotate(Touch touch1)
    {
        if (rotInfo.mode == RotationInfo.RotMode.ROTATING)
        {
            var startPos = (this.transform.position - pivotInfo.pivotPos).normalized * zoomInfo.curDistance;
            var rotation = rotInfo.FigureOutAxisAngleRotation(pivotInfo.pivotPos, startPos, touch1.position);
            var position = rotation * startPos + pivotInfo.pivotPos;

            this.transform.position = position;
            this.transform.LookAt(pivotInfo.pivotPos);

            rotInfo.UpdateRot(touch1.position);
        }
        else if (rotInfo.mode == RotationInfo.RotMode.PREPARED)
        {
            rotInfo.TriggerRotation();
        }
    }

    IEnumerator RefreshPivot()
    {
        while (pivotInfo.GetPivotDistanceToMove() >= ZoomInfo.zoomDampening)
        {
            pivotInfo.MoveToCandidate(ZoomInfo.zoomDampening);
            transform.LookAt(pivotInfo.pivotPos);
            yield return null;
        }

        float targetDist = pivotInfo.GetPivotDistanceToMove();
        float dist = ZoomInfo.ClampZoomingDistancePerTick(targetDist);
        if (dist > 0.0f)
        { 
            pivotInfo.MoveToCandidate(dist);
            transform.LookAt(pivotInfo.pivotPos);
        }

        pivotInfo.Clear();
    }

    GameObject GetPickedBrick(Touch touch)
    {
        RaycastHit[] hits = Physics.RaycastAll(Camera.main.ScreenPointToRay(touch.position));

        List<GameObject> candidates = new List<GameObject>();
        Bounds bounds = new Bounds();

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.gameObject != terrainMesh)
            {
                candidates.Add(hit.transform.gameObject);
                bounds.Encapsulate(hit.transform.gameObject.GetComponent<Brick>().AABB);
            }
        }

        if (candidates.Count > 0)
        {
            APARaycastHit preciseHit;
            APAObjectDictionary.singleton.Init(bounds, candidates);

            if (APARaycast.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out preciseHit))
            {
                preciseHit.transform.GetComponent<Renderer>().material.color = new Color(1, 0, 0, 1);
                return preciseHit.transform.gameObject;
            }
        }

        return null;
    }

    void OnPostRender()
    {
        if (rotInfo.mode == RotationInfo.RotMode.ROTATING)
        {
            const float radius = 100;
            const int delta = 10;

            Vector2 pos = rotInfo.lastTouchPosition.Value;

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
            rotInfo.Clear();
            return;
        }

        if (Input.touchCount == 1)
        {
            Touch touch1 = Input.touches[0];

            if (touch1.phase == TouchPhase.Began)
            {
                GameObject obj = GetPickedBrick(touch1);
                if (obj)
                {
                    pivotInfo.SelectPivotCandidate(obj);
                }

                if (pivotInfo.mode != PivotInfo.SelectMode.CONFIRMED)
                    rotInfo.PrepareRotation(touch1.position);
            }
            else if (touch1.phase == TouchPhase.Stationary)
            {
                rotInfo.TriggerRotation();
            }
            else if (touch1.phase == TouchPhase.Moved)
            {
                Rotate(touch1);
            }
            else if (touch1.phase == TouchPhase.Ended)
            {
                rotInfo.Clear();
            }
        }
        else if (Input.touchCount == 2)
        {
            rotInfo.Clear();
            pivotInfo.Clear();

            Touch touch1 = Input.touches[0];
            Touch touch2 = Input.touches[1];

            // if at least one of them moved
            if (touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved)
                ZoomInOut(touch1, touch2);
        }

        if (pivotInfo.mode == PivotInfo.SelectMode.CONFIRMED)
            StartCoroutine(RefreshPivot());
    }
}
