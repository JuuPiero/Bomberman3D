using UnityEngine.UI;

namespace ThanhHoang.Bomberman
{
    public class LobbyScreen : BaseScreen
    {
        public Button backButton;
        protected override void Awake()
        {
            base.Awake();
            backButton?.onClick.AddListener(() =>
            {
                Service.Get<NavigationContainer>().Stack.GoBack();
            });
        }
    }
}