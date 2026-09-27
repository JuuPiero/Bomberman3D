using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using NetPlayer = Photon.Realtime.Player;

public enum NetState
{
    Disconnected, // not online (main menu home)
    Connecting,   // connecting to Photon / joining the lobby
    Lobby,        // in the lobby, room list available
    Joining,      // creating / joining a room
    Room,         // inside an online room
    Leaving,      // leaving a room, going back to the lobby
    Story,        // offline room used by Story mode
}

/// <summary>
/// Owns the Photon connection: connect, lobby, room list, create/join/leave rooms, ready flags and
/// match start. Story mode runs in PUN's offline mode so gameplay code is shared with Battle mode.
/// Lives across scenes (DontDestroyOnLoad) and is created on first access if not in the scene.
/// </summary>
public class NetworkController : MonoBehaviourPunCallbacks
{
    public const string MenuScene = "MainMenu";
    public const string BattleScene = "Level1";
    public const string StoryFirstScene = "Level1";
    public const int MaxRoomPlayers = 4;
    public const int MaxNickNameLength = 16;
    public const int MaxRoomNameLength = 20;

    const string NickNameKey = "bomberman.nickname";
    const string RoomCodeChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    static NetworkController s_instance;
    static bool s_quitting;
    static bool s_menuLoadRequested;

    public static NetworkController Instance
    {
        get
        {
            if (s_instance == null && !s_quitting)
            {
                s_instance = FindFirstObjectByType<NetworkController>();
                if (s_instance == null)
                    s_instance = new GameObject(nameof(NetworkController)).AddComponent<NetworkController>();
            }
            return s_instance;
        }
    }

    /// <summary>Set when Story mode is started from the menu; GameManager resets lives/score once.</summary>
    public static bool StoryNewGame { get; set; }

    public event Action StateChanged;
    public event Action RoomListChanged;
    public event Action RoomChanged;
    public event Action<string> Error;

    public NetState State { get; private set; } = NetState.Disconnected;

    /// <summary>Last error that happened while no menu was listening (e.g. disconnect mid-match).</summary>
    public string PendingError { get; private set; }

    readonly Dictionary<string, RoomInfo> _rooms = new Dictionary<string, RoomInfo>();
    readonly Dictionary<int, int> _pendingSlots = new Dictionary<int, int>(); // master only: actor -> slot not yet confirmed
    bool _pendingStory;

    public IEnumerable<RoomInfo> Rooms => _rooms.Values;

    public string NickName
    {
        get
        {
            string saved = PlayerPrefs.GetString(NickNameKey, "");
            return string.IsNullOrWhiteSpace(saved) ? "" : saved;
        }
        set
        {
            PlayerPrefs.SetString(NickNameKey, Sanitize(value, MaxNickNameLength));
            PlayerPrefs.Save();
            PhotonNetwork.NickName = NickName;
        }
    }

    public bool HasNickName => !string.IsNullOrEmpty(NickName);

    #region Unity

    void Awake()
    {
        if (s_instance != null && s_instance != this)
        {
            // Deactivate first so the duplicate never registers for Photon callbacks (OnEnable).
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }
        s_instance = this;
        DontDestroyOnLoad(gameObject);

        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.SerializationRate = 15;
    }

    public override void OnEnable()
    {
        base.OnEnable();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        s_menuLoadRequested = false;
    }

    void OnApplicationQuit()
    {
        s_quitting = true;
    }

    #endregion

    #region Public API (menus)

    public void ConnectOnline()
    {
        if (State == NetState.Connecting) return;

        if (PhotonNetwork.OfflineMode)
            PhotonNetwork.OfflineMode = false;

        PhotonNetwork.NickName = HasNickName ? NickName : "Player" + UnityEngine.Random.Range(1000, 9999);

        if (PhotonNetwork.IsConnectedAndReady)
        {
            if (PhotonNetwork.InRoom) SetState(NetState.Room);
            else if (PhotonNetwork.InLobby) SetState(NetState.Lobby);
            else
            {
                SetState(NetState.Connecting);
                PhotonNetwork.JoinLobby();
            }
            return;
        }

        SetState(NetState.Connecting);
        if (!PhotonNetwork.ConnectUsingSettings())
        {
            SetState(NetState.Disconnected);
            RaiseError("Could not connect to the server.");
            return;
        }
        PhotonNetwork.GameVersion = Application.version;
    }

