using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        _player.StartCoroutine(Test());
    }

    IEnumerator Test()
    {
        yield return new WaitForSeconds(4f);
        SceneManager.LoadScene("MainMenu");
    }
}