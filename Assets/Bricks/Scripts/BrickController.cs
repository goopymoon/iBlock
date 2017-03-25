using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrickController : MonoBehaviour
{
    public GameObject terrainMesh;

    GameObject headBrick = null;
    GameObject curBrick = null;
    GameObject lastBrick = null;

    enum eControlMode
    {
        CM_SHOW,
        CM_COMPOSE,
    }

    private eControlMode controlMode = eControlMode.CM_SHOW;

    private const float mouseDragDistanceThreshold = 5;
    private const float brickMoveSpeed = 5.0f;
    private Vector3 brickPos = Vector3.zero;
    private Vector3 destInputPosition;

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

    public void StartStage()
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

    public void EndStage()
    {
        SetOBBVisibility(false);
        SetActiveState(true);

        curBrick = null;
    }

    public void NextStage()
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

    public void PreviousStage()
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

    public void ToggleCompseMode()
    {
        if (controlMode == eControlMode.CM_COMPOSE)
            controlMode = eControlMode.CM_SHOW;
        else if (controlMode == eControlMode.CM_SHOW)
            controlMode = eControlMode.CM_COMPOSE;
    }

    private bool IsMouseButtonClicked(int slot)
    {
        if (Input.GetMouseButtonDown(slot))
        {
            destInputPosition = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(slot))
        {
            Vector3 delta = Input.mousePosition - destInputPosition;
            return (delta.magnitude < mouseDragDistanceThreshold);
        }

        return false;
    }

    private void StickToGridLine(ref Vector3 pos)
    {
        pos.x = Mathf.Ceil(pos.x / LdConstant.LDU_IN_MM) * LdConstant.LDU_IN_MM;
        pos.z = Mathf.Ceil(pos.z / LdConstant.LDU_IN_MM) * LdConstant.LDU_IN_MM;
    }

    private bool GetBrickTargetPos(GameObject skipObj, out Vector3 pos)
    {
        RaycastHit[] hits = Physics.RaycastAll(Camera.main.ScreenPointToRay(Input.mousePosition));
        bool found = false;

        pos = Vector3.zero;
        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.gameObject == skipObj)
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

    IEnumerator DragBrick(Vector3 destPos)
    {
        float moveSpeed = Time.deltaTime * brickMoveSpeed;

        StickToGridLine(ref destPos);

        while (destPos != brickPos)
        {
            var translation = destPos - brickPos;

            translation.x = Mathf.Clamp(translation.x, -moveSpeed, moveSpeed);
            translation.z = Mathf.Clamp(translation.z, -moveSpeed, moveSpeed);

            brickPos = brickPos + translation;

            yield return null;
        }
    }

    private void StickToUnderlaidBricks(GameObject go, ref Vector3 pos)
    {
        Bounds aabb = go.GetComponent<Brick>().AABB;
        Vector3 raycastStartPos = pos + Vector3.up * 100;

        RaycastHit[] hits = Physics.BoxCastAll(raycastStartPos, aabb.extents,
            Vector3.down, go.transform.rotation, Mathf.Infinity);

        float height = 0;
        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.gameObject == go)
                continue;

            if (hit.transform.gameObject == terrainMesh)
                continue;

            if (height < hit.point.y)
                height = hit.point.y;
        }

        pos.y = height + aabb.extents.y;
    }

    void LateUpdate()
    {
        if (controlMode != eControlMode.CM_COMPOSE)
            return;

        if (IsMouseButtonClicked(0))
        {
            NextStage();
            return;
        }
        else if (IsMouseButtonClicked(1))
        {
            PreviousStage();
            return;
        }

        if (curBrick != null)
        {
            Vector3 candidatePos;

            if (GetBrickTargetPos(curBrick, out candidatePos))
            {
                StartCoroutine("DragBrick", candidatePos);

                StickToUnderlaidBricks(curBrick, ref brickPos);

                if (curBrick.GetComponent<Brick>().IsNearlyPositioned(brickPos))
                    curBrick.GetComponent<Brick>().RestoreTransform();
                else
                    curBrick.transform.position = brickPos;
            }
        }
    }

    private void Awake()
    {
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
            if (Input.GetKeyDown(KeyCode.S))
            {
                StartStage();
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                EndStage();
            }
            else if (Input.GetKeyDown(KeyCode.N))
            {
                NextStage();
            }
            else if (Input.GetKeyDown(KeyCode.P))
            {
                PreviousStage();
            }
        }
    }
}
