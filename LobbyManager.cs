using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
public class LobbyManager : MonoBehaviourPunCallbacks, ILobbyCallbacks, IPunObservable
{
    public static LobbyManager lobby;

    public string roomName;
    public int roomSize;
    public GameObject roomListingPrefab; //префаб комнаты для панели
    public Transform roomsPanel; //панель комнат в меню
    public List<RoomInfo> roomListings; //список комнат

    public Text itemsFreeText;

    void Awake()
    {
        lobby = this;
    }

    // Start is called before the first frame updates
    void Start()
    {
        
        PhotonNetwork.ConnectUsingSettings();
        roomListings = new List<RoomInfo>(); //////////////////?????/
        UpdateFreeSlotsText();
    }

    public void UpdateFreeSlotsText()
    {
        itemsFreeText.text = GMSetting.setting.freeSlotsForBuy.ToString();
    }

    public override void OnConnectedToMaster()//Когда игрок подключается к мастер серверу протона
    {

        Debug.Log("Player has connected to the Photon master server");
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.NickName = "Player " + Random.Range(0, 1000);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        base.OnRoomListUpdate(roomList);
        int tempIndex;
        foreach (RoomInfo room in roomList)
        {
            if (roomListings != null)
            {
                tempIndex = roomListings.FindIndex(ByName(room.Name));
            }
            else
            {
                tempIndex = -1;
            }
            if (tempIndex != -1)
            {
                roomListings.RemoveAt(tempIndex);
                Destroy(roomsPanel.GetChild(tempIndex).gameObject);
            }
            else
            {
                roomListings.Add(room);
                ListRoom(room);
            }
        }
    }

    static System.Predicate<RoomInfo> ByName(string name)
    {
        return delegate (RoomInfo room)
        {
            return room.Name == name;
        };
    }

    void RemoveRoomListings()
    {
        int i = 0;
        while (roomsPanel.childCount != 0)
        {
            Destroy(roomsPanel.GetChild(i).gameObject);
            i++;
        }
    }

    void ListRoom(RoomInfo room)
    {
        if (room.IsOpen && room.IsVisible)
        {
            GameObject tempListing = Instantiate(roomListingPrefab, roomsPanel);
            RoomButton tempButton = tempListing.GetComponent<RoomButton>();
            tempButton.roomName = room.Name;
            tempButton.roomSize = room.MaxPlayers;
            tempButton.currentRoomSize = room.PlayerCount;
            tempButton.SetRoom();
        }
    }

