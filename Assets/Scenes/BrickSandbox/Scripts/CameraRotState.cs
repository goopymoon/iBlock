using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraRotState<Transition, StateID> : FSMState<Transition, StateID>
{
    public CameraRotState(StateID id) : base(id) { }
}

public class RotIdle<Transition, StateID> : CameraRotState<Transition, StateID>
{
    CameraOpMgr owner;

    public override void Do()
    {
        owner.rotInfo.Clear();
    }
    public RotIdle(StateID id, CameraOpMgr owner_)
        : base(id)
    {
        owner = owner_;
    }
}

public class RotPrepare<Transition, StateID> : CameraRotState<Transition, StateID>
{
    const float RotInterval = 0.2f;
    CameraOpMgr owner;

    public override void Update(float dt)
    {
        base.Update(dt);

        if (elapsedTime > RotInterval)
        {
            owner.curRotFsm.Advance(eCameraOpTansition.ROT_RUN);
        }
    }
    public RotPrepare(StateID id, CameraOpMgr owner_)
        : base(id)
    {
        owner = owner_;
    }
}

public class RotRun<Transition, StateID> : CameraRotState<Transition, StateID>
{
    CameraOpMgr owner;

    public override void Update(float dt)
    {
        base.Update(dt);
        owner.DoRotation();
    }
    public RotRun(StateID id, CameraOpMgr owner_)
        : base(id)
    {
        owner = owner_;
    }
}

