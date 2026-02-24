using Photon.Pun;
using UnityEngine.UI;

namespace ThanhHoang.Bomberman
{
    public class BattleScreen : BaseScreen
    {
        public Button backButton;
        public Button playBattleModeButton;
        public Button joinRoomButton;
        protected override void Awake()
        {
            base.Awake();
            backButton?.onClick.AddListener(() =>
            {
                Service.Get<NavigationContainer>().Stack.Navigate<MainMenuScreen>();
            });
            playBattleModeButton?.onClick.AddListener(() =>
            {
                // Service.Get<NavigationContainer>().Stack.Navigate<MatchmakingScreen>();
                // PhotonNetwork.JoinOrCreateRoom("TestRoom", new Photon.Realtime.RoomOptions { MaxPlayers = 4 }, null);
                // Service.Get<NavigationContainer>().Stack.Navigate<LobbyScreen>();
                NetworkController.Instance.CreateRoom("TestRoom");
            });
            joinRoomButton?.onClick.AddListener(() =>
            {
                // Service.Get<NavigationContainer>().Stack.Navigate<LobbyScreen>();
                PhotonNetwork.JoinRoom("TestRoom");
            });
        }
    }
}