    public override void OnJoinedRoom()//Когда комната создается либо в неё заходят
    {
        base.OnJoinedRoom();
        Debug.Log("We are now in a room");

        PhotonRoom.room.lobbyGO.SetActive(false);
        PhotonRoom.room.roomGO.SetActive(true);
        if (PhotonNetwork.IsMasterClient)
        {
            LobbyManager.lobby.fieldSizeSlider.interactable = true;
        }
        PhotonRoom.room.ClearPlayerListings();
        PhotonRoom.room.ListPlayers();

        PhotonRoom.room.photonPlayers = PhotonNetwork.PlayerList;
        PhotonRoom.room.playersInRoom = PhotonRoom.room.photonPlayers.Length;
        PhotonRoom.room.myNumberInRoom = PhotonRoom.room.playersInRoom;


        if (MultiplayerSetting.multiplayerSetting.delayStart)
        {
            Debug.Log("Displayer players in room out of max players possible (" + PhotonRoom.room.playersInRoom + ":" + MultiplayerSetting.multiplayerSetting.maxPlayers + ")");
            if (PhotonRoom.room.playersInRoom > 1)
            {
                PhotonRoom.room.readyToCount = true;
            }
            if (PhotonRoom.room.playersInRoom == MultiplayerSetting.multiplayerSetting.maxPlayers)
            {
                PhotonRoom.room.readyToStart = true;
                if (!PhotonNetwork.IsMasterClient) return;
                PhotonNetwork.CurrentRoom.IsOpen = false;
            }
        }
    }
    public override void OnPlayerEnteredRoom(Player newPlayer) //Когда в комнату заходит кто-то
    {
        base.OnPlayerEnteredRoom(newPlayer);
        Debug.Log("A new player has joined the room");
        PhotonRoom.room.ClearPlayerListings();
        PhotonRoom.room.ListPlayers();
        PhotonRoom.room.photonPlayers = PhotonNetwork.PlayerList;
        PhotonRoom.room.playersInRoom++;

        if (MultiplayerSetting.multiplayerSetting.delayStart)
        {
            //Debug.Log("Displayer players in room out of max players possible (" + PhotonRoom.room.playersInRoom + ":" + MultiplayerSetting.multiplayerSetting.maxPlayers + ")");
            if (PhotonRoom.room.playersInRoom > 1)
            {
                PhotonRoom.room.readyToCount = true;
            }
            if (PhotonRoom.room.playersInRoom == MultiplayerSetting.multiplayerSetting.maxPlayers)
            {
                PhotonRoom.room.readyToStart = true;
                if (!PhotonNetwork.IsMasterClient) return;
                PhotonNetwork.CurrentRoom.IsOpen = false;
            }
        }
    }
    //public override void OnPlayerLeftRoom(Player otherPlayer)
    //{
    //    base.OnPlayerLeftRoom(otherPlayer);
    //    Debug.Log(otherPlayer.NickName + " has left the game");
    //    PhotonRoom.room.playersInRoom--;
    //    PhotonRoom.room.ClearPlayerListings();
    //    PhotonRoom.room.ListPlayers();
    //}
    public void CreateRoom()//Создание комнаты
    {
        Debug.Log("Trying to create a new Room");
        //Опции комнати
        RoomOptions roomOps = new RoomOptions() { IsVisible = true, IsOpen = true, MaxPlayers = (byte)roomSize };
        PhotonNetwork.CreateRoom(roomName, roomOps);//При создании комнаты нужны эти опции
    }
    public override void OnCreatedRoom() //Когда комната создана
    {
        Debug.Log("Room has been created!");
        GMSetting.setting.money = GMSetting.setting.startMoney;
        GMSetting.setting.UpdateMoneyUI();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.Log("Tried to create a new room but failed, there must already be a room with the same name");
        //CreateRoom();
    }

    public void OnRoomNameChanged(string nameIn) //Имя в лобби изменено
    {
        roomName = nameIn;
    }

    public void OnRoomSizeChanged(string sizeIn) //Кол-во игроков изменено
    {
        roomSize = int.Parse(sizeIn);
    }

    public void JoinLobbyOnClick() //Подключение по щелчку 
    {
        if (!PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
    }

    private int fieldGridSize = 7;
    public Slider fieldSizeSlider;
    public Text fieldGridSizeText;
    public GameObject menuUI;
    public void OnFieldSizeChanged() //Кол-во игроков изменено
    {
        fieldGridSize = (int)fieldSizeSlider.value;
        fieldGridSizeText.text = fieldGridSize + "x" + fieldGridSize;
    }

    public void StartGame()
    {
        PhotonRoom.room.isGameLoaded = true;
        //if (!PhotonNetwork.IsMasterClient) return;
        if (MultiplayerSetting.multiplayerSetting.delayStart)
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;
        }
        //PhotonNetwork.LoadLevel(MultiplayerSetting.multiplayerSetting.multiplayerScene);
        menuUI.SetActive(false);
        FieldGameManager.instance.StartGame(fieldGridSize);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(fieldGridSize);
            stream.SendNext(fieldGridSize);
        }
        else
        {
            fieldGridSize = (int)stream.ReceiveNext();
            fieldSizeSlider.value = (int)stream.ReceiveNext();
        }
    }
}
