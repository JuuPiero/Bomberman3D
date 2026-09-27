using System;
using UnityEngine.UIElements;

namespace ThanhHoang.Bomberman.UI
{
    public class LoadingOverlay : UIView
    {
        readonly Label _text;
        readonly Button _cancel;
        readonly VisualElement _spinner;
        readonly IVisualElementScheduledItem _spin;
        float _angle;

        public LoadingOverlay(VisualElement root, Action onCancel) : base(root)
        {
            _text = Q<Label>("loading-text");
            _spinner = Q<VisualElement>("loading-spinner");
            _cancel = BindButton("btn-loading-cancel", onCancel);

            _spin = _spinner.schedule.Execute(() =>
            {
                _angle = (_angle + 9f) % 360f;
                _spinner.style.rotate = new Rotate(new Angle(_angle, AngleUnit.Degree));
            }).Every(16);
            _spin.Pause();
        }

        public void Show(string message, bool cancellable)
        {
            _text.text = message;
            SetVisible(_cancel, cancellable);
            Show();
        }

        protected override void OnShow() => _spin.Resume();
        protected override void OnHide() => _spin.Pause();
    }
}
