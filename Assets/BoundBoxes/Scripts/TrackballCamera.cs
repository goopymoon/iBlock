using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TrackballCamera : CameraWithTouchFilter
{
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
            if (pickedObj != null)
                pickedObj.GetComponent<Brick>().RestoreMaterial();

            pickedObj = obj;
            pickedTime = curTime;
            mode = mode_;

            if (mode == SelectMode.CONFIRMED)
                pickedObj.GetComponent<Brick>().ShowSilhouette();
        }

        public void SelectPivotCandidate(GameObject obj)
        {
            float curTime = Time.time;

            switch(mode)
            {
                case SelectMode.NA:
                    Debug.Assert(pickedObj == null, "PickObj must be null.");
                    SetPivotCandidate(obj, curTime, SelectMode.SELECTED);
                    break;
                case SelectMode.SELECTED:
                    Debug.Assert(pickedObj != null, "PickObj must not be null.");
                    if (pickedObj == obj)
                    {
                        if (curTime - pickedTime < pivotPickInterval)
                        {
                            SetPivotCandidate(obj, curTime, SelectMode.CONFIRMED);
                        }
                        else
                        {
                            Debug.Log(string.Format("{0}: {1} sec", pickedObj.ToString(), curTime - pickedTime));
                        }
                    }
                    else
                    {
                        Debug.Log(string.Format("{0} != {1}", pickedObj.ToString(), obj.ToString()));
                        Clear();
                    }
                    break;
                case SelectMode.CONFIRMED:
                default:
                    SetPivotCandidate(obj, curTime, SelectMode.SELECTED);
                    break;
            }
        }

        public void MoveToCandidate(float dist)
        {
            var vecPos = (pickedObj.transform.position - pivotPos).normalized * dist;
            pivotPos = pivotPos + vecPos;
        }
    }

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
            pivotInfo.Clear();
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
