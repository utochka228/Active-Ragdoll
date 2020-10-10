using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using RichTextPlugin;
using System;

public class PhotonRoom : MonoBehaviourPunCallbacks, IInRoomCallbacks
{
    public static PhotonRoom room; //Singleton
    public PhotonView PV;

    public bool isGameLoaded;
    public int currentScene;

    public Player[] photonPlayers; //игроки в комнате
    public int playersInRoom; //кол-во игроков в комнате
    public int myNumberInRoom;

    public int playersInGame;

    public float fieldGridSize = 1; //Размер игрового поля

    public bool readyToCount;
    public bool readyToStart;
    public float startingTime;
    private float lessThanMaxPlayers;
    private float atMaxPlayers;
    private float timeToStart;

    public GameObject lobbyGO; //объект лобби
    public GameObject roomGO; //объект комнаты
    public Transform playersPanel;//панель игроков
    public GameObject playerListingPrefab;//префаб игрока в лобби
    public GameObject startButton; //кнопка запуска игры
    public GameObject scoreBoard;
    public GameObject playerScoreTable;

    public Text gameModeText;
    public Text mapNameText;
    public Image mapImage;
    public Sprite[] images;

    [SerializeField]
    private Button teamButton;
    [SerializeField]
    private Button[] classButtons; //кнопки классов для персонажа

    public GameObject myPhotonNetworkPlayer; //ссылка на созданого игрока(пустышки)

    public GMSetting settings;//настройки геймплея мультиплеера

    public Action OnGameSceneChanged;


