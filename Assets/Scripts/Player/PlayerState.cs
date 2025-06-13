public abstract class PlayerState : State
{
    protected Player _player;
    public Player Player { get { return _player; } }

    public PlayerState(Player player, string animationBoolName = "") : base(animationBoolName)
    {
        _player = player;
    }

    public override bool IsMatchingConditions()
    {
        return false;
    }

    public override void Enter()
    {
        base.Enter();
        _player?.Anim.SetBool(AnimationBoolName, true);
    }


    public override void Exit()
    {
        base.Exit();
        _player?.Anim.SetBool(AnimationBoolName, false);
    }
}