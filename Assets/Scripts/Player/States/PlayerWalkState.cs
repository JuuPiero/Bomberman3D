
using UnityEngine;

public class PlayerWalkState: PlayerState
{
    public PlayerWalkState(Player player, string animationBoolName = "") : base(player, animationBoolName)
    {
    }

    public override bool IsMatchingConditions()
    {
        return _player.InputDirection != Vector3.zero;
    }
}