using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TMP_Text _timeText;
    [SerializeField] private TMP_Text _scoreText;
    [SerializeField] private TMP_Text _lifeCountText;


    void OnEnable()
    {
        GameManager.Instance.OnTimeChanged += UpdateTime;
        GameManager.Instance.OnScoreChanged += UpdateScore;
    }

    void OnDisable()
    {
        GameManager.Instance.OnTimeChanged -= UpdateTime;
        GameManager.Instance.OnScoreChanged -= UpdateScore;
    }

    void Start()
    {
        _lifeCountText.text = GameManager.Instance.LifeCount.ToString();
    }

    void UpdateTime()
    {
        _timeText.text = "Time: " + Mathf.RoundToInt(GameManager.Instance.TimeLeft);
    }

    void UpdateScore()
    {
        _scoreText.text = GameManager.Instance.Score.ToString();
    }

}