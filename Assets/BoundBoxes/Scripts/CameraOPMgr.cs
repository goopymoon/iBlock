using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum eCameraOpTansition
{
    NA = 0,

    PIVOT_RESET,
    PIVOT_TOUCH,
    PIVOT_CONFIRM,

    ROT_RESET,
    ROT_TOUCH,
    ROT_RUN,

    ZOOM_RESET,
    ZOOM_RUN,
}

public enum eCameraOpState
{
    NA = 0,

    PIVOT_IDLE,
    PIVOT_SELECT,
    PIVOT_CONFIRM,

    ROT_IDLE,
    ROT_PREPARE,
    ROT_RUN,

    ZOOM_IDLE,
    ZOOM_RUN,
}

public class CameraOpMgr
{
    // pivot state
    public PivotIdle<eCameraOpTansition, eCameraOpState> sPivotIdle;
    public PivotSelect<eCameraOpTansition, eCameraOpState> sPivotSelect;
    public PivotConfirm<eCameraOpTansition, eCameraOpState> sPivotConfirm;
    // rot state
    public RotIdle<eCameraOpTansition, eCameraOpState> sRotIdle;
    public RotPrepare<eCameraOpTansition, eCameraOpState> sRotPrepare;
    public RotRun<eCameraOpTansition, eCameraOpState> sRotRun;
    // zoom state
    public ZoomIdle<eCameraOpTansition, eCameraOpState> sZoomIdle;
    public ZoomRun<eCameraOpTansition, eCameraOpState> sZoomRun;

    public FSMSystem<eCameraOpTansition, eCameraOpState> curPivotFsm;
    public FSMSystem<eCameraOpTansition, eCameraOpState> curRotFsm;
    public FSMSystem<eCameraOpTansition, eCameraOpState> curZoomFsm;

    public TrackballCamera container;
    private GameObject skipObj;

    // camera operation data
    public PivotInfo pivotInfo;
    public ZoomInfo zoomInfo;
    public RotationInfo rotInfo;

    public CameraOpMgr(TrackballCamera cam, GameObject pickingSkipObj)
    {
        container = cam;
        skipObj = pickingSkipObj;

        pivotInfo = new PivotInfo();
        zoomInfo = new ZoomInfo(Vector3.Distance(cam.transform.position, pivotInfo.pivotPos));
        rotInfo = new RotationInfo();

        InitFSM();
    }

    void InitFSMStates()
    {
        // pivot state
        sPivotIdle = new PivotIdle<eCameraOpTansition, eCameraOpState>(eCameraOpState.PIVOT_IDLE, this);
        sPivotSelect = new PivotSelect<eCameraOpTansition, eCameraOpState>(eCameraOpState.PIVOT_SELECT, this);
        sPivotConfirm = new PivotConfirm<eCameraOpTansition, eCameraOpState>(eCameraOpState.PIVOT_CONFIRM, this);
        // rotation state
        sRotIdle = new RotIdle<eCameraOpTansition, eCameraOpState>(eCameraOpState.ROT_IDLE, this);
        sRotPrepare = new RotPrepare<eCameraOpTansition, eCameraOpState>(eCameraOpState.ROT_PREPARE, this);
        sRotRun = new RotRun<eCameraOpTansition, eCameraOpState>(eCameraOpState.ROT_RUN, this);
        // zoom state
        sZoomIdle = new ZoomIdle<eCameraOpTansition, eCameraOpState>(eCameraOpState.ZOOM_IDLE, this);
        sZoomRun = new ZoomRun<eCameraOpTansition, eCameraOpState>(eCameraOpState.ZOOM_RUN, this);

        // add pivot transition
        sPivotIdle.AddTransition(eCameraOpTansition.PIVOT_RESET, sPivotIdle.ID);
        sPivotIdle.AddTransition(eCameraOpTansition.PIVOT_TOUCH, sPivotSelect.ID);
        sPivotSelect.AddTransition(eCameraOpTansition.PIVOT_RESET, sPivotIdle.ID);
        sPivotSelect.AddTransition(eCameraOpTansition.PIVOT_TOUCH, sPivotSelect.ID);
        sPivotSelect.AddTransition(eCameraOpTansition.PIVOT_CONFIRM, sPivotConfirm.ID);
        sPivotConfirm.AddTransition(eCameraOpTansition.PIVOT_RESET, sPivotIdle.ID);
        sPivotConfirm.AddTransition(eCameraOpTansition.PIVOT_TOUCH, sPivotSelect.ID);
        // add rotation transition
        sRotIdle.AddTransition(eCameraOpTansition.ROT_RESET, sRotIdle.ID);
        sRotIdle.AddTransition(eCameraOpTansition.ROT_TOUCH, sRotPrepare.ID);
        sRotPrepare.AddTransition(eCameraOpTansition.ROT_RESET, sRotIdle.ID);
        sRotPrepare.AddTransition(eCameraOpTansition.ROT_TOUCH, sRotPrepare.ID);
        sRotPrepare.AddTransition(eCameraOpTansition.ROT_RUN, sRotRun.ID);
        sRotRun.AddTransition(eCameraOpTansition.ROT_RESET, sRotIdle.ID);
        sRotRun.AddTransition(eCameraOpTansition.ROT_TOUCH, sRotRun.ID);
        sRotRun.AddTransition(eCameraOpTansition.ROT_RUN, sRotRun.ID);
        // add zoom transition
        sZoomIdle.AddTransition(eCameraOpTansition.ZOOM_RESET, sZoomIdle.ID);
        sZoomIdle.AddTransition(eCameraOpTansition.ZOOM_RUN, sZoomRun.ID);
        sZoomRun.AddTransition(eCameraOpTansition.ZOOM_RESET, sZoomIdle.ID);
        sZoomRun.AddTransition(eCameraOpTansition.ZOOM_RUN, sZoomRun.ID);
    }