    public void Disconnect()
    {
        _pendingStory = false;
        ClientState clientState = PhotonNetwork.NetworkClientState;
        bool online = !PhotonNetwork.OfflineMode && clientState != ClientState.Disconnected && clientState != ClientState.PeerCreated;
        if (online)
            PhotonNetwork.Disconnect(); // also works while still connecting; ends in OnDisconnected
        else
            SetState(NetState.Disconnected);
    }

    public void CreateRoom(string roomName, int maxPlayers, bool isPrivate)
    {
        if (State != NetState.Lobby) return;

        roomName = Sanitize(roomName, MaxRoomNameLength);
        if (string.IsNullOrEmpty(roomName)) roomName = GenerateRoomCode();

        SetState(NetState.Joining);
        if (!PhotonNetwork.CreateRoom(roomName, CreateRoomOptions(maxPlayers, isPrivate)))
        {
            SetState(NetState.Lobby);
            RaiseError("Could not create the room.");
        }
    }

    public void JoinRoom(string roomName)
    {
        if (State != NetState.Lobby) return;

        roomName = Sanitize(roomName, MaxRoomNameLength);
        if (string.IsNullOrEmpty(roomName))
        {
            RaiseError("Enter a room name or code.");
            return;
        }

        SetState(NetState.Joining);
        if (!PhotonNetwork.JoinRoom(roomName))
        {
            SetState(NetState.Lobby);
            RaiseError("Could not join the room.");
        }
    }

    /// <summary>Joins any open public room, or creates a new one if none is available.</summary>
    public void QuickPlay()
    {
        if (State != NetState.Lobby) return;

        SetState(NetState.Joining);
        if (!PhotonNetwork.JoinRandomOrCreateRoom(roomName: GenerateRoomCode(), roomOptions: CreateRoomOptions(MaxRoomPlayers, false)))
        {
            SetState(NetState.Lobby);
            RaiseError("Could not find a room.");
        }
    }

    public void LeaveRoom()
    {
        if (PhotonNetwork.OfflineMode)
        {
            LeaveToMenu();
            return;
        }
        if (!PhotonNetwork.InRoom) return;

        SetState(NetState.Leaving);
        PhotonNetwork.LeaveRoom(false);
    }

