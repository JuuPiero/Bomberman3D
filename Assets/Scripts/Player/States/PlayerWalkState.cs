
public class PlayerWalkState: PlayerState
{
    public PlayerWalkState(Player player, string animationBoolName = "") : base(player, animationBoolName)
    {
    }

    public override bool IsMatchingConditions()
    {
        return _player.InputDirection.x != 0.0f ||  _player.InputDirection.z != 0.0f;
    }
}