    void Awake()
    {
        if (PhotonRoom.room != null)
        {
            myNumberInRoom = PhotonRoom.room.myNumberInRoom;

            playersInRoom = PhotonNetwork.PlayerList.Length;
        }

        if (PhotonRoom.room == null)
        {
            PV = GetComponent<PhotonView>();
            playersInRoom = PhotonNetwork.PlayerList.Length;
            PV.RPC("RPC_UpdateLobby", RpcTarget.All);
            photonPlayers = PhotonNetwork.PlayerList;
            PhotonRoom.room = this;
        }
        else
        {
            if (PhotonRoom.room != this)
            {
                Destroy(PhotonRoom.room.gameObject);
                PhotonRoom.room = this;
            }
        }

        if (GameData.data != null)
            LoadData();

        DontDestroyOnLoad(this.gameObject);
    }

    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.AddCallbackTarget(this);
        SceneManager.sceneLoaded += OnSceneFinishedLoading;
    }
    public override void OnDisable()
    {
        base.OnDisable();
        PhotonNetwork.RemoveCallbackTarget(this);
        SceneManager.sceneLoaded -= OnSceneFinishedLoading;
    }
    
    void Start()
    {
        PV = GetComponent<PhotonView>();
        

        //Saving data in scripts when scene is changing
        OnGameSceneChanged += Inventory.instance.SaveData; 
        OnGameSceneChanged += GMSetting.setting.SaveData; 
        OnGameSceneChanged += SaveData; 

        readyToCount = false;
        readyToStart = false;
        lessThanMaxPlayers = startingTime;
        atMaxPlayers = 6;
        timeToStart = startingTime;

        gameModeText.text = RichText.Paint("Game mode: ", Color.red, true, false)
            + RichText.Paint("None", Color.white, false, true);
        mapNameText.text = RichText.Paint("Map: ", Color.yellow, true, false)
            + RichText.Paint("None", Color.white, false, true);
    }
    public void OnGameOvered()
    {
        if (OnGameSceneChanged != null)
            OnGameSceneChanged();
    }

    void SaveData()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            GameData.data.playerWinPlaces = new Player[playersInRoom];
            Debug.Log("playerWinPlaces.count: " + playerWinPlaces.Length);
            foreach (var pl in playerWinPlaces)
            {
                Debug.Log(pl.NickName + " in list");
            }
            playerWinPlaces.CopyTo(GameData.data.playerWinPlaces, 0);
            foreach (var pl in GameData.data.playerWinPlaces)
            {
                Debug.Log(pl.NickName + " in list(DAATA)");
            }
            GameData.data.earnedScore = new int[8];
            playerEarnedScore.CopyTo(GameData.data.earnedScore, 0);
            Debug.Log("Saved!");
        }
    }
    void LoadData()
    {
        /*
        if (PhotonNetwork.IsMasterClient)
        {
            playerWinPlaces = new Player[playersInRoom];
            GameData.data.playerWinPlaces.CopyTo(playerWinPlaces, 0);
            playerEarnedScore = new int[PhotonRoom.room.playersInRoom];
            GameData.data.earnedScore.CopyTo(playerEarnedScore, 0);
            Debug.Log("LOADED!");
        }*/
    }

    void Update()
    {
        if (PhotonRoom.room == null) Debug.Log("ROOM NULL!");
    }

    public void ClearPlayerListings()//очистить список игроков
    {
        for (int i = playersPanel.childCount - 1; i >= 0; i--)
        {
            Destroy(playersPanel.GetChild(i).gameObject);
        }
        playerListings.RemoveAll(x => x);
    }
    private GameObject playerLocalList;
    private List<GameObject> playerListings = new List<GameObject>();
    private PlayerListing pl;
    public void ListPlayers()//записать игроков
    {
        if (PhotonNetwork.InRoom)
        {
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                GameObject tempListing = Instantiate(playerListingPrefab, playersPanel);
                playerListings.Add(tempListing);
                if (player.IsLocal)
                {
                    playerLocalList = tempListing;
                    tempListing.GetComponent<PlayerListing>().readyToggle.interactable = true;
                }
                if (PhotonNetwork.IsMasterClient)
                {
                    int index = playerListings.IndexOf(playerLocalList);
                    PV.RPC("RPC_ActivateMasterIcon", RpcTarget.All, index);
                }

                Text tempText = tempListing.transform.GetChild(0).GetComponent<Text>();
                tempText.text = player.NickName;
            }
        }
    }
    public void OnToggle(GameObject _pl)
    {
        int index = playerListings.IndexOf(_pl);
        PV.RPC("RPC_OnReadyToPlay", RpcTarget.All, index);
    }

    public void OnReadyCheckChanged()
    {
        //PV.RPC("RPC_CheckReadyToggles", RpcTarget.MasterClient);
        PV.RPC("RPC_CheckReadyToggles", RpcTarget.All);
    }

    void RestartTimer()
    {
        lessThanMaxPlayers = startingTime;
        timeToStart = startingTime;
        atMaxPlayers = 6;
        readyToCount = false;
        readyToStart = false;
    }

    void OnSceneFinishedLoading(Scene scene, LoadSceneMode mode)
    {

        currentScene = scene.buildIndex;
        if (currentScene == MultiplayerSetting.multiplayerSetting.multiplayerScene)
        {
            isGameLoaded = true;
            if (MultiplayerSetting.multiplayerSetting.delayStart)
            {
                PV.RPC("RPC_LoadedGameScene", RpcTarget.MasterClient);
            }
            else
            {
                RPC_CreatePlayer();
            }
        }
        
    }
    
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);
        Debug.Log(otherPlayer.NickName + " has left the game");
        playersInRoom--;
        ClearPlayerListings();
        ListPlayers();
    }

    public void EnablePanel(GameObject obj)//для панелей карт, режимов и тд.
    {
        obj.SetActive(true);
    }
    public void DisablePanel(GameObject obj)//для панелей карт, режимов и тд.
    {
        obj.SetActive(false);
    }
    public void DisconnectPlayer()
    {
        if(isGameLoaded && GMSetting.setting.mode == GameMode.DeathMatch) //Для очистки списка игроков в дезматче
        {
            foreach (var player in DeathMatch.DM.players)
            {
                if(player.PV.IsMine)
                    DeathMatch.DM.players.Remove(player);
            }
        }
            
        Destroy(PhotonRoom.room.gameObject);
        StartCoroutine(DisconnectAndLoad());
    }

    public void LeaveLobby()
    {
        PhotonNetwork.LeaveRoom();
        roomGO.SetActive(false);
        lobbyGO.SetActive(true);
    }

    IEnumerator DisconnectAndLoad()
    {
        //PhotonNetwork.Disconnect();
        PhotonNetwork.LeaveRoom();
        while (PhotonNetwork.InRoom)
            yield return null;
        SceneManager.LoadScene(MultiplayerSetting.multiplayerSetting.menuScene);
    }

    //GameMode
    public void SelectGameMode(int mode)
    {
        PV.RPC("RPC_ChangeGameMode", RpcTarget.AllBuffered, mode);
    }
    //Map
    public void SelectMap(int map)
    {
        PV.RPC("RPC_ChangeMap", RpcTarget.AllBuffered, map);
    }
    //Class
    public void OnClickCharacterPick(int whichCharacter)
    {
        if (PlayerInfo.PI != null)
        {
            PlayerInfo.PI.mySelectedCharacter = whichCharacter;
            PlayerPrefs.SetInt("MyCharacter", whichCharacter);

            GMSetting.setting.characterClass = (Class)whichCharacter;
            int index = playerListings.IndexOf(playerLocalList);
            PV.RPC("RPC_ChangeClass", RpcTarget.AllBuffered, whichCharacter, index);

            for (int i = 0; i < classButtons.Length; i++)
            {
                if (i == whichCharacter) continue;
                if (classButtons[i].interactable == true) classButtons[i].interactable = false;
            }
        }
    }
    public void CallRPC_SetPlayerWinPlaces(Player player, bool haveBonus)
    {
        PV.RPC("RPC_SetPlayersWinPlaces", RpcTarget.MasterClient, player, haveBonus);
    }
    public void CallRPC_SpawnScoreBoard()
    {
        
        PV.RPC("RPC_SpawnScoreBoard", RpcTarget.All);
        
    }
    public void CallRPC_DoLastActions()
    {
        
        PV.RPC("RPC_DoLastActions", RpcTarget.All);
    }
    #region All_RPC

    public Player[] playerWinPlaces;
    public int listedPlayers = 0;
    public int[] playerEarnedScore;
    [PunRPC]
    void RPC_SetPlayersWinPlaces(Player player, bool haveBonus)
    {
        for (int i = 0; i < playerWinPlaces.Length; i++)
        {
            if (playerWinPlaces[i] == null)
            {
                playerWinPlaces[i] = player;
                if (!haveBonus) MiniGame.MG.getBonus[i] = false;
                listedPlayers++;
                Debug.Log(player + "has " + (i + 1) + " place!");
                break;
            }
            else continue;
        }
    }

    Transform scBoard;
    [PunRPC]
    void RPC_SpawnScoreBoard()
    {
        Transform canvas = GameObject.FindGameObjectWithTag("Canvas").transform;
        scBoard = Instantiate(scoreBoard, canvas).transform;
        if (PhotonNetwork.IsMasterClient)
        {
            for (int i = 0; i < PhotonRoom.room.playersInRoom; i++)
            {
                PV.RPC("RPC_SetValuesToPlayerResultTable", RpcTarget.All, playerWinPlaces[i], playerEarnedScore[i], MiniGame.MG.getBonus[i]);
            }
        }
    }

    [PunRPC]
    void RPC_SetValuesToPlayerResultTable(Player player, int score, bool haveBonus)
    {
        Transform plScoreTable = Instantiate(playerScoreTable, scBoard.GetChild(0)).transform;
        plScoreTable.GetChild(1).GetComponent<Text>().text = player.NickName;
        if (haveBonus)
        {
            plScoreTable.GetChild(2).GetComponent<Text>().text = (score - MiniGame.MG.bonusScore).ToString();
            plScoreTable.GetChild(3).GetComponent<Text>().text = "+ " + MiniGame.MG.bonusScore.ToString();
        }
        else
        {
            plScoreTable.GetChild(2).GetComponent<Text>().text = score.ToString();
            plScoreTable.GetChild(3).GetComponent<Text>().text = "No bonus";
        }
    }
    
    [PunRPC]
    void RPC_ChangeGameMode(int mode)
    {
        settings.mode = (GameMode)mode;
        gameModeText.text = RichText.Paint("Game mode: ", Color.red, true, false)
            + RichText.Paint(settings.mode.ToString(), Color.white, false, true);
    }

    [PunRPC]
    void RPC_ChangeMap(int map)
    {
        settings.map = (Map)map;
        mapNameText.text = mapNameText.text = RichText.Paint("Map: ", Color.yellow, true, false)
            + RichText.Paint(settings.map.ToString(), Color.white, false, true);
        foreach (Sprite sprite in images)
        {
            if (sprite.name == settings.map.ToString()) mapImage.sprite = sprite;
        }
    }
    [PunRPC]
    void RPC_ChangeClass(int whichClass, int index)
    {
        pl = playerListings[index].GetComponent<PlayerListing>();
        classButtons[whichClass].GetComponent<Button>().interactable = false;
        pl.classText.text = ((Class) whichClass).ToString();

    }

    [PunRPC]
    void RPC_DoLastActions() //Последние действия перед окончанием сцены
    {
        GMSetting.setting.isGameEnded = true;
        GMSetting.setting.isFirstLaunch = false;
        OnGameOvered();
        Destroy(GMSetting.setting.gameObject);
        Destroy(gameObject);
    }

    [PunRPC]
    void RPC_CreatePlayer()
    {
        myPhotonNetworkPlayer = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "PhotonNetworkPlayer"), transform.position, Quaternion.identity);
    }

    [PunRPC]
    void RPC_UpdateLobby()
    {
        lobbyGO.SetActive(false);
        roomGO.SetActive(false);
        ClearPlayerListings();
        ListPlayers();
    }
    [PunRPC]
    void RPC_LoadedGameScene()
    {
        playersInGame++;
        if (playersInGame == PhotonNetwork.PlayerList.Length)
        {
            PV.RPC("RPC_CreatePlayer", RpcTarget.All);
        }
    }


    [PunRPC]
    void RPC_OnReadyToPlay(int index)
    {
        pl = playerListings[index].GetComponent<PlayerListing>();
        if (pl.toggleGroup.enabled == false)
            pl.toggleGroup.enabled = true;

        pl.toggleGroup.allowSwitchOff = !pl.toggleGroup.allowSwitchOff;
        pl.isReady = pl.toggleGroup.allowSwitchOff;

        var colors = pl.readyToggle.colors;

        if (pl.readyToggle.interactable)
        {
            if (pl.toggleGroup.allowSwitchOff)
            {
                colors.normalColor = Color.green;
            }
            else
            {
                colors.normalColor = Color.red;
            }
        }
        else
        {
            if (pl.toggleGroup.allowSwitchOff)
            {
                colors.disabledColor = Color.green;
                pl.checkMark.GetComponent<Image>().enabled = false;
                pl.checkMark.GetComponent<Image>().enabled = true;
            }
            else
            {
                colors.disabledColor = Color.red;
                pl.checkMark.GetComponent<Image>().enabled = false;
            }
        }
        pl.readyToggle.colors = colors;
    }

    [PunRPC]
    void RPC_ActivateMasterIcon(int index)
    {
        pl = playerListings[index].GetComponent<PlayerListing>();
        pl.hostIcon.SetActive(true);
    }
    [PunRPC]
    void RPC_CheckReadyToggles()
    {
        int readyCount = 0;
        foreach (Transform player in playersPanel)
        {
            if (player.GetComponent<PlayerListing>().isReady)
                readyCount++;
        }
        if (readyCount == playersInRoom)
            LobbyManager.lobby.StartGame();
    }
    #endregion
}
