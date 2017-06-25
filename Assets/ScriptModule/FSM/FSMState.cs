using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class FSMState<Transition, StateID>
{
    protected Dictionary<Transition, StateID> map = new Dictionary<Transition, StateID>();

    public StateID ID { get { return stateID; } }
    protected StateID stateID;

    protected float elapsedTime { get; private set; }
    protected bool blocked { get; private set; }
    protected virtual bool IsAllowReEnter() { return false; }

    public FSMState(StateID id)
    {
        stateID = id;
        elapsedTime = 0;
        blocked = false;
    }

    public void AddTransition(Transition trans, StateID id)
    {
        if (map.ContainsKey(trans))
        {
            Debug.LogError("FSMState ERROR: State " + stateID 
                + " already has transition " + trans 
                + "Impossible to assign to another state");
            return;
        }

        map.Add(trans, id);
    }

    public void DeleteTransition(Transition trans)
    {
        if (map.ContainsKey(trans))
        {
            map.Remove(trans);
            return;
        }

        Debug.LogError("FSMState ERROR: Transition " + trans 
            + " passed to " + stateID 
            + " was not on the state's transition list");
    }

    public void Block()
    {
        blocked = true;
    }

    public void Unblock()
    {
        blocked = false;
    }

    public bool TryTransition(Transition trans, out StateID id)
    {
        id = default(StateID);

        if (!blocked)
        {
            if (map.ContainsKey(trans))
            {
                id = map[trans];
                return IsAllowReEnter() ? true : (!stateID.Equals(id));
            }

            Debug.LogError("FSM ERROR: State " + ToString()
                + " does not have a target state "
                + " for transition " + trans);
        }

        return false;
    }

    public virtual void OnEnter()
    {
        elapsedTime = 0.0f;
        Debug.Log(string.Format("OnEnter: {0}", stateID));
    }

    public virtual void Do() {}

    public virtual void OnExit()
    {
        Debug.Log(string.Format("OnExit: {0} {1} sec", stateID, elapsedTime));
    }

    public virtual void Update(float dt)
    {
        elapsedTime += dt;
    }

} // class FSMState

