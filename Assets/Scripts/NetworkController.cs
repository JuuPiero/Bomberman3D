using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class NetworkController : MonoBehaviourPunCallbacks
{
    public static NetworkController Instance { get; private set; }
    public virtual void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this) Destroy(gameObject);
    }

    private void Start()
    {
        print("Loading...");
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("OnConnectedToMaster() was called by PUN.");
        PhotonNetwork.JoinLobby();
    }
   
    public void CreateRoom(string roomName)
    {
        PhotonNetwork.CreateRoom(roomName, new RoomOptions { MaxPlayers = 4 }, null);
    }

    public override void OnCreatedRoom()
    {
        base.OnCreatedRoom();
        print("Created Room: " + PhotonNetwork.CurrentRoom.Name);
    }

    public override void OnJoinedLobby()
    {
        base.OnJoinedLobby();
        print("Joined Lobby");
    }
    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        print("Joined Room: " + PhotonNetwork.CurrentRoom.Name);
        PhotonNetwork.LoadLevel("Level1");
    }


    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        base.OnRoomListUpdate(roomList);
        print("Room List Updated: " + roomList.Count + " rooms available.");
    }
}
