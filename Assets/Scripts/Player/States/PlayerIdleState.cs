
public class PlayerIdleState: PlayerState
{
    public PlayerIdleState(Player player, string animationBoolName = "") : base(player, animationBoolName)
    {
    }

    public override bool IsMatchingConditions()
    {
        return _player.Horizontal == 0f;
    }
}