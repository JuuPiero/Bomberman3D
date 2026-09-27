using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThanhHoang.Bomberman.UI
{
    /// <summary>Choose the nickname shown to other players.</summary>
    public class ProfileScreen : UIView
    {
        const int MinLength = 2;

        readonly TextField _field;
        readonly Label _error;
        Action _onSaved;
        Action _onBack;

        public ProfileScreen(VisualElement root) : base(root)
        {
            _field = Q<TextField>("nickname-field");
            _field.maxLength = NetworkController.MaxNickNameLength;
            SetPlaceholder(_field, "TYPE YOUR NAME");
            _field.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);

            _error = Q<Label>("nickname-error");

            BindButton("btn-profile-save", Save);
            BindButton("btn-profile-back", () => _onBack?.Invoke());
        }

        public void Open(Action onSaved, Action onBack)
        {
            _onSaved = onSaved;
            _onBack = onBack;
        }

        protected override void OnShow()
        {
            _field.value = NetworkController.Instance.NickName;
            _error.text = "";
            _field.schedule.Execute(() => _field.Focus()).StartingIn(50);
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                Save();
        }

        void Save()
        {
            string nickname = NetworkController.Sanitize(_field.value, NetworkController.MaxNickNameLength);
            if (nickname.Length < MinLength)
            {
                _error.text = $"YOUR NAME NEEDS AT LEAST {MinLength} CHARACTERS";
                return;
            }

            NetworkController.Instance.NickName = nickname;
            _onSaved?.Invoke();
        }
    }
}
