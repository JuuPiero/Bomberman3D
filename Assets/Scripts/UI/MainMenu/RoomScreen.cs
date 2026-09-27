using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UIElements;
using NetPlayer = Photon.Realtime.Player;

namespace ThanhHoang.Bomberman.UI
{
    /// <summary>Inside a room: four player slots, ready toggle, host starts the match.</summary>
    public class RoomScreen : UIView
    {
        class SlotView
        {
            public VisualElement Root;
            public Label Name;
            public Label Status;
            public Label Host;
            public Label You;
        }

        readonly MainMenuUI _menu;
        readonly SlotView[] _slots = new SlotView[NetworkController.MaxRoomPlayers];
        readonly Label _title;
        readonly Label _code;
        readonly Label _visibility;
        readonly Label _count;
        readonly Label _hint;
        readonly Button _ready;
        readonly Button _start;

        public RoomScreen(VisualElement root, MainMenuUI menu) : base(root)
        {
            _menu = menu;
            _title = Q<Label>("room-title");
            _code = Q<Label>("room-code");
            _visibility = Q<Label>("room-visibility");
            _count = Q<Label>("room-count");
            _hint = Q<Label>("room-hint");

            for (int i = 0; i < _slots.Length; i++)
            {
                VisualElement slotRoot = Q<VisualElement>($"slot-{i}");
                _slots[i] = new SlotView
                {
                    Root = slotRoot,
                    Name = slotRoot.Q<Label>("slot-name"),
                    Status = slotRoot.Q<Label>("slot-status"),
                    Host = slotRoot.Q<Label>("slot-host"),
                    You = slotRoot.Q<Label>("slot-you"),
                };
            }

            BindButton("btn-leave", () => NetworkController.Instance.LeaveRoom());
            BindButton("btn-copy-code", CopyCode);
            _ready = BindButton("btn-ready", ToggleReady);
            _start = BindButton("btn-start", () => NetworkController.Instance.StartMatch());
        }

        protected override void OnShow()
        {
            // Coming back from a match: everybody has to confirm again.
            if (PhotonNetwork.InRoom && PhotonNetwork.LocalPlayer.IsReady())
                NetworkController.Instance.SetReady(false);
            Refresh();
        }

        public void Refresh()
        {
            if (!IsVisible || !PhotonNetwork.InRoom) return;

            Room room = PhotonNetwork.CurrentRoom;
            _title.text = room.Name;
            _code.text = room.Name;
            _visibility.text = room.IsVisible ? "PUBLIC" : "PRIVATE";
            _count.text = $"{room.PlayerCount}/{room.MaxPlayers} PLAYERS";

            NetPlayer[] bySlot = PlacePlayers();
            for (int i = 0; i < _slots.Length; i++)
                UpdateSlot(_slots[i], bySlot[i], i >= room.MaxPlayers);

            bool isHost = PhotonNetwork.IsMasterClient;
            bool ready = PhotonNetwork.LocalPlayer.IsReady();

            SetVisible(_ready, !isHost);
            _ready.text = ready ? "CANCEL READY" : "READY";
            _ready.EnableInClassList("btn--success", !ready);

            SetVisible(_start, isHost);
            bool canStart = NetworkController.Instance.CanStartMatch(out string reason);
            _start.SetEnabled(canStart);

            if (canStart)
                _hint.text = room.PlayerCount < 2 ? "YOU ARE ALONE - START A PRACTICE ROUND OR WAIT FOR FRIENDS" : "EVERYONE IS READY - PRESS START!";
            else if (!isHost)
                _hint.text = ready ? "WAITING FOR THE HOST TO START..." : "PRESS READY WHEN YOU ARE SET";
            else
                _hint.text = reason.ToUpperInvariant();
        }

        /// <summary>Players sit in their assigned slot; players still waiting for one take the first free seat.</summary>
        static NetPlayer[] PlacePlayers()
        {
            var bySlot = new NetPlayer[NetworkController.MaxRoomPlayers];
            var waiting = new List<NetPlayer>();

            foreach (NetPlayer player in PhotonNetwork.PlayerList)
            {
                int slot = player.GetSlot();
                if (slot >= 0 && slot < bySlot.Length && bySlot[slot] == null) bySlot[slot] = player;
                else waiting.Add(player);
            }

            foreach (NetPlayer player in waiting)
            {
                for (int i = 0; i < bySlot.Length; i++)
                {
                    if (bySlot[i] != null) continue;
                    bySlot[i] = player;
                    break;
                }
            }
            return bySlot;
        }

        static void UpdateSlot(SlotView view, NetPlayer player, bool locked)
        {
            bool empty = player == null;
            bool ready = !empty && (player.IsMasterClient || player.IsReady());

            view.Root.EnableInClassList("slot--empty", empty && !locked);
            view.Root.EnableInClassList("slot--locked", empty && locked);
            view.Root.EnableInClassList("slot--ready", ready);

            view.Name.text = empty ? (locked ? "LOCKED" : "WAITING...") : player.NickName;
            SetVisible(view.Host, !empty && player.IsMasterClient);
            SetVisible(view.You, !empty && player.IsLocal);
            view.Status.text = empty ? (locked ? "" : "EMPTY") : ready ? "READY" : "NOT READY";
        }

        void ToggleReady()
        {
            NetworkController.Instance.SetReady(!PhotonNetwork.LocalPlayer.IsReady());
        }

        void CopyCode()
        {
            if (!PhotonNetwork.InRoom) return;
            GUIUtility.systemCopyBuffer = PhotonNetwork.CurrentRoom.Name;
            _menu.ShowToast("ROOM CODE COPIED", false);
        }
    }
}
