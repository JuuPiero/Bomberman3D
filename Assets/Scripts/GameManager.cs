using System;
using System.Collections;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using NetPlayer = Photon.Realtime.Player;

public enum GameMode { Story, Battle }
public enum MatchPhase { Waiting, Countdown, Playing, Ended }

public struct MatchResult
{
    public bool IsGameOver;   // Story: no lives left
    public bool IsDraw;       // Battle: nobody (or everybody) survived / time ran out
    public int WinnerActor;
    public string WinnerName;
    public bool LocalWon;
}

/// <summary>
/// Runs a level. Story mode (offline room): timer, score and lives like before.
/// Battle mode (online room): players spawn in the corners, a synced countdown starts the round and
/// the master client decides the winner, then brings everybody back to the room.
/// </summary>
public class GameManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public static GameManager Instance { get; private set;}
    [field: SerializeField] public GameDataSO Data { get; private set; }

    [Header("Spawning")]
    [SerializeField] private string _playerPrefabName = "Player"; // must be inside a Resources folder
    [SerializeField] private Transform _storySpawn;
    [Tooltip("Optional. Empty = the four corners of the map.")]
    [SerializeField] private Transform[] _battleSpawns;

    [Header("Battle")]
    [SerializeField] private float _battleTime = 180f;
    [SerializeField] private float _countdownTime = 3f;
    [SerializeField] private float _loadTimeout = 10f;
    [SerializeField] private float _resultsTime = 6f;

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

    public GameMode Mode { get; private set; }
    public MatchPhase Phase { get; private set; } = MatchPhase.Waiting;
    public event Action OnPhaseChanged;
    public event Action<MatchResult> OnMatchEnded;
    public event Action<Player> OnAnyPlayerDied;

    public Player LocalPlayer { get; private set; }
    public double RoundStartTime { get; private set; }
    public float CountdownLeft => Phase == MatchPhase.Countdown ? Mathf.Max(0f, (float)(RoundStartTime - PhotonNetwork.Time)) : 0f;
    public float ResultsTimeLeft => Mathf.Max(0f, _resultsEndTime - Time.time);
    public bool InputBlocked { get; set; }
    public bool CanControl => Phase == MatchPhase.Playing && !InputBlocked;

    private float _phaseStartTime;
    private float _resultsEndTime;
    private int _startPlayerCount;
    private bool _roundStartSent;
    private bool _roundEndSent;
    private Coroutine _roundCheck;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (!PhotonNetwork.InRoom)
            NetworkController.Instance.EnsureOfflineRoom(); // scene played directly from the editor

        // Known from Awake so other scripts can read it in their Start.
        Mode = PhotonNetwork.OfflineMode ? GameMode.Story : GameMode.Battle;
        _enemyCount = FindObjectsByType<Enemy>(FindObjectsSortMode.None).Length;
    }

    void Start()
    {
        _phaseStartTime = Time.time;
        if (BombManager.Instance == null)
            Debug.LogError("[GameManager] No BombManager in the scene. Run Tools > Bomberman > Setup UI Toolkit + Multiplayer.", this);

        if (Mode == GameMode.Story) PrepareStory();
        else PrepareBattle();

        SpawnLocalPlayer();
    }

    void Update()
    {
        switch (Phase)
        {
            case MatchPhase.Waiting:
                if (Mode == GameMode.Battle && PhotonNetwork.IsMasterClient && !_roundStartSent &&
                    (Player.All.Count >= PhotonNetwork.CurrentRoom.PlayerCount || Time.time - _phaseStartTime > _loadTimeout))
                {
                    _roundStartSent = true;
                    NetEvents.Raise(NetEvents.RoundStart, PhotonNetwork.Time + _countdownTime, ReceiverGroup.All);
                }
                break;

            case MatchPhase.Countdown:
                if (PhotonNetwork.Time >= RoundStartTime) SetPhase(MatchPhase.Playing);
                break;

            case MatchPhase.Playing:
                if (Mode == GameMode.Story) UpdateGameTime();
                else UpdateBattleTime();
                break;
        }
    }

    #region Story

    void PrepareStory()
    {
        if (NetworkController.StoryNewGame)
        {
            NetworkController.StoryNewGame = false;
            Data.ResetData();
        }

        _timeLeft = _maxTime;
        Score = 0;
        _lifeCount = Data.lifeCountLeft;
        if (_lifeCount <= 0)
        {
            _lifeCount = LIFE_COUNT_RESET;
            Data.lifeCountLeft = LIFE_COUNT_RESET;
        }
        SetPhase(MatchPhase.Playing);
    }

    private void UpdateGameTime()
    {
        if (_isGameOver) return;
        TimeLeft = Mathf.Max(0f, TimeLeft - Time.deltaTime);
        if (TimeLeft <= 0)
        {
            GameOver();
        }
    }

    /// <summary>Story: time is up or the player died.</summary>
    public void GameOver()
    {
        if (_isGameOver) return;
        _isGameOver = true;

        if (LocalPlayer != null && !LocalPlayer.isDead) LocalPlayer.Kill(); // OnPlayerDied continues
        else StartCoroutine(LoseLifeAfter(2f));
    }

    public void NextStage()
    {

    }

    public void IncreaseScore(int score)
    {
        Score += score;
        _enemyCount--;
    }

    IEnumerator LoseLifeAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        ReduceLifeCount();
    }

    void ReduceLifeCount()
    {
        _lifeCount--;
        Data.lifeCountLeft = _lifeCount;
        if (_lifeCount <= 0)
        {
            SetPhase(MatchPhase.Ended);
            OnMatchEnded?.Invoke(new MatchResult { IsGameOver = true });
            return;
        }
        ResetStage();
    }

    void ResetStage()
    {
        PhotonNetwork.LoadLevel(SceneManager.GetActiveScene().name);
    }

    /// <summary>Story game over screen: start again from full lives.</summary>
    public void RestartStory()
    {
        Data.ResetData();
        ResetStage();
    }

    #endregion

    #region Battle

    void PrepareBattle()
    {
        TimeLeft = _battleTime;

        // Battle is player vs player: no enemies and no exit portal.
        foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            Destroy(enemy.gameObject);
        _enemyCount = 0;
        ItemManager.Instance?.DisablePortal();

        // Same on every client, so the map stays identical.
        foreach (Vector2Int corner in GridManager.Instance.GetCornerCells())
            GridManager.Instance.ClearBricksAround(corner, 2);
    }

    void UpdateBattleTime()
    {
        TimeLeft = Mathf.Max(0f, _battleTime - (float)(PhotonNetwork.Time - RoundStartTime));
        if (TimeLeft <= 0f && PhotonNetwork.IsMasterClient)
            EndRound(-1);
    }

    /// <summary>Master only: the round ends when at most one player is alive (or nobody, in a solo test).</summary>
    void CheckRoundEnd()
    {
        if (Mode != GameMode.Battle || Phase != MatchPhase.Playing || !PhotonNetwork.IsMasterClient) return;

        int alive = 0;
        Player survivor = null;
        foreach (Player player in Player.All)
        {
            if (player.isDead) continue;
            alive++;
            survivor = player;
        }

        bool solo = _startPlayerCount <= 1;
        if (alive == 0 || (!solo && alive == 1))
            EndRound(alive == 1 ? survivor.ActorNumber : -1);
    }

    void ScheduleRoundCheck()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (_roundCheck != null) StopCoroutine(_roundCheck);
        _roundCheck = StartCoroutine(RoundCheckAfter(1.5f));
    }

    IEnumerator RoundCheckAfter(float delay)
    {
        // Small grace period: players caught in the same explosion report their death at slightly
        // different times, and should end in a draw rather than a win for whoever reported last.
        yield return new WaitForSeconds(delay);
        _roundCheck = null;
        CheckRoundEnd();
    }

    void EndRound(int winnerActor)
    {
        if (_roundEndSent) return;
        _roundEndSent = true;
        NetEvents.Raise(NetEvents.RoundEnd, winnerActor, ReceiverGroup.All);
    }

    IEnumerator ReturnToRoomAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (PhotonNetwork.IsMasterClient) NetworkController.Instance.ReturnToRoom();
    }

    void ShowBattleResult(int winnerActor)
    {
        NetPlayer winner = winnerActor > 0 ? PhotonNetwork.CurrentRoom?.GetPlayer(winnerActor) : null;
        var result = new MatchResult
        {
            IsDraw = winner == null,
            WinnerActor = winnerActor,
            WinnerName = winner != null ? winner.NickName : "",
            LocalWon = winner != null && winner.IsLocal,
        };

        _resultsEndTime = Time.time + _resultsTime;
        SetPhase(MatchPhase.Ended);
        OnMatchEnded?.Invoke(result);

        if (PhotonNetwork.IsMasterClient) StartCoroutine(ReturnToRoomAfter(_resultsTime));
    }

    #endregion

    #region Players

    void SpawnLocalPlayer()
    {
        int slot = 0;
        if (Mode == GameMode.Battle)
        {
            slot = PhotonNetwork.LocalPlayer.GetSlot();
            if (slot < 0) slot = Array.IndexOf(PhotonNetwork.PlayerList, PhotonNetwork.LocalPlayer);
            slot = Mathf.Clamp(slot, 0, NetworkController.MaxRoomPlayers - 1);
        }

        PhotonNetwork.Instantiate(_playerPrefabName, GetSpawnPosition(slot), Quaternion.identity, 0, new object[] { slot });
    }

    Vector3 GetSpawnPosition(int slot)
    {
        if (Mode == GameMode.Story && _storySpawn != null)
            return _storySpawn.position;

        if (Mode == GameMode.Battle && _battleSpawns != null && slot < _battleSpawns.Length && _battleSpawns[slot] != null)
            return _battleSpawns[slot].position;

        Vector2Int[] corners = GridManager.Instance.GetCornerCells();
        Vector3 pos = GridManager.Instance.CellToWorld(corners[slot % corners.Length]);
        pos.y = 3f;
        return pos;
    }

    /// <summary>Called by every player (local and remote) once it is ready.</summary>
    public void OnPlayerSpawned(Player player)
    {
        if (!player.IsLocal) return;
        LocalPlayer = player;
        FollowWithCamera(player.transform);
    }

    /// <summary>Called on every client when any player dies.</summary>
    public void OnPlayerDied(Player player)
    {
        OnAnyPlayerDied?.Invoke(player);

        if (Mode == GameMode.Story)
        {
            if (player.IsLocal)
            {
                _isGameOver = true;
                StartCoroutine(LoseLifeAfter(3f));
            }
            return;
        }

        if (player.IsLocal) StartCoroutine(SpectateAfter(2f));
        ScheduleRoundCheck();
    }

    IEnumerator SpectateAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        foreach (Player player in Player.All)
        {
            if (player.isDead) continue;
            FollowWithCamera(player.transform);
            yield break;
        }
    }

    public Player GetClosestAlivePlayer(Vector3 position)
    {
        Player closest = null;
        float best = float.MaxValue;
        foreach (Player player in Player.All)
        {
            if (player.isDead) continue;
            float distance = (player.transform.position - position).sqrMagnitude;
            if (distance < best)
            {
                best = distance;
                closest = player;
            }
        }
        return closest;
    }

    static void FollowWithCamera(Transform target)
    {
        CinemachineCamera cam = FindFirstObjectByType<CinemachineCamera>();
        if (cam == null) return;
        cam.Follow = target;
        cam.PreviousStateIsValid = false;
    }

    #endregion

    void SetPhase(MatchPhase phase)
    {
        if (Phase == phase) return;
        Phase = phase;
        _phaseStartTime = Time.time;
        OnPhaseChanged?.Invoke();
    }

    public void OnEvent(EventData photonEvent)
    {
        switch (photonEvent.Code)
        {
            case NetEvents.RoundStart:
                if (Phase != MatchPhase.Waiting) return;
                RoundStartTime = (double)photonEvent.CustomData;
                _startPlayerCount = PhotonNetwork.CurrentRoom.PlayerCount;
                SetPhase(MatchPhase.Countdown);
                break;

            case NetEvents.RoundEnd:
                if (Phase == MatchPhase.Ended) return;
                ShowBattleResult((int)photonEvent.CustomData);
                break;
        }
    }

    public override void OnPlayerLeftRoom(NetPlayer otherPlayer)
    {
        // PUN destroys the leaver's player object; check whether that ends the round.
        if (Mode == GameMode.Battle) ScheduleRoundCheck();
    }

    public override void OnMasterClientSwitched(NetPlayer newMasterClient)
    {
        if (Mode != GameMode.Battle || !PhotonNetwork.IsMasterClient) return;

        // Take over the old master's duties.
        if (Phase == MatchPhase.Playing) ScheduleRoundCheck();
        else if (Phase == MatchPhase.Ended) StartCoroutine(ReturnToRoomAfter(Mathf.Max(1f, ResultsTimeLeft)));
    }
}
