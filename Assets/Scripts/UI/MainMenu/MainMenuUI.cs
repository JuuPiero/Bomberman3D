using UnityEngine;
using UnityEngine.UIElements;

namespace ThanhHoang.Bomberman.UI
{
    /// <summary>
    /// Main menu controller (UI Toolkit). Screens are driven by <see cref="NetworkController.State"/>:
    /// Disconnected = home, Lobby = room browser, Room = room screen, anything in between = loading.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuUI : MonoBehaviour
    {
        HomeScreen _home;
        ProfileScreen _profile;
        RoomBrowserScreen _browser;
        RoomScreen _room;
        CreateRoomDialog _createDialog;
        LoadingOverlay _loading;
        Toast _toast;
        UIView _current;
        NetworkController _net;
        bool _started;

        // Built in Start (UIDocument has cloned its UXML by then) and again on every re-enable,
        // because UIDocument recreates its visual tree when it is re-enabled.
        void Start()
        {
            _started = true;
            Build();
        }

        void OnEnable()
        {
            if (_started) Build();
        }

        void Build()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            _net = NetworkController.Instance;

            _home = new HomeScreen(root.Q("home"), this);
            _profile = new ProfileScreen(root.Q("profile"));
            _browser = new RoomBrowserScreen(root.Q("browser"), this);
            _room = new RoomScreen(root.Q("room"), this);
            _createDialog = new CreateRoomDialog(root.Q("create-dialog"));
            _loading = new LoadingOverlay(root.Q("loading"), () => _net.Disconnect());
            _toast = new Toast(root.Q("toast"));
            _current = null;

            Label version = root.Q<Label>("version");
            if (version != null) version.text = $"BOMBERMAN  V{Application.version}";

            _net.StateChanged += OnNetStateChanged;
            _net.RoomListChanged += _browser.RefreshList;
            _net.RoomChanged += _room.Refresh;
            _net.Error += OnNetError;

            OnNetStateChanged();

            string pendingError = _net.ConsumePendingError();
            if (!string.IsNullOrEmpty(pendingError)) ShowToast(pendingError, true);
        }

        void OnDisable()
        {
            if (_net == null) return;
            _net.StateChanged -= OnNetStateChanged;
            _net.RoomListChanged -= _browser.RefreshList;
            _net.RoomChanged -= _room.Refresh;
            _net.Error -= OnNetError;
        }

        public void ShowScreen(UIView screen)
        {
            if (_current == screen) return;
            _current?.Hide();
            _current = screen;
            screen.Show();
        }

        public void ShowToast(string message, bool isError) => _toast.Show(message, isError);

        public void PlayOnline()
        {
            if (_net.HasNickName) _net.ConnectOnline();
            else OpenProfile(connectAfterSave: true);
        }

        public void OpenProfile(bool connectAfterSave)
        {
            UIView returnTo = _current ?? _home;
            _profile.Open(
                onSaved: () =>
                {
                    ShowScreen(returnTo);
                    if (connectAfterSave) _net.ConnectOnline();
                    else if (returnTo == _browser) _browser.RefreshList();
                },
                onBack: () => ShowScreen(returnTo));
            ShowScreen(_profile);
        }

        public void OpenCreateDialog() => _createDialog.Show();

        void OnNetStateChanged()
        {
            switch (_net.State)
            {
                case NetState.Disconnected:
                    _loading.Hide();
                    _createDialog.Hide();
                    if (_current != _profile) ShowScreen(_home);
                    break;
                case NetState.Connecting:
                    _loading.Show("CONNECTING...", cancellable: true);
                    break;
                case NetState.Joining:
                    _createDialog.Hide();
                    _loading.Show("JOINING ROOM...", cancellable: false);
                    break;
                case NetState.Leaving:
                    _loading.Show("LEAVING ROOM...", cancellable: false);
                    break;
                case NetState.Story:
                    _loading.Show("LOADING...", cancellable: false);
                    break;
                case NetState.Lobby:
                    _loading.Hide();
                    ShowScreen(_browser);
                    break;
                case NetState.Room:
                    _loading.Hide();
                    _createDialog.Hide();
                    ShowScreen(_room);
                    break;
            }
        }

        void OnNetError(string message) => ShowToast(message, true);
    }
}
