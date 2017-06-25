using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FSMSystem<Transition, StateID>
{
    public StateID CurrentStateID() { return currentState.ID; }
    public FSMState<Transition, StateID> currentState { get; private set; }

    private Dictionary<StateID, FSMState<Transition, StateID>> states;

    public FSMSystem()
    {
        states = new Dictionary<StateID, FSMState<Transition, StateID>>();
    }

    public void AddState(FSMState<Transition, StateID> s)
    {
        if (s == null)
        {
            Debug.LogError("FSM ERROR: Null reference is not allowed");
        }

        if (states.Count == 0)
        {
            states.Add(s.ID, s);
            currentState = s;
            return;
        }

        if (states.ContainsKey(s.ID))
        {
            Debug.LogError("FSMSystem ERROR: State " + s.ID + " already exits.");
            return;
        }

        states.Add(s.ID, s);
    }

    public void RemoveState(StateID id)
    {
        if (states.ContainsKey(id))
        {
            states.Remove(id);
            return;
        }

        Debug.LogError("FSMSystem ERROR: Impossible to delete state " + id 
            + ". It was not on the list of states");
    }

    public void Advance(Transition trans)
    {
        StateID id;
        
        if (currentState.TryTransition(trans, out id))
        {
            // Reset the state to its desired condition before it can reason or act
            currentState.OnExit();

            // Update the currentState		
            currentState = states[id];
            currentState.OnEnter();
            currentState.Do();
        }
    }

    public void Update(float dt)
    {
        currentState.Update(dt);
    }

} //class FSMSystem
