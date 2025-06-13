using System;

public abstract class State 
{
    public StateMachine StateMachine { get; set; }
    public string AnimationBoolName {get; protected set; } // is bool param
    public float ExitTime { get; protected set; } = 1.0f;
    public bool CanExit { get; set; } = true;

    public void AnimationFinished() 
    {
        CanExit = true;
    }

    public State(string animationBoolName = "") 
    {
        AnimationBoolName = animationBoolName;
    }

    public virtual void Enter()
    {
    }
   

    public virtual void Exit() 
    {
        StateMachine.PrevState = this;
    }

    public virtual void Update() 
    {
    }

    public virtual void FixedUpdate()
    {
    }

    public void AnimationTrigger() 
    {
    }
    public abstract bool IsMatchingConditions();
}