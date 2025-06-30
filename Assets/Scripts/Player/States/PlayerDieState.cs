public class PlayerDieState : PlayerState
{
    public PlayerDieState(Player player, string animationBoolName = "") : base(player, animationBoolName)
    {
    }

    public override bool IsMatchingConditions()
    {
        return _player.isDead;
    }
    public override void Enter()
    {
        base.Enter();
        CanExit = false;
    }
}