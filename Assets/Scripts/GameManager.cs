using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set;}
    [field: SerializeField] public GameDataSO Data { get; private set; }

    [SerializeField] private int _score = 0;
    public event Action OnScoreChanged;
    public int Score
    {
        get => _score;
        set
        {
            _score = value;
            OnScoreChanged?.Invoke();
        }
    }

    public event Action OnTimeChanged;
    [SerializeField] private float _maxTime;
    [SerializeField] private float _timeLeft;

    public float TimeLeft
    {
        get => _timeLeft;
        set
        {
            _timeLeft = value;
            OnTimeChanged?.Invoke();
        }
    }

    const int LIFE_COUNT_RESET = 5;
    [SerializeField] private int _lifeCount;
    public int LifeCount => _lifeCount;

    [SerializeField] private bool _isGameOver = false;

    [SerializeField] private int _enemyCount;

    void Awake()
    {
        if (Instance == null || Instance != this)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        _enemyCount = FindObjectsByType<Enemy>(FindObjectsSortMode.None).Length;
    }

    void Start()
    {
        _timeLeft = _maxTime;
        Score = 0;
        _lifeCount = Data.lifeCountLeft;
        if (_lifeCount <= 0)
        {
            _lifeCount = LIFE_COUNT_RESET;
            Data.lifeCountLeft = LIFE_COUNT_RESET;
        }
    }

    void Update()
    {
        if (!_isGameOver)
        {
            UpdateGameTime();
        }
    }


    public void GameOver()
    {
        _isGameOver = true;
        ReduceLifeCount();
    }

    public void NextStage()
    {
      
    }

    private void UpdateGameTime()
    {
        TimeLeft -= Time.deltaTime;
        if (TimeLeft <= 0)
        {
            GameOver();
        }
    }

    public void IncreaseScore(int score)
    {
        Score += score;
        _enemyCount--;
    }

    void ReduceLifeCount()
    {
        Data.lifeCountLeft--;
        if (_lifeCount < 0)
        {
            SceneManager.LoadScene("MainMenu");
        }
        ResetStage();
    }

    void ResetStage()
    {
        Data.ResetData();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}