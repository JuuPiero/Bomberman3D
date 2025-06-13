using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[Serializable]
public class StateMachine {

    public event Action OnStateChange;
    protected List<State> states = new List<State>();

    public State CurrentState { get; set; }
    public State PrevState { get; set; }

    public void Initialize(State state = null) {
        CurrentState = state ?? states.First();
        PrevState = CurrentState;
        CurrentState.Enter();
    }

    public void ChangeState(State newState)
    {
        if (CurrentState == newState) return;
        CurrentState?.Exit();
        CurrentState = newState;
        CurrentState.Enter();
        OnStateChange?.Invoke();
    }

  
    public void AddState(State state)
    {
        state.StateMachine ??= this;
        // states.Add(state);
        states.Insert(0, state);
    }

    public void Update() 
    {
        if (CurrentState == null)
        {
            Debug.Log("Current State is null");
            return;
        }

        if (CurrentState.CanExit)  // Chỉ tìm state mới nếu state hiện tại cho phép
        {
            foreach (State state in states)
            {
                if (state.IsMatchingConditions())
                {
                    ChangeState(state);
                    break;
                }
            }
        }
        CurrentState.Update(); 
    }

    public void FixedUpdate() {
        CurrentState?.FixedUpdate();
    }

    public bool CurrentIs<T>() where T : State
    {
       return CurrentState.GetType() == typeof(T);
    }
    public State GetState<T>() where T : State
    {
        var state = states.Where(s => s.GetType() == typeof(T)).FirstOrDefault();
        return state != null ? state : null;
    }

}