using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerDieState : PlayerState
{
    public PlayerDieState(Player player, string animationBoolName = "") : base(player, animationBoolName)
    {
        _player.OnPlayerDeath += PlayerDie;
    }
    ~PlayerDieState() {
        _player.OnPlayerDeath -= PlayerDie;
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

    void PlayerDie() {
        _player.StartCoroutine(DieCO());
    }

    IEnumerator DieCO()
    {
        yield return new WaitForSeconds(4f);
        SceneManager.LoadScene("MainMenu");
        // Open Panel Over
    }
}