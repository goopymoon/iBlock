using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Transition : IEquatable<Transition>
{
    public string Name { private set; get; }
    public int Value { private set; get; }

    public Transition(int value, String name)
    {
        this.Name = name;
        this.Value = value;
    }

    public override String ToString()
    {
        return Name;
    }

    public bool Equals(Transition other)
    {
        return other.Name == this.Name && other.Value == this.Value;
    }
}

public class StateID : IEquatable<StateID>
{
    public string Name { private set; get; }
    public int Value { private set; get; }

    public StateID(int value, String name)
    {
        this.Name = name;
        this.Value = value;
    }

    public override String ToString()
    {
        return Name;
    }

    public bool Equals(StateID other)
    {
        return other.Name == this.Name && other.Value == this.Value;
    }
}

public class TransitionType
{
    public static readonly Transition NullTransition = new Transition(0, "NullTransition");
}

public class StateIDType
{
    public static readonly StateID NullStateID = new StateID(0, "NullStateID");
}

public abstract class FSMState
{
    protected Dictionary<Transition, StateID> map = new Dictionary<Transition, StateID>();
    protected StateID stateID;
    public StateID ID { get { return stateID; } }

    public void AddTransition(Transition trans, StateID id)
    {
        // Check if anyone of the args is invalid
        if (trans == TransitionType.NullTransition)
        {
            Debug.LogError("FSMState ERROR: NullTransition is not allowed for a real transition");
            return;
        }

        if (id == StateIDType.NullStateID)
        {
            Debug.LogError("FSMState ERROR: NullStateID is not allowed for a real ID");
            return;
        }

        // Since this is a Deterministic FSM,
        //   check if the current transition was already inside the map
        if (map.ContainsKey(trans))
        {
            Debug.LogError("FSMState ERROR: State " + stateID.ToString() + " already has transition " + trans.ToString() +
                           "Impossible to assign to another state");
            return;
        }

        map.Add(trans, id);
    }

    public void DeleteTransition(Transition trans)
    {
        // Check for NullTransition
        if (trans == TransitionType.NullTransition)
        {
            Debug.LogError("FSMState ERROR: NullTransition is not allowed");
            return;
        }

        // Check if the pair is inside the map before deleting
        if (map.ContainsKey(trans))
        {
            map.Remove(trans);
            return;
        }
        Debug.LogError("FSMState ERROR: Transition " + trans.ToString() + " passed to " + stateID.ToString() +
                       " was not on the state's transition list");
    }

    public StateID GetOutputState(Transition trans)
    {
        // Check if the map has this transition
        if (map.ContainsKey(trans))
        {
            return map[trans];
        }
        return StateIDType.NullStateID;
    }

    public virtual void OnEnter() { }
    public virtual void Do() { }
    public virtual void OnExit() { }

} // class FSMState

public class FSMSystem
{
    private Dictionary<StateID, FSMState> states;

    // The only way one can change the state of the FSM is by performing a transition
    // Don't change the CurrentState directly
    private StateID currentStateID;
    public StateID CurrentStateID { get { return currentStateID; } }
    private FSMState currentState;
    public FSMState CurrentState { get { return currentState; } }

    public FSMSystem()
    {
        states = new Dictionary<StateID, FSMState>();
    }

    public void AddState(FSMState s)
    {
        // Check for Null reference before deleting
        if (s == null)
        {
            Debug.LogError("FSM ERROR: Null reference is not allowed");
        }

        // First State inserted is also the Initial state,
        //   the state the machine is in when the simulation begins
        if (states.Count == 0)
        {
            states.Add(s.ID, s);
            currentState = s;
            currentStateID = s.ID;
            return;
        }

        if (states.ContainsKey(s.ID))
        {
            Debug.LogError("FSMSystem ERROR: State " + s.ID.ToString() + " already exits.");
            return;
        }

        states.Add(s.ID, s);
    }

    public void DeleteState(StateID id)
    {
        // Check for NullState before deleting
        if (id == StateIDType.NullStateID)
        {
            Debug.LogError("FSM ERROR: NullStateID is not allowed for a real state");
            return;
        }

        // Check if the pair is inside the map before deleting
        if (states.ContainsKey(id))
        {
            states.Remove(id);
            return;
        }
        Debug.LogError("FSMSystem ERROR: Impossible to delete state " + id.ToString() +
                       ". It was not on the list of states");
    }

    public void PerformTransition(Transition trans)
    {
        // Check for NullTransition before changing the current state
        if (trans == TransitionType.NullTransition)
        {
            Debug.LogError("FSM ERROR: NullTransition is not allowed for a real transition");
            return;
        }

        // Check if the currentState has the transition passed as argument
        StateID id = currentState.GetOutputState(trans);
        if (id == StateIDType.NullStateID)
        {
            Debug.LogError("FSM ERROR: State " + currentStateID.ToString() + " does not have a target state " +
                           " for transition " + trans.ToString());
            return;
        }

        // Do the post processing of the state before setting the new one
        currentState.OnEnter();

        // Update the currentStateID and currentState		
        currentStateID = id;
        currentState = states[id];
        currentState.Do();

        // Reset the state to its desired condition before it can reason or act
        currentState.OnExit();
    }

} //class FSMSystem