    void InitFSMSystems()
    {
        curPivotFsm = new FSMSystem<eCameraOpTansition, eCameraOpState>();
        curRotFsm = new FSMSystem<eCameraOpTansition, eCameraOpState>();
        curZoomFsm = new FSMSystem<eCameraOpTansition, eCameraOpState>();

        // register pivot state
        curPivotFsm.AddState(sPivotIdle);
        curPivotFsm.AddState(sPivotSelect);
        curPivotFsm.AddState(sPivotConfirm);

        // register rotation state
        curRotFsm.AddState(sRotIdle);
        curRotFsm.AddState(sRotPrepare);
        curRotFsm.AddState(sRotRun);

        // register zoom state
        curZoomFsm.AddState(sZoomIdle);
        curZoomFsm.AddState(sZoomRun);
    }

    void InitFSM()
    {
        InitFSMStates();
        InitFSMSystems();
    }

    public void Update(float dt)
    {
        curPivotFsm.Update(dt);
        curRotFsm.Update(dt);
        curZoomFsm.Update(dt);
    }

    GameObject GetPickedBrick(Touch touch)
    {
        RaycastHit[] hits = Physics.RaycastAll(Camera.main.ScreenPointToRay(touch.position));

        List<GameObject> candidates = new List<GameObject>();
        Bounds bounds = new Bounds();

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.gameObject != skipObj)
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

    public bool CanRotate()
    {
        return (curRotFsm.CurrentStateID() == sRotRun.ID);
    }

    public void ConfirmPivot()
    {
        container.ApplyRefreshingPivot();
    }

    public void StopRotation()
    {
        curRotFsm.Advance(eCameraOpTansition.ROT_RESET);
    }
    public void StopZoom()
    {
        curZoomFsm.Advance(eCameraOpTansition.ZOOM_RESET);
    }

    public void DoRotation()
    {
        container.ApplyRotation();
        rotInfo.RefreshLastTouchPos();
    }

    public void DoZoom()
    {
        float dist;

        if (zoomInfo.CalZoomDist(out dist))
            container.ApplyZoom(dist);
    }

    void HandlePivotTouch(Touch touch)
    {
        GameObject obj = GetPickedBrick(touch);

        if (obj)
        {
            pivotInfo.candObj = obj;
            curPivotFsm.Advance(eCameraOpTansition.PIVOT_TOUCH);
        }
    }

    public void HandleOneTouch(Touch touch)
    {
        rotInfo.SetCurTouchPos(touch.position);

        switch (touch.phase)
        {
            case TouchPhase.Began:
                HandlePivotTouch(touch);
                curRotFsm.Advance(eCameraOpTansition.ROT_TOUCH);
                break;
            case TouchPhase.Stationary:
                curRotFsm.Advance(eCameraOpTansition.ROT_TOUCH);
                break;
            case TouchPhase.Ended:
                curRotFsm.Advance(eCameraOpTansition.ROT_RESET);
                break;
        }
    }

    public void HandleTwoTouch(Touch touch1, Touch touch2)
    {
        curRotFsm.Advance(eCameraOpTansition.ROT_RESET);
        curPivotFsm.Advance(eCameraOpTansition.PIVOT_RESET);

        // if at least one of them moved
        if (touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved)
        {
            zoomInfo.SetTouch(touch1, touch2);
            curZoomFsm.Advance(eCameraOpTansition.ZOOM_RUN);
        }
    }
}
