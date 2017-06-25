using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraPivotState<Transition, StateID> : FSMState<Transition, StateID>
{
    public CameraPivotState(StateID id) : base(id) { }
}

public class PivotIdle<Transition, StateID> : CameraPivotState<Transition, StateID>
{
    CameraOpMgr owner;

    public override void Do()
    {
        owner.pivotInfo.Clear();
    }

    public PivotIdle(StateID id, CameraOpMgr owner_)
        : base(id)
    {
        owner = owner_;
    }
}

public class PivotSelect<Transition, StateID> : CameraPivotState<Transition, StateID>
{
    const float PickInterval = 1.0f;
    CameraOpMgr owner;

    protected override bool IsAllowReEnter() { return true; }

    public override void OnEnter()
    {
        base.OnEnter();

        owner.sRotPrepare.Block();
        owner.sRotRun.Block();
        owner.sZoomRun.Block();
    }
    public override void Do()
    {
        bool isConfirm;
        owner.pivotInfo.Select(out isConfirm);

        if (isConfirm)
            owner.curPivotFsm.Advance(eCameraOpTansition.PIVOT_CONFIRM);
    }
    public override void OnExit()
    {
        base.OnExit();

        owner.sRotPrepare.Unblock();
        owner.sRotRun.Unblock();
        owner.sZoomRun.Unblock();
    }
    public override void Update(float dt)
    {
        base.Update(dt);

        if (elapsedTime > PickInterval)
            owner.curPivotFsm.Advance(eCameraOpTansition.PIVOT_RESET);
    }

    public PivotSelect(StateID id, CameraOpMgr owner_)
        : base(id)
    {
        owner = owner_;
    }
}

public class PivotConfirm<Transition, StateID> : CameraPivotState<Transition, StateID>
{
    CameraOpMgr owner;

    public override void Do()
    {
        owner.ConfirmPivot();
    }

    public PivotConfirm(StateID id, CameraOpMgr owner_)
        : base(id)
    {
        owner = owner_;
    }
}
