using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ThanhHoang.Bomberman
{
    public class MainMenuScreen : BaseScreen
    {
        public Button battleModeButton;
        public Button startButton;

        protected override void Awake()
        {
            base.Awake();
            battleModeButton?.onClick.AddListener(() =>
            {
                Navigate<BattleScreen>();
            });

            startButton?.onClick.AddListener(() =>
            {
                SceneManager.LoadScene("Level1");
            });
        }
    }
}