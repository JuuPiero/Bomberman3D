using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThanhHoang.Bomberman.UI
{
    /// <summary>
    /// In-game HUD (UI Toolkit): timer, score/lives (Story), player list and name tags (Battle),
    /// local power-ups, countdown, pause menu and the end-of-round screen.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class HudUI : MonoBehaviour
    {
        [SerializeField] private float _nameTagHeight = 3.3f;

        VisualElement _root;
        Label _time, _score, _lives, _countdown, _banner;
        Label _statBombs, _statRange, _statSpeed;
        VisualElement _storyScore, _storyLives, _chips, _nameTagLayer;
        VisualElement _pause, _result;
        Label _pauseHint, _resultTitle, _resultSubtitle, _resultHint;
        Button _pauseQuit, _resultPrimary, _resultSecondary;

        readonly Dictionary<Player, Label> _nameTags = new();
        GameManager _gm;
        Player _statsPlayer;
        Camera _camera;
        bool _started;
        bool _paused;
        bool _hasResult;
        int _shownSeconds = -1;
        int _shownCountdown = -1;
        float _hideGoAt;

        #region Lifecycle

        void Start()
        {
            _started = true;
            Build();
        }

        void OnEnable()
        {
            if (_started) Build();
        }

        void OnDisable()
        {
            Player.Spawned -= OnPlayerSpawned;
            Player.Despawned -= OnPlayerDespawned;
            if (_gm != null)
            {
                _gm.OnTimeChanged -= UpdateTime;
                _gm.OnScoreChanged -= UpdateScore;
                _gm.OnPhaseChanged -= UpdatePhase;
                _gm.OnMatchEnded -= ShowResult;
                _gm.OnAnyPlayerDied -= OnPlayerDied;
            }
            BindStats(null);
            _nameTags.Clear();
            if (_paused) Time.timeScale = 1f;
        }

        void Build()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            _gm = GameManager.Instance;

            _time = Q<Label>("time-label");
            _score = Q<Label>("score-label");
            _lives = Q<Label>("lives-label");
            _countdown = Q<Label>("countdown");
            _banner = Q<Label>("banner");
            _statBombs = Q<Label>("stat-bombs");
            _statRange = Q<Label>("stat-range");
            _statSpeed = Q<Label>("stat-speed");
            _storyScore = Q<VisualElement>("story-score");
            _storyLives = Q<VisualElement>("story-lives");
            _chips = Q<VisualElement>("player-chips");
            _nameTagLayer = Q<VisualElement>("nametags");
            _pause = Q<VisualElement>("pause");
            _pauseHint = Q<Label>("pause-hint");
            _result = Q<VisualElement>("result");
            _resultTitle = Q<Label>("result-title");
            _resultSubtitle = Q<Label>("result-subtitle");
            _resultHint = Q<Label>("result-hint");

            Bind("btn-pause", () => SetPaused(true));
            Bind("btn-resume", () => SetPaused(false));
            _pauseQuit = Bind("btn-quit", QuitMatch);
            _resultPrimary = Bind("btn-result-primary", () => _gm.RestartStory());
            _resultSecondary = Bind("btn-result-secondary", QuitMatch);

            bool battle = _gm != null && _gm.Mode == GameMode.Battle;
            Show(_storyScore, !battle);
            Show(_storyLives, !battle);
            Show(_chips, battle);
            Show(_pause, false);
            Show(_result, false);
            Show(_countdown, false);
            Show(_banner, false);
            _pauseQuit.text = battle ? "LEAVE MATCH" : "QUIT TO MENU";
            _pauseHint.text = battle ? "THE MATCH KEEPS RUNNING WHILE THIS MENU IS OPEN" : "";

            Player.Spawned += OnPlayerSpawned;
            Player.Despawned += OnPlayerDespawned;
            if (_gm != null)
            {
                _gm.OnTimeChanged += UpdateTime;
                _gm.OnScoreChanged += UpdateScore;
                _gm.OnPhaseChanged += UpdatePhase;
                _gm.OnMatchEnded += ShowResult;
                _gm.OnAnyPlayerDied += OnPlayerDied;
            }

            foreach (Player player in Player.All) OnPlayerSpawned(player);
            UpdateTime();
            UpdateScore();
            UpdatePhase();
        }

        #endregion

        void Update()
        {
            if (_gm == null) return;

            if (Input.GetKeyDown(KeyCode.Escape) && !_hasResult)
                SetPaused(!_paused);

            UpdateCountdown();

            if (_hasResult && _gm.Mode == GameMode.Battle)
                _resultHint.text = $"BACK TO THE ROOM IN {Mathf.CeilToInt(_gm.ResultsTimeLeft)}...";
        }

        void LateUpdate()
        {
            if (_nameTags.Count == 0 || _root?.panel == null) return;
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;

            foreach (KeyValuePair<Player, Label> pair in _nameTags)
            {
                Player player = pair.Key;
                Label tag = pair.Value;
                if (player == null) continue;

                Vector3 world = player.transform.position + Vector3.up * _nameTagHeight;
                bool visible = !player.isDead && _camera.WorldToViewportPoint(world).z > 0f;
                Show(tag, visible);
                if (!visible) continue;

                Vector2 pos = RuntimePanelUtils.CameraTransformWorldToPanel(_root.panel, world, _camera);
                tag.style.left = pos.x;
                tag.style.top = pos.y;
            }
        }

        #region Top bar

        void UpdateTime()
        {
            if (_gm == null) return;
            int seconds = Mathf.CeilToInt(_gm.TimeLeft);
            if (seconds == _shownSeconds) return;
            _shownSeconds = seconds;
            _time.text = $"{seconds / 60}:{seconds % 60:00}";
            _time.parent.EnableInClassList("hud-card--danger", seconds <= 10);
        }

        void UpdateScore()
        {
            if (_gm == null) return;
            _score.text = _gm.Score.ToString();
            _lives.text = _gm.LifeCount.ToString();
        }

        void RebuildChips()
        {
            _chips.Clear();
            foreach (Player player in Player.All.Where(p => p != null).OrderBy(p => p.Slot))
            {
                var chip = new VisualElement();
                chip.AddToClassList("player-chip");
                chip.EnableInClassList("player-chip--local", player.IsLocal);
                chip.EnableInClassList("player-chip--dead", player.isDead);

                var dot = new VisualElement();
                dot.AddToClassList("player-chip__dot");
                dot.AddToClassList(SlotClass("player-bg--", player.Slot));
                chip.Add(dot);

                var name = new Label(player.NickName) { enableRichText = false };
                name.AddToClassList("player-chip__name");
                chip.Add(name);

                _chips.Add(chip);
            }
        }

        #endregion

        #region Players

        void OnPlayerSpawned(Player player)
        {
            if (player == null || _nameTags.ContainsKey(player)) return;

            if (_gm != null && _gm.Mode == GameMode.Battle)
            {
                var tag = new Label(player.IsLocal ? "YOU" : player.NickName)
                {
                    enableRichText = false,
                    pickingMode = PickingMode.Ignore,
                };
                tag.AddToClassList("nametag");
                tag.AddToClassList(SlotClass("player-bg--", player.Slot));
                _nameTagLayer.Add(tag);
                _nameTags[player] = tag;
                RebuildChips();
            }

            if (player.IsLocal) BindStats(player);
        }

        void OnPlayerDespawned(Player player)
        {
            if (_nameTags.Remove(player, out Label tag)) tag.RemoveFromHierarchy();
            if (player == _statsPlayer) BindStats(null);
            if (_gm != null && _gm.Mode == GameMode.Battle) RebuildChips();
        }

        void OnPlayerDied(Player player)
        {
            if (_gm.Mode == GameMode.Battle) RebuildChips();
            UpdatePhase();
        }

        void BindStats(Player player)
        {
            if (_statsPlayer != null) _statsPlayer.OnStatsChanged -= UpdateStats;
            _statsPlayer = player;
            if (_statsPlayer != null)
            {
                _statsPlayer.OnStatsChanged += UpdateStats;
                UpdateStats();
            }
        }

        void UpdateStats()
        {
            if (_statsPlayer == null) return;
            _statBombs.text = _statsPlayer.maxBomb.ToString();
            _statRange.text = _statsPlayer.explosionRange.ToString();
            _statSpeed.text = Mathf.RoundToInt(_statsPlayer.speed).ToString();
        }

        #endregion

        #region Center messages

        void UpdatePhase()
        {
            if (_gm == null) return;

            switch (_gm.Phase)
            {
                case MatchPhase.Waiting:
                    SetBanner(_gm.Mode == GameMode.Battle ? "WAITING FOR PLAYERS..." : null);
                    break;
                case MatchPhase.Countdown:
                    SetBanner("GET READY!");
                    break;
                case MatchPhase.Playing:
                    Player local = _gm.LocalPlayer;
                    SetBanner(_gm.Mode == GameMode.Battle && local != null && local.isDead ? "YOU DIED - SPECTATING" : null);
                    break;
                case MatchPhase.Ended:
                    SetBanner(null);
                    Show(_countdown, false);
                    break;
            }
        }

        void UpdateCountdown()
        {
            if (_gm.Phase == MatchPhase.Countdown)
            {
                int n = Mathf.CeilToInt(_gm.CountdownLeft);
                if (n != _shownCountdown && n > 0)
                {
                    _shownCountdown = n;
                    PopCountdown(n.ToString(), false);
                    _hideGoAt = 0f;
                }
                return;
            }

            if (_gm.Phase == MatchPhase.Playing && _shownCountdown > 0)
            {
                _shownCountdown = 0;
                PopCountdown("GO!", true);
                _hideGoAt = Time.time + 0.9f;
            }

            if (_hideGoAt > 0f && Time.time >= _hideGoAt)
            {
                _hideGoAt = 0f;
                Show(_countdown, false);
            }
        }

        void PopCountdown(string text, bool go)
        {
            _countdown.text = text;
            _countdown.EnableInClassList("countdown--go", go);
            Show(_countdown, true);
            // Start big and faded, then let the USS transition shrink it back.
            _countdown.AddToClassList("countdown--pop");
            _countdown.schedule.Execute(() => _countdown.RemoveFromClassList("countdown--pop")).StartingIn(20);
        }

        void SetBanner(string text)
        {
            Show(_banner, !string.IsNullOrEmpty(text));
            if (!string.IsNullOrEmpty(text)) _banner.text = text;
        }

        #endregion

        #region Pause & results

        void SetPaused(bool paused)
        {
            if (_gm == null || (_hasResult && paused)) return;
            _paused = paused;
            Show(_pause, paused);
            _gm.InputBlocked = paused;

            // Only Story can really pause; online matches keep running for everybody else.
            if (_gm.Mode == GameMode.Story) Time.timeScale = paused ? 0f : 1f;
        }

        void QuitMatch()
        {
            SetPaused(false);
            NetworkController.Instance.LeaveToMenu();
        }

        void ShowResult(MatchResult result)
        {
            SetPaused(false);
            _hasResult = true;
            SetBanner(null);
            Show(_countdown, false);

            _result.RemoveFromClassList("result--win");
            _result.RemoveFromClassList("result--lose");
            _result.RemoveFromClassList("result--draw");

            if (result.IsGameOver)
            {
                _result.AddToClassList("result--lose");
                _resultTitle.text = "GAME OVER";
                _resultSubtitle.text = $"SCORE {_gm.Score}";
                _resultHint.text = "";
                Show(_resultPrimary, true);
                _resultSecondary.text = "MAIN MENU";
            }
            else
            {
                if (result.IsDraw)
                {
                    _result.AddToClassList("result--draw");
                    _resultTitle.text = "DRAW!";
                    _resultSubtitle.text = "NOBODY WINS THIS ROUND";
                }
                else if (result.LocalWon)
                {
                    _result.AddToClassList("result--win");
                    _resultTitle.text = "VICTORY!";
                    _resultSubtitle.text = "YOU ARE THE LAST BOMBER STANDING";
                }
                else
                {
                    _result.AddToClassList("result--lose");
                    _resultTitle.text = "DEFEAT";
                    _resultSubtitle.text = $"{result.WinnerName.ToUpperInvariant()} WINS THE ROUND";
                }
                Show(_resultPrimary, false);
                _resultSecondary.text = "LEAVE ROOM";
            }

            Show(_result, true);
        }

        #endregion

        #region Helpers

        T Q<T>(string name) where T : VisualElement
        {
            T element = _root.Q<T>(name);
            if (element == null)
                throw new System.InvalidOperationException($"HudUI: element '{name}' ({typeof(T).Name}) not found in Hud.uxml.");
            return element;
        }

        Button Bind(string name, System.Action onClick)
        {
            Button button = Q<Button>(name);
            button.focusable = false;
            button.clicked += onClick;
            return button;
        }

        static void Show(VisualElement element, bool visible)
        {
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        static string SlotClass(string prefix, int slot) => prefix + Mathf.Clamp(slot, 0, 3);

        #endregion
    }
}
