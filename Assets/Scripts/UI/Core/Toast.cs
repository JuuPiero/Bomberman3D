using UnityEngine.UIElements;

namespace ThanhHoang.Bomberman.UI
{
    /// <summary>Short message at the top of the screen that hides itself.</summary>
    public class Toast : UIView
    {
        const long DurationMs = 3500;

        readonly Label _text;
        IVisualElementScheduledItem _autoHide;

        public Toast(VisualElement root) : base(root)
        {
            _text = Q<Label>("toast-text");
            _text.enableRichText = false;
        }

        public void Show(string message, bool isError)
        {
            _text.text = message.ToUpperInvariant();
            Root.EnableInClassList("toast--error", isError);

            Hide();
            Show();

            _autoHide?.Pause();
            _autoHide = Root.schedule.Execute(Hide).StartingIn(DurationMs);
        }
    }
}