    public void SetReady(bool ready)
    {
        if (!PhotonNetwork.InRoom) return;
        PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { NetProps.Ready, ready } });
    }

    /// <summary>Host can start once every other player is ready.</summary>
    public bool CanStartMatch(out string reason)
    {
        reason = "";
        if (!PhotonNetwork.InRoom || PhotonNetwork.OfflineMode) return false;
        if (!PhotonNetwork.IsMasterClient)
        {
            reason = "Waiting for the host to start...";
            return false;
        }

        foreach (NetPlayer player in PhotonNetwork.PlayerList)
        {
            if (player.GetSlot() < 0)
            {
                reason = "Assigning player slots...";
                return false;
            }
            if (!player.IsMasterClient && !player.IsReady())
            {
                reason = "Waiting for everyone to be ready...";
                return false;
            }
        }
        return true;
    }

    public void StartMatch()
    {
        if (!CanStartMatch(out _)) return;

        PhotonNetwork.CurrentRoom.IsOpen = false; // nobody can join mid-match
        PhotonNetwork.LoadLevel(BattleScene);
    }

    /// <summary>Master only: clean networked objects and bring everybody back to the room screen.</summary>
    public void ReturnToRoom()
    {
        if (!PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom || PhotonNetwork.OfflineMode) return;

        PhotonNetwork.DestroyAll(); // also clears the instantiation cache so players don't respawn in the menu
        foreach (NetPlayer player in PhotonNetwork.PlayerList)
            player.SetCustomProperties(new Hashtable { { NetProps.Ready, false } }); // everybody confirms again
        PhotonNetwork.CurrentRoom.IsOpen = true;
        PhotonNetwork.LoadLevel(MenuScene);
    }

    public void StartStory()
    {
        StoryNewGame = true;
        _pendingStory = true;

        if (PhotonNetwork.IsConnected && !PhotonNetwork.OfflineMode)
        {
            PhotonNetwork.Disconnect(); // continues in OnDisconnected
            return;
        }
        EnterStoryRoom();
    }

    /// <summary>Used when a gameplay scene is played directly in the editor without going through the menu.</summary>
    public void EnsureOfflineRoom()
    {
        if (PhotonNetwork.InRoom) return;

        _pendingStory = false;
        if (PhotonNetwork.IsConnected && !PhotonNetwork.OfflineMode)
        {
            Debug.LogWarning("[Network] Gameplay scene loaded while online but outside a room.");
            return;
        }
        PhotonNetwork.OfflineMode = true;
        PhotonNetwork.CreateRoom("Story", new RoomOptions { MaxPlayers = 1 });
        SetState(NetState.Story);
    }

    /// <summary>Leaves whatever room we are in and goes back to the main menu.</summary>
    public void LeaveToMenu()
    {
        if (PhotonNetwork.OfflineMode)
        {
            SetState(NetState.Disconnected);
            PhotonNetwork.OfflineMode = false; // leaves the offline room -> OnLeftRoom
            LoadMenuIfNeeded();
            return;
        }
        if (PhotonNetwork.InRoom)
        {
            LeaveRoom();
            return;
        }
        LoadMenuIfNeeded();
    }

    public string ConsumePendingError()
    {
        string error = PendingError;
        PendingError = null;
        return error;
    }

    public IEnumerable<RoomInfo> GetSortedRooms()
    {
        return _rooms.Values
            .Where(r => r.IsVisible && !r.RemovedFromList)
            .OrderByDescending(r => r.IsOpen && r.PlayerCount < r.MaxPlayers)
            .ThenByDescending(r => r.PlayerCount)
            .ThenBy(r => r.Name);
    }

    #endregion

    #region Photon callbacks

    public override void OnConnectedToMaster()
    {
        if (PhotonNetwork.OfflineMode) return;

        // After connecting, or after leaving a room, go (back) to the lobby to receive the room list.
        if (State == NetState.Connecting || State == NetState.Leaving || State == NetState.Lobby)
            PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        _rooms.Clear();
        // Properties stay on the local player between rooms; don't bring an old slot into a new room.
        PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { NetProps.Slot, null }, { NetProps.Ready, false } });
        SetState(NetState.Lobby);
        RoomListChanged?.Invoke();
    }

    public override void OnLeftLobby()
    {
        _rooms.Clear();
        RoomListChanged?.Invoke();
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        // PUN only sends changes: merge them into the cache.
        foreach (RoomInfo info in roomList)
        {
            if (info.RemovedFromList) _rooms.Remove(info.Name);
            else _rooms[info.Name] = info;
        }
        RoomListChanged?.Invoke();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        SetState(NetState.Lobby);
        RaiseError(returnCode == ErrorCode.GameIdAlreadyExists ? "A room with this name already exists." : $"Create room failed: {message}");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        SetState(NetState.Lobby);
        RaiseError(returnCode switch
        {
            ErrorCode.GameFull => "The room is full.",
            ErrorCode.GameClosed => "This room is already playing.",
            ErrorCode.GameDoesNotExist => "Room not found.",
            _ => $"Join room failed: {message}",
        });
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        SetState(NetState.Lobby);
        RaiseError($"No room found: {message}");
    }

    public override void OnJoinedRoom()
    {
        _rooms.Clear();

        if (PhotonNetwork.OfflineMode)
        {
            SetState(NetState.Story);
            if (_pendingStory)
            {
                _pendingStory = false;
                PhotonNetwork.LoadLevel(StoryFirstScene);
            }
            return;
        }

        _pendingSlots.Clear();
        PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { NetProps.Ready, false } });
        AssignSlots();
        SetState(NetState.Room);
    }

    public override void OnLeftRoom()
    {
        _pendingSlots.Clear();
        LoadMenuIfNeeded();

        // Online: PUN reconnects to the master server, OnConnectedToMaster re-joins the lobby.
        if (!PhotonNetwork.OfflineMode && State != NetState.Story && PhotonNetwork.IsConnected)
            SetState(NetState.Leaving);
    }

    public override void OnPlayerEnteredRoom(NetPlayer newPlayer)
    {
        AssignSlots();
        RoomChanged?.Invoke();
    }

    public override void OnPlayerLeftRoom(NetPlayer otherPlayer)
    {
        _pendingSlots.Remove(otherPlayer.ActorNumber);
        RoomChanged?.Invoke();
    }

    public override void OnMasterClientSwitched(NetPlayer newMasterClient)
    {
        if (PhotonNetwork.IsMasterClient && !PhotonNetwork.OfflineMode)
        {
            PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { { NetProps.Host, PhotonNetwork.NickName } });
            AssignSlots();
        }
        RoomChanged?.Invoke();
    }

    public override void OnPlayerPropertiesUpdate(NetPlayer targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey(NetProps.Slot))
            _pendingSlots.Remove(targetPlayer.ActorNumber);
        RoomChanged?.Invoke();
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        RoomChanged?.Invoke();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        _rooms.Clear();
        _pendingSlots.Clear();

        if (_pendingStory)
        {
            EnterStoryRoom();
            return;
        }

        bool expected = cause == DisconnectCause.None
                        || cause == DisconnectCause.DisconnectByClientLogic
                        || cause == DisconnectCause.ApplicationQuit;
        SetState(NetState.Disconnected);

        if (!expected)
            RaiseError($"Disconnected: {cause}");

        LoadMenuIfNeeded();
    }

    #endregion

    #region Helpers

    void EnterStoryRoom()
    {
        if (PhotonNetwork.OfflineMode && PhotonNetwork.InRoom)
        {
            _pendingStory = false;
            SetState(NetState.Story);
            PhotonNetwork.LoadLevel(StoryFirstScene);
            return;
        }

        SetState(NetState.Story);
        PhotonNetwork.OfflineMode = true;                                // fires OnConnectedToMaster (ignored)
        PhotonNetwork.CreateRoom("Story", new RoomOptions { MaxPlayers = 1 }); // fires OnJoinedRoom -> loads the level
    }

    RoomOptions CreateRoomOptions(int maxPlayers, bool isPrivate)
    {
        return new RoomOptions
        {
            MaxPlayers = Mathf.Clamp(maxPlayers, 2, MaxRoomPlayers),
            IsVisible = !isPrivate,
            IsOpen = true,
            CleanupCacheOnLeave = true,
            CustomRoomProperties = new Hashtable { { NetProps.Host, PhotonNetwork.NickName } },
            CustomRoomPropertiesForLobby = new[] { NetProps.Host },
        };
    }

    /// <summary>Master only: give every player a unique slot (0..3), keeping slots already assigned.</summary>
    void AssignSlots()
    {
        if (!PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom || PhotonNetwork.OfflineMode) return;

        var used = new HashSet<int>();
        var needSlot = new List<NetPlayer>();

        foreach (NetPlayer player in PhotonNetwork.PlayerList)
        {
            int slot = _pendingSlots.TryGetValue(player.ActorNumber, out int pending) ? pending : player.GetSlot();
            if (slot >= 0 && slot < MaxRoomPlayers && used.Add(slot)) continue;
            needSlot.Add(player);
        }

        foreach (NetPlayer player in needSlot)
        {
            int slot = 0;
            while (used.Contains(slot)) slot++;
            used.Add(slot);
            _pendingSlots[player.ActorNumber] = slot;
            player.SetCustomProperties(new Hashtable { { NetProps.Slot, slot } });
        }
    }

    void SetState(NetState state)
    {
        if (State == state) return;
        State = state;
        StateChanged?.Invoke();
    }

    void RaiseError(string message)
    {
        Debug.LogWarning("[Network] " + message);
        if (Error != null) Error.Invoke(message);
        else PendingError = message;
    }

    static void LoadMenuIfNeeded()
    {
        if (s_menuLoadRequested || SceneManager.GetActiveScene().name == MenuScene) return;
        s_menuLoadRequested = true; // several callbacks can ask for it in the same frame
        SceneManager.LoadScene(MenuScene);
    }

    static string GenerateRoomCode()
    {
        var chars = new char[5];
        for (int i = 0; i < chars.Length; i++)
            chars[i] = RoomCodeChars[UnityEngine.Random.Range(0, RoomCodeChars.Length)];
        return new string(chars);
    }

    public static string Sanitize(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        // '<' and '>' would be interpreted as rich text tags by UI labels.
        string clean = value.Replace("<", "").Replace(">", "").Trim();
        return clean.Length > maxLength ? clean.Substring(0, maxLength) : clean;
    }

    #endregion
}
