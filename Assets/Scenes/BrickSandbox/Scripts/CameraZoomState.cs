using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraZoomState<Transition, StateID> : FSMState<Transition, StateID>
{
    public CameraZoomState(StateID id) : base(id) { }
}

public class ZoomIdle<Transition, StateID> : CameraZoomState<Transition, StateID>
{
    CameraOpMgr owner;

    public override void Do()
    {
        owner.zoomInfo.ClearTouch();
    }
    public ZoomIdle(StateID id, CameraOpMgr owner_)
        : base(id)
    {
        owner = owner_;
    }
}

public class ZoomRun<Transition, StateID> : CameraZoomState<Transition, StateID>
{
    CameraOpMgr owner;

    public override void Update(float dt)
    {
        base.Update(dt);
        owner.DoZoom();
    }
    public ZoomRun(StateID id, CameraOpMgr owner_)
        : base(id)
    {
        owner = owner_;
    }
}

