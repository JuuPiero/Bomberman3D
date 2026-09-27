public class PlayerDieState : PlayerState
{
    // What happens after a death (lose a life, end the round...) is handled by GameManager.
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
