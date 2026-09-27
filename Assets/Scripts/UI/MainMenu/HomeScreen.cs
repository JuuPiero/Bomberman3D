using UnityEngine;
using UnityEngine.UIElements;

namespace ThanhHoang.Bomberman.UI
{
    public class HomeScreen : UIView
    {
        readonly Label _nickname;

        public HomeScreen(VisualElement root, MainMenuUI menu) : base(root)
        {
            _nickname = Q<Label>("home-nickname");

            BindButton("btn-online", menu.PlayOnline);
            BindButton("btn-story", () => NetworkController.Instance.StartStory());
            BindButton("btn-quit", Quit);
            BindButton("btn-edit-name", () => menu.OpenProfile(connectAfterSave: false));
        }

        protected override void OnShow()
        {
            string nickname = NetworkController.Instance.NickName;
            _nickname.text = string.IsNullOrEmpty(nickname) ? "NO NAME YET" : nickname;
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
