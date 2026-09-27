using UnityEngine;
using UnityEngine.UIElements;

namespace ThanhHoang.Bomberman.UI
{
    public class CreateRoomDialog : UIView
    {
        readonly TextField _name;
        readonly Button[] _maxButtons;
        readonly Button _public;
        readonly Button _private;
        readonly Label _hint;
        int _maxPlayers = NetworkController.MaxRoomPlayers;
        bool _isPrivate;

        public CreateRoomDialog(VisualElement root) : base(root)
        {
            _name = Q<TextField>("create-name");
            _name.maxLength = NetworkController.MaxRoomNameLength;
            SetPlaceholder(_name, "LEAVE EMPTY FOR A RANDOM CODE");
            _name.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) Create();
            }, TrickleDown.TrickleDown);

            _maxButtons = new[]
            {
                BindButton("max-2", () => SelectMaxPlayers(2)),
                BindButton("max-3", () => SelectMaxPlayers(3)),
                BindButton("max-4", () => SelectMaxPlayers(4)),
            };
            _public = BindButton("vis-public", () => SelectPrivate(false));
            _private = BindButton("vis-private", () => SelectPrivate(true));
            _hint = Q<Label>("create-hint");

            BindButton("btn-create-cancel", Hide);
            BindButton("btn-create-confirm", Create);
        }

        protected override void OnShow()
        {
            _name.value = "";
            SelectMaxPlayers(NetworkController.MaxRoomPlayers);
            SelectPrivate(false);
            _name.schedule.Execute(() => _name.Focus()).StartingIn(50);
        }

        void SelectMaxPlayers(int count)
        {
            _maxPlayers = count;
            for (int i = 0; i < _maxButtons.Length; i++)
                _maxButtons[i].EnableInClassList("seg__item--selected", i + 2 == count);
        }

        void SelectPrivate(bool isPrivate)
        {
            _isPrivate = isPrivate;
            _public.EnableInClassList("seg__item--selected", !isPrivate);
            _private.EnableInClassList("seg__item--selected", isPrivate);
            _hint.text = isPrivate
                ? "PRIVATE ROOMS ARE HIDDEN FROM THE LIST. SHARE THE ROOM CODE WITH YOUR FRIENDS."
                : "ANYONE CAN FIND THIS ROOM IN THE LIST OR WITH QUICK PLAY.";
        }

        void Create()
        {
            Hide();
            NetworkController.Instance.CreateRoom(_name.value, _maxPlayers, _isPrivate);
        }
    }
}
