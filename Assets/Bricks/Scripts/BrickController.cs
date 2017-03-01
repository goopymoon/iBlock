using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrickController : MonoBehaviour
{
    public GameObject terrainMesh;

    GameObject headBrick = null;
    GameObject curBrick = null;
    GameObject lastBrick = null;

    public Bounds outerAABB { get; set; }

    enum eControlMode
    {
        CM_SHOW,
        CM_COMPOSE,
    }

    eControlMode controlMode = eControlMode.CM_SHOW;
    float mouseDragDistanceThreshold = 10;

    private Vector3 currentInputPosition;

    public void Register(GameObject go)
    {
        if (headBrick == null)
        {
            headBrick = go;
            curBrick = go;
        }
        else
        {
            curBrick.GetComponent<Brick>().nextBrick = go;
            go.GetComponent<Brick>().prevBrick = curBrick;
            curBrick = go;
            lastBrick = go;
        }
    }

    void SetActiveState(bool flag)
    {
        GameObject itor = headBrick;

        while (itor != null)
        {
            if (flag)
                itor.GetComponent<Brick>().RestoreTransform();

            itor.SetActive(flag);
            itor = itor.GetComponent<Brick>().nextBrick;
        }
    }

    void SetOBBVisibility(bool flag)
    {
        var obbs = gameObject.GetComponentsInChildren<BoundBoxes_BoundBox>();
        foreach (var element in obbs)
        {
            element.SelectBound(flag);
        }
    }

    void StartStage()
    {
        SetOBBVisibility(false);
        SetActiveState(false);

        curBrick = headBrick;
        if (curBrick != null)
        {
            curBrick.SetActive(true);
            if (controlMode == eControlMode.CM_COMPOSE)
                curBrick.GetComponent<BoundBoxes_BoundBox>().SelectBound(true);
        }
    }

    void EndStage()
    {
        SetOBBVisibility(false);
        SetActiveState(true);

        curBrick = null;
    }

    void NextStage()
    {
        if (curBrick == null)
            return;

        if (curBrick == lastBrick)
        {
            EndStage();
            return;
        }

        var nextBrick = curBrick.GetComponent<Brick>().nextBrick;
        if (nextBrick != null)
        {
            curBrick.GetComponent<BoundBoxes_BoundBox>().SelectBound(false);

            curBrick = nextBrick;
            curBrick.SetActive(true);
            curBrick.GetComponent<BoundBoxes_BoundBox>().SelectBound(true);
        }
    }

    void PreviousStage()
    {
        if (curBrick == null)
        {
            curBrick = lastBrick;
            curBrick.GetComponent<BoundBoxes_BoundBox>().SelectBound(true);
            return;
        }

        var prevBrick = curBrick.GetComponent<Brick>().prevBrick;
        if (prevBrick != null)
        {
            curBrick.GetComponent<Brick>().RestoreTransform();
            curBrick.SetActive(false);
            curBrick.GetComponent<BoundBoxes_BoundBox>().SelectBound(false);

            curBrick = prevBrick;
            curBrick.SetActive(true);
            curBrick.GetComponent<BoundBoxes_BoundBox>().SelectBound(true);
        }
    }

    private bool TerrainPos(GameObject go, ref Vector3 pos)
    {
        RaycastHit[] hits = Physics.RaycastAll(Camera.main.ScreenPointToRay(Input.mousePosition));
        if (hits.Length == 0)
            return false;

        bool found = false;
        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.gameObject == go)
                continue;

            if (!found)
            {
                pos = hit.point;
                found = true;
            }
            else
            {
                if (pos.y < hit.point.y)
                    pos = hit.point;
            }
        }

        return found;
    }

    private void ToggleCompseMode()
    {
        if (controlMode == eControlMode.CM_COMPOSE)
            controlMode = eControlMode.CM_SHOW;
        else if (controlMode == eControlMode.CM_SHOW)
            controlMode = eControlMode.CM_COMPOSE;
    }

    private void ControlComposite()
    {
        if (curBrick == null)
            return;

        Vector3 pos = new Vector3();

        if (TerrainPos(curBrick, ref pos))
        { 
            Bounds aabb = curBrick.GetComponent<Brick>().AABB;
            Vector3 raycastStartPos = pos + transform.up * outerAABB.size.y * 2;
            Vector3 maxOffset = pos;

            RaycastHit[] hits = Physics.RaycastAll(raycastStartPos, -1 * curBrick.transform.up);
            foreach (RaycastHit hit in hits)
            {
                if (hit.transform.gameObject != curBrick)
                {
                    Vector3 verticalShift = hit.point + transform.up * aabb.extents.y;
                    if (verticalShift.y > maxOffset.y)
                    {
                        maxOffset = verticalShift;
                    }
                }
            }

            curBrick.transform.position = maxOffset;
        }
    }

    private bool IsMouseButtonClicked(int slot)
    {
        if (Input.GetMouseButtonDown(slot))
        {
            currentInputPosition = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(slot))
        {
            Vector3 dist = Input.mousePosition - currentInputPosition;
            return (dist.magnitude < mouseDragDistanceThreshold);
        }

        return false;
    }

    // Use this for initialization
    void Start()
    {
    }

    // Update is called once per frame
    void Update ()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            ToggleCompseMode();

            if (controlMode == eControlMode.CM_COMPOSE)
                StartStage();
            else if (controlMode == eControlMode.CM_SHOW)
                EndStage();
        }

        if (controlMode == eControlMode.CM_COMPOSE)
        {
            ControlComposite();

            if (Input.GetKeyDown(KeyCode.S))
            {
                StartStage();
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                EndStage();
            }
            else if (Input.GetKeyDown(KeyCode.N) || IsMouseButtonClicked(0))
            {
                NextStage();
            }
            else if (Input.GetKeyDown(KeyCode.P) || IsMouseButtonClicked(1))
            {
                PreviousStage();
            }
        }
    }
}
