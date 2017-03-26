using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrickController : MonoBehaviour
{
    public GameObject terrainMesh;

    GameObject headBrick = null;
    GameObject curBrick = null;
    GameObject lastBrick = null;

    public VirtualJoyStick joyStickLeft;
    public VirtualJoyStick joyStickRight;

    private const float brickMoveSpeed = 2.0f;
    private Vector3 destination;
    private bool isMoving = false;

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

    private void GetGridAlignedDelta(ref Vector3 dir)
    {
        if (dir.x > 0)
            dir.x = Mathf.Floor(dir.x / LdConstant.LDU_IN_MM) * LdConstant.LDU_IN_MM;
        else if (dir.x < 0)
            dir.x = Mathf.Ceil(dir.x / LdConstant.LDU_IN_MM) * LdConstant.LDU_IN_MM;

        if (dir.z > 0)
            dir.z = Mathf.Floor(dir.z / LdConstant.LDU_IN_MM) * LdConstant.LDU_IN_MM;
        else if (dir.z < 0)
            dir.z = Mathf.Ceil(dir.z / LdConstant.LDU_IN_MM) * LdConstant.LDU_IN_MM;
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

    IEnumerator DragBrick()
    {
        float moveSpeed = Time.deltaTime * brickMoveSpeed;
        Vector3 brickPos = curBrick.transform.position;

        var translation = destination - brickPos;
        GetGridAlignedDelta(ref translation);

        Vector3 destPos = brickPos + translation;

        while (isMoving && destPos != brickPos)
        {
            translation = destPos - brickPos;

            translation.x = Mathf.Clamp(translation.x, -moveSpeed, moveSpeed);
            translation.z = Mathf.Clamp(translation.z, -moveSpeed, moveSpeed);

            brickPos += translation;

            StickToUnderlaidBricks(curBrick, ref brickPos);
            curBrick.transform.position = brickPos;

            yield return null;
        }

        destination = brickPos;

        if (curBrick.GetComponent<Brick>().IsNearlyPositioned(brickPos))
            curBrick.GetComponent<Brick>().RestoreTransform();
    }

    void MoveBrick()
    {
        if (joyStickLeft.InputDirection.x != 0 || joyStickLeft.InputDirection.y != 0)
        {
            isMoving = true;

            Vector3 deltaPos;

            deltaPos.x = joyStickLeft.InputDirection.x;
            deltaPos.z = joyStickLeft.InputDirection.y;
            deltaPos.y = 0;

            destination += deltaPos;

            StartCoroutine(DragBrick());
        }
        else
        {
            isMoving = false;
        }
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

    void LateUpdate()
    {
        if (curBrick == null)
            return;

        MoveBrick();
    }
}
