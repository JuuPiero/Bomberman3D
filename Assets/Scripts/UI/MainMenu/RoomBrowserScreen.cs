using System;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThanhHoang.Bomberman.UI
{
    /// <summary>Lobby: list/search rooms, quick play, create a room or join one by code.</summary>
    public class RoomBrowserScreen : UIView
    {
        readonly ScrollView _list;
        readonly Label _empty;
        readonly Label _status;
        readonly Label _nickname;
        readonly TextField _search;
        readonly TextField _code;

        public RoomBrowserScreen(VisualElement root, MainMenuUI menu) : base(root)
        {
            _list = Q<ScrollView>("room-list");
            _empty = Q<Label>("room-empty");
            _status = Q<Label>("lobby-status");
            _nickname = Q<Label>("lobby-nickname");

            _search = Q<TextField>("search-field");
            SetPlaceholder(_search, "SEARCH ROOM OR HOST...");
            _search.RegisterValueChangedCallback(_ => RefreshList());

            _code = Q<TextField>("code-field");
            _code.maxLength = NetworkController.MaxRoomNameLength;
            SetPlaceholder(_code, "ROOM CODE");
            _code.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) JoinByCode();
            }, TrickleDown.TrickleDown);

            BindButton("btn-quick", () => NetworkController.Instance.QuickPlay());
            BindButton("btn-create", menu.OpenCreateDialog);
            BindButton("btn-join-code", JoinByCode);
            BindButton("btn-lobby-back", () => NetworkController.Instance.Disconnect());
            BindButton("btn-lobby-name", () => menu.OpenProfile(connectAfterSave: false));

            Root.schedule.Execute(UpdateStatus).Every(1000);
        }

        protected override void OnShow()
        {
            _nickname.text = PhotonNetwork.NickName;
            _code.value = "";
            RefreshList();
            UpdateStatus();
        }

        public void RefreshList()
        {
            if (!IsVisible) return;

            _list.Clear();
            string filter = _search.value != null ? _search.value.Trim() : "";
            int count = 0;

            foreach (RoomInfo room in NetworkController.Instance.GetSortedRooms())
            {
                if (filter.Length > 0 && !Contains(room.Name, filter) && !Contains(room.GetHost(), filter))
                    continue;
                _list.Add(CreateRow(room));
                count++;
            }

            _empty.text = filter.Length > 0 ? "NO ROOM MATCHES YOUR SEARCH" : "NO ROOMS YET - CREATE ONE OR QUICK PLAY!";
            SetVisible(_empty, count == 0);
        }

        VisualElement CreateRow(RoomInfo room)
        {
            bool playing = !room.IsOpen;
            bool full = room.PlayerCount >= room.MaxPlayers;
            bool joinable = !playing && !full;
            string roomName = room.Name;

            var row = new VisualElement();
            row.AddToClassList("room-row");
            row.EnableInClassList("room-row--closed", !joinable);

            row.Add(MakeLabel(roomName, "col--name", "room-row__name"));
            row.Add(MakeLabel(room.GetHost(), "col--host", "room-row__host"));
            row.Add(MakeLabel($"{room.PlayerCount}/{room.MaxPlayers}", "col--players", "room-row__players"));

            var statusCol = new VisualElement();
            statusCol.AddToClassList("col--status");
            Label status = MakeLabel(playing ? "PLAYING" : full ? "FULL" : "OPEN", "pill", playing ? "pill--warn" : full ? "pill--bad" : "pill--good");
            statusCol.Add(status);
            row.Add(statusCol);

            var actionCol = new VisualElement();
            actionCol.AddToClassList("col--action");
            var join = new Button(() => NetworkController.Instance.JoinRoom(roomName)) { text = "JOIN", focusable = false };
            join.AddToClassList("btn");
            join.AddToClassList("btn--small");
            join.AddToClassList("btn--primary");
            join.SetEnabled(joinable);
            actionCol.Add(join);
            row.Add(actionCol);

            return row;
        }

        static Label MakeLabel(string text, params string[] classes)
        {
            var label = new Label(text) { enableRichText = false };
            foreach (string cls in classes) label.AddToClassList(cls);
            return label;
        }

        static bool Contains(string value, string filter)
        {
            return !string.IsNullOrEmpty(value) && value.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        void JoinByCode()
        {
            NetworkController.Instance.JoinRoom(_code.value);
        }

        void UpdateStatus()
        {
            if (!IsVisible) return;
            if (!PhotonNetwork.IsConnectedAndReady)
            {
                _status.text = "CONNECTING...";
                return;
            }

            string region = PhotonNetwork.CloudRegion ?? "";
            int slash = region.IndexOf('/');
            if (slash >= 0) region = region.Substring(0, slash);

            _status.text = $"REGION {region.ToUpperInvariant()}  |  PING {PhotonNetwork.GetPing()} MS  |  {PhotonNetwork.CountOfPlayers} ONLINE";
        }
    }
}
