using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;
using System;
using UnityEngine.UI;

public enum CellType { Normal, Mine, Bomb }

public class Cell : MonoBehaviour
{
    public bool isPainted = false;
    public bool isMine = false;
    public CellType cellType = CellType.Normal;
    public Player owner;

    private bool[] bombDirections = new bool[4];//0-up, 1-down, 2-right, 3-left
    private Vector3 positionOnField = Vector3.zero;
    public Cell(Vector2 positionOnGameField)
    {
        positionOnField = new Vector3(positionOnGameField.x, 0, positionOnField.y);
    }
    public Cell(CellType type, Vector2 positionOnGameField)
    {
        cellType = type;
        positionOnField = new Vector3(positionOnGameField.x, 0, positionOnField.y);

        if (type == CellType.Bomb)
        {
            for (int i = 0; i < bombDirections.Length; i++)
            {
                int rand = UnityEngine.Random.Range(0, 2);
                bombDirections[i] = Convert.ToBoolean(rand);
                if (bombDirections[i])
                {
                    Debug.Log("Position " + positionOnField);
                    GameObject direction = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    if (i == 0) direction.transform.position = new Vector3(positionOnField.x + 0, positionOnField.y + 0, positionOnField.z + 0.5f);
                    if (i == 1) direction.transform.position = new Vector3(positionOnField.x + 0, positionOnField.y + 0, positionOnField.z - 0.5f);
                    if (i == 2) direction.transform.position = new Vector3(positionOnField.x + 0.5f, positionOnField.y + 0, positionOnField.z + 0);
                    if (i == 3) direction.transform.position = new Vector3(positionOnField.x - 0.5f, positionOnField.y + 0, positionOnField.z + 0);
                    direction.transform.localScale /= 10f;
                }
            }
        }
    }
    public void Interact(PhotonView user, Vector2 cellPosition)
    {
        switch(cellType)
        {
            case CellType.Mine:
                if (isPainted && !isMine)
                {
                    Vector2 lastPaintedCell = FieldGameManager.instance.lastPaintedCell;

                    if (FieldGameManager.instance.fieldCells[lastPaintedCell].isPainted)
                        user.RPC("RPC_DeselectCell", RpcTarget.All, lastPaintedCell);
                    else
                    {
                        bool breakLoop = false;
                        //Find first painted cell
                        for (int y = 0; y < FieldGameManager.instance.fieldGridSize; y++)
                        {
                            for (int x = 0; x < FieldGameManager.instance.fieldGridSize; x++)
                            {
                                if(FieldGameManager.instance.fieldCells[new Vector2(x, y)].isPainted &&
                                    FieldGameManager.instance.fieldCells[new Vector2(x, y)].isMine)
                                {
                                    user.RPC("RPC_DeselectCell", RpcTarget.All, new Vector2(x, y));
                                    breakLoop = true;
                                    break;
                                }
                            }
                            if (breakLoop) break;
                        }
                    }
                    Debug.Log("U exploded!");
                    isMine = false;
                    cellType = CellType.Normal;
                }
                break;
            case CellType.Bomb:
                Debug.Log("IT's BOMB!");
                break;
            default:
                user.RPC("RPC_DeselectCell", RpcTarget.All, cellPosition);
                break;
        }
    }
    public void ChangeCellType(CellType type)
    {
        cellType = type;
    }
}

public class FieldGameManager : MonoBehaviour, IPunObservable
{
    #region PUBLIC VARIABLES

    public static FieldGameManager instance;

    public float mainTextureScale = 1f;
    public float secondsOfTurn = 60;

    public Material[] cellMaterials;

    public GameObject quad;
    public GameObject quadSelected;
    public GameObject playerCardPrefab;

    public bool myTurn;
    public List<Player> playersQueue = new List<Player>();

    public int[] playersScore;

    public Text timerText;
    public Text console;

    public Transform playerQueueTrans;

    public LayerMask layer;
    public Vector2 lastPaintedCell = Vector2.zero;

    public Dictionary<Vector2, Cell> fieldCells = new Dictionary<Vector2, Cell>();

    public Transform selectableQuad;

    #endregion

    #region PRIVATE VARIABLES

    Dictionary<Vector2, GameObject> cells = new Dictionary<Vector2, GameObject>();
    Dictionary<Vector2, GameObject> selectedCells = new Dictionary<Vector2, GameObject>();
    //Dictionary<Vector2, bool> isMineCells = new Dictionary<Vector2, bool>();

    private PhotonView PV;

    private float timer;

    private bool canCount;
    private bool placePlayerToMap;

    private Player myPlayer;
    private Player currentPlayerTurn; //Текущий игрок, который ходит

    private int myMaterial = -1;
    private int paintedCellsCount;
    private int playersReadyToChangingCells = 0;

    private bool loadFieldData;

    #endregion

    void Awake()
    {
        instance = this;
        PV = GetComponent<PhotonView>();

        loadFieldData = GameData.data.loadFieldData;
        playersReadyToChangingCells = 0;

        if (loadFieldData)
        {
            PV.RPC("RPC_LoadFieldData", RpcTarget.All);
            GenerateGameField(fieldGridSize);

            //Найти своего локального игрока
            foreach (var player in playersQueue)
            {
                if (player.IsLocal)
                {
                    myPlayer = player;
                    Debug.Log("MyPlayer: " + myPlayer.NickName);
                }
            }

            if (PhotonNetwork.IsMasterClient)
            {
                
                for (int i = 0; i < PhotonRoom.room.playersInRoom; i++)
                {
                    PV.RPC("RPC_ListPlayersToQueue", RpcTarget.All, i, playersQueue[i].NickName);
                }
                canCount = true;
                PV.RPC("RPC_ChangeColor", RpcTarget.All, playersQueue.IndexOf(currentPlayerTurn));
            }
        }
    }

    //Прибавить собранные очки
    void AddEarnedScoreFromPreviousMatch()
    {
        int indexOfPlayer = -1;
        
        for (int y = 0; y < GameData.data.playerWinPlaces.Length; y++)
        {
            foreach (var pl in playersQueue)
            {
                if (GameData.data.playerWinPlaces[y] == pl)
                {
                    indexOfPlayer = playersQueue.IndexOf(pl);
                    break;
                }
            }
            playersScore[y] += GameData.data.earnedScore[indexOfPlayer];
        }
    }

    void Start()
    {
        timer = secondsOfTurn;
    }

    void PrintDebug(string text)
    {
        console.text = console.text + text + "\n";
        Debug.Log(text);
    }

    void CountDown()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            PlayPlayerTurn(false);
            timer = secondsOfTurn;
        }
    }

    void Update()
    {
        if(myPlayer != null)
        {
            if (currentPlayerTurn == myPlayer)
                myTurn = true;
            else
                myTurn = false;
        }

        if (Input.GetKeyDown(KeyCode.Space) && myTurn) //Попросить хоста поменять очередь
        {
            PV.RPC("RPC_SendToMasterCall", RpcTarget.MasterClient, 1, 1, 1);
        }

        if (PhotonNetwork.IsMasterClient)
        {
            if (paintedCellsCount == fieldCells.Count && fieldCells.Count > 0)
            {
                EndOfGame();
            }
            if(playersReadyToChangingCells == PhotonRoom.room.playersInRoom && loadFieldData)
            {
                PV.RPC("RPC_ChangeCells", RpcTarget.All);
                playersReadyToChangingCells = 0;
            }

            if (Input.GetKeyDown(KeyCode.T))
            {
                AddEarnedScoreFromPreviousMatch();
            }
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            PV.RPC("RPC_SendToMasterCall", RpcTarget.MasterClient, 0, playersQueue.IndexOf(myPlayer), 1);
            
        }

        if (myTurn)
        {
            if (Input.GetMouseButtonDown(0) && playersScore[playersQueue.IndexOf(myPlayer)] > 0)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, 100, layer))
                {
                    Vector2 clickedPos = new Vector2(hit.transform.position.x, hit.transform.position.z);
                    if (!fieldCells[clickedPos].isPainted && isNearToMyCell(clickedPos))
                    {
                        lastPaintedCell = clickedPos;
                        PV.RPC("RPC_SelectCell", RpcTarget.All, clickedPos, myMaterial, false);

                        if(PV.IsMine)
                            playersScore[playersQueue.IndexOf(myPlayer)]--;
                        else
                            PV.RPC("RPC_SendToMasterCall", RpcTarget.MasterClient, 0, playersQueue.IndexOf(myPlayer), -1);
                    }
                }

            }
            if (Input.GetMouseButtonDown(1))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, 100))
                {
                    Vector2 clickedPos = new Vector2(hit.transform.position.x, hit.transform.position.z);
                    fieldCells[clickedPos].Interact(PV, clickedPos);
                }
            }
        }

        if (canCount)
        {
            string timerStr = timer.ToString();
            if (timerStr.IndexOf(',') < 0)
                timerText.text = timerStr;
            else
                timerText.text = timerStr.Substring(0, timerStr.IndexOf(','));
            CountDown();
        }
        if (playersQueue.Count > 0 && myMaterial == -1)
        {
            placePlayerToMap = true;
            myMaterial = playersQueue.IndexOf(myPlayer);
        }

        if (placePlayerToMap && myMaterial != -1)
        {
            PlacePlayerToMap(fieldGridSize);
        }

        if (myTurn)
        {
            selectableQuad.gameObject.SetActive(true);
            Ray ray2 = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit2;

            if (Physics.Raycast(ray2, out hit2, 100))
            {
                Vector2 clickedPos = new Vector2(hit2.transform.position.x, hit2.transform.position.z);
                selectableQuad.position = new Vector3(clickedPos.x, 0.1f, clickedPos.y);
            }
        }
        else selectableQuad.gameObject.SetActive(false);
    }

    void EndOfGame() //Конец игры, подсчет всего
    {
        PrintDebug("Match overed!");
    }

    bool isNearToMyCell(Vector2 cell) //Проверка на близость ячейки, чтобы можна было разместить рядом еще одну
    {
        Vector2 first = new Vector2(cell.x, cell.y);
        first = new Vector2(first.x + 1, first.y);
        Vector2 second = new Vector2(cell.x, cell.y);
        second = new Vector2(second.x - 1, second.y);
        Vector2 third = new Vector2(cell.x, cell.y);
        third = new Vector2(third.x, third.y + 1);
        Vector2 fourth = new Vector2(cell.x, cell.y);
        fourth = new Vector2(fourth.x, fourth.y - 1);

        Cell value;

        if (fieldCells.TryGetValue(first, out value))
        {
            if (fieldCells[first].isPainted && fieldCells[first].isMine)
            {
                return true;
            }
        }
        if (fieldCells.TryGetValue(second, out value))
        {
            if (fieldCells[second].isPainted && fieldCells[second].isMine)
            {
                return true;
            }
        }
        if (fieldCells.TryGetValue(third, out value))
        {
            if (fieldCells[third].isPainted && fieldCells[third].isMine)
            {
                return true;
            }
        }
        if (fieldCells.TryGetValue(fourth, out value))
        {
            if (fieldCells[fourth].isPainted && fieldCells[fourth].isMine)
            {
                return true;
            }
        }

        return false;
    }
    public float fieldGridSize;
    public void StartGame(float fldGridSize) //Начало игры, вызывает PhotonRoom когда все галочки нажаты
    {
        fieldGridSize = fldGridSize;
        //Найти своего локального игрока
        foreach (var player in PhotonRoom.room.photonPlayers)
        {
            if (player.IsLocal)
            {
                myPlayer = player;
            }
        }
        int playersLength = PhotonRoom.room.playersInRoom;

        if (!loadFieldData)
        {
            playersScore = new int[playersLength];
        }

        GenerateGameField(fieldGridSize);

        if (PhotonNetwork.IsMasterClient)
        {
            GenerateInteractableCells();
            RandomizePlayerTurn();
            for (int i = 0; i < playersLength; i++)
            {
                PV.RPC("RPC_ListPlayersToQueue", RpcTarget.All, i, playersQueue[i].NickName);
            }
            PlayPlayerTurn(true);
            canCount = true;
        }
    }

    private void PlacePlayerToMap(float fieldGridSize) //Закрасить 1 ячейку игрока на карте
    {
        Vector2[] possiblePositions = new Vector2[8];

        int valueMinus;
        if ((fieldGridSize % 2) == 0)
            valueMinus = 0;
        else valueMinus = 1;

        possiblePositions[0] = new Vector2(0, 0);
        possiblePositions[1] = new Vector2(fieldGridSize - 1, fieldGridSize - 1);
        possiblePositions[2] = new Vector2(0, fieldGridSize - 1);
        possiblePositions[3] = new Vector2(fieldGridSize - 1, 0);
        possiblePositions[4] = new Vector2(0, ((fieldGridSize - valueMinus) /2));
        possiblePositions[5] = new Vector2(fieldGridSize - 1, ((fieldGridSize - valueMinus) / 2));
        possiblePositions[6] = new Vector2(((fieldGridSize - valueMinus) / 2), fieldGridSize - 1);
        possiblePositions[7] = new Vector2(((fieldGridSize - valueMinus) / 2), 0);

        lastPaintedCell = possiblePositions[myMaterial];
        PV.RPC("RPC_SelectCell", RpcTarget.All, possiblePositions[myMaterial], myMaterial, true);
        //fieldCells[possiblePositions[myMaterial]].isMine = true;

        placePlayerToMap = false;
    }

    int playerIndex = -1;
    private void RandomizePlayerTurn() //Сгенерировать единожды очередь
    {
        int playersLength = PhotonRoom.room.playersInRoom;
        for (int i = 0; i < playersLength; i++)
        {
            playerIndex = UnityEngine.Random.Range(0, PhotonRoom.room.playersInRoom);

            if (playersQueue.Contains(PhotonRoom.room.photonPlayers[playerIndex]))
            {
                playersLength++;
                continue;
            }

            playersQueue.Add(PhotonRoom.room.photonPlayers[playerIndex]);
        }
    }


    void ChangeCardColor(int player)
    {
        playerQueueTrans.GetChild(player).GetComponent<Image>().color = Color.green;

        for (int i = 0; i < PhotonRoom.room.playersInRoom; i++)
        {
            if (i == player) continue;

            playerQueueTrans.GetChild(i).GetComponent<Image>().color = Color.white;
        }
    }

    private void PlayPlayerTurn(bool isFirstLaunch) //Метод для выбора следуещего игрока в очереди
    {
        if (isFirstLaunch)
            currentPlayerTurn = playersQueue[0];
        else
        {
            if (playersQueue.IndexOf(currentPlayerTurn) < playersQueue.Count-1)
            {
                currentPlayerTurn = playersQueue[playersQueue.IndexOf(currentPlayerTurn) + 1];
            }
            else
            {
                StartMiniGame();
            }
        }
        int currentPlayer = playersQueue.IndexOf(currentPlayerTurn);
        PV.RPC("RPC_ChangeColor", RpcTarget.All, currentPlayer);
    }

    private void StartMiniGame() //Рандомно выбрать мини-игру и запустить её
    {
        Debug.Log("Mini-Game started!");
        currentPlayerTurn = playersQueue[0];

        PhotonRoom.room.listedPlayers = 0;
        //Очистить места и очки победителей
        Debug.Log("PhotonRoom.room.playerEarnedScore.Length: " + PhotonRoom.room.playerEarnedScore.Length);

        for (int i = 0; i < PhotonRoom.room.playerEarnedScore.Length; i++)
        {
            PhotonRoom.room.playerEarnedScore[i] = 0;
            if(PhotonRoom.room.playerWinPlaces != null)
            {
                Debug.Log("PhotonRoom.room.playerWinPlaces.length: " + PhotonRoom.room.playerWinPlaces.Length);
                PhotonRoom.room.playerWinPlaces[i] = null;
            }
        }
        //Save field data
        PV.RPC("RPC_SaveFieldData", RpcTarget.All);

        PhotonNetwork.LoadLevel(MultiplayerSetting.multiplayerSetting.multiplayerScene);
    }

    private void GenerateGameField(float fieldGridSize)
    {
        GameObject field = new GameObject("Field");
        field.transform.position = new Vector3((fieldGridSize - 1) / 2, 0f, (fieldGridSize - 1) / 2);
        for (int y = 0; y < fieldGridSize; y++)
        {
            for (int x = 0; x < fieldGridSize; x++)
            {
                GameObject obj = Instantiate(quad);
                ////////////////////////////////Delete/////////////
                //bool Mine = UnityEngine.Random.Range(0, 2) >= 1f ? true : false;
                //bool Bomb = UnityEngine.Random.Range(0, 2) >= 1f ? true : false;
                //if (Mine)
                //{
                //    GameObject mine = GameObject.CreatePrimitive(PrimitiveType.Cube);
                //    mine.name = "MINE";
                //    mine.transform.position = obj.transform.position;
                //    mine.transform.SetParent(obj.transform);
                //    mine.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                //}
                //if (Bomb && !Mine)
                //{
                //    GameObject bomb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                //    bomb.name = "Bomb";
                //    bomb.transform.position = obj.transform.position;
                //    bomb.transform.SetParent(obj.transform);
                //    bomb.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                //}
                //////////////////////////////Delete/////////////////
                GameObject selObj = Instantiate(quadSelected);
                Vector2 position = new Vector2(x, y);
                if (loadFieldData)
                {
                    obj.SetActive(!fieldCells[position].isPainted);
                    selObj.SetActive(fieldCells[position].isPainted);
                    
                }
                else
                {
                    selObj.SetActive(false);
                    //if (Mine) ///////////////////Delete
                    //    fieldCells.Add(position, new Cell(CellType.Mine, position));
                    //else if(!Bomb) fieldCells.Add(position, new Cell(position));

                    //if (Bomb && !Mine) ///////////////////Delete
                    //    fieldCells.Add(position, new Cell(CellType.Bomb, position));
                    fieldCells.Add(position, new Cell(position));
                }
                Debug.Log("Cell with position: " + position + " is " + fieldCells[position].cellType);
                obj.transform.position = new Vector3(x, 0, y);
                selObj.transform.position = new Vector3(x, 0, y);
                obj.transform.eulerAngles = new Vector3(90f, 0, 0);
                selObj.transform.eulerAngles = new Vector3(90f, 0, 0);
                obj.transform.SetParent(field.transform);
                selObj.transform.SetParent(field.transform);
                cells.Add(position, obj);
                selectedCells.Add(position, selObj);
            }
        }

        //Сказать мастеру серверу, чтобы добавил текущего в список готовых к измене плиток
        PV.RPC("RPC_SendToMasterCall", RpcTarget.MasterClient, 2, -1, -1);

        BoxCollider col = field.AddComponent<BoxCollider>();
        col.size = new Vector3(fieldGridSize, 1, fieldGridSize);
        col.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        Camera.main.transform.GetComponent<CameraFitObject>().collider = col;
    }

    void GenerateInteractableCells()
    {
        for (float y = 0; y < fieldGridSize; y++)
        {
            for (float x = 0; x < fieldGridSize; x++)
            {
                float multiplier = UnityEngine.Random.Range(0.01f, 2f);
                float value = Mathf.PerlinNoise(x/ fieldGridSize * multiplier, y/ fieldGridSize * multiplier);
                Debug.Log("VALUE: " + value);
                if(value >= 0.6f)
                {
                    Debug.Log("Generate!");
                    PV.RPC("RPC_SelectCell", RpcTarget.All, new Vector2(x, y), 4, false);
                }
            }
        }
        Debug.Log("Generate end");
    }
    
    bool valuesReceived = false;
    bool valuesSended = false;
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            if (playersScore.Length > 0)
            {
                if(!valuesSended)
                {
                    for (int i = 0; i < PhotonRoom.room.playersInRoom; i++)
                    {
                        stream.SendNext(playersQueue[i]);
                    }
                    valuesSended = true;
                }
                for (int i = 0; i < PhotonRoom.room.playersInRoom; i++)
                {
                    stream.SendNext(playersScore[i]);
                }
            }
            stream.SendNext(currentPlayerTurn);
            stream.SendNext(canCount);
            stream.SendNext(timer);
        }
        else
        {

            if (playersScore.Length > 0)
            {
                if (!valuesReceived)
                {
                    for (int i = 0; i < PhotonRoom.room.playersInRoom; i++)
                    {
                        playersQueue.Add((Player)stream.ReceiveNext());
                    }
                    valuesReceived = true;
                }
                for (int i = 0; i < PhotonRoom.room.playersInRoom; i++)
                {
                    
                    playersScore[i] = (int)stream.ReceiveNext();
                }
            }

            currentPlayerTurn = (Player)stream.ReceiveNext();
            canCount = (bool)stream.ReceiveNext();
            timer = (float)stream.ReceiveNext();
        }
    }
    #region All_RPC
    [PunRPC]
    void RPC_ChangeCells()
    {
        for (int y = 0; y < fieldGridSize; y++)
        {
            for (int x = 0; x < fieldGridSize; x++)
            {
                Vector2 pos = new Vector2(x, y);
                if (fieldCells[pos].isPainted && fieldCells[pos].isMine)
                {
                    PV.RPC("RPC_SelectCell", RpcTarget.All, pos, myMaterial, false);
                }
            }
        }
    }

    [PunRPC]
    void RPC_SendToMasterCall(int code, int index, int value)
    {
        switch (code)
        {
            case 0:
                playersScore[index] += value;
                break;
            case 1:
                PlayPlayerTurn(false);
                timer = secondsOfTurn;
                break;
            case 2:
                playersReadyToChangingCells++;
                break;
            default:
                break;
        }
    }
    [PunRPC]
    void RPC_ListPlayersToQueue(int i, string nick)
    {
        GameObject card = Instantiate(playerCardPrefab, playerQueueTrans);
        card.GetComponent<PlayerCard>().indexInQueue = i;
        card.GetComponent<PlayerCard>().nick = nick;
        Color color = cellMaterials[i].GetColor("Color_480A737A");
        color = new Color(color.r, color.g, color.b, 1f);
        card.GetComponent<PlayerCard>().playerColor.color = color;
    }

    [PunRPC]
    void RPC_ChangeColor(int pl)
    {
        ChangeCardColor(pl);
    }

    [PunRPC]
    void RPC_SelectCell(Vector2 cellPosition, int materialIndex, bool mine) //Delete mine BOOL
    {
        fieldCells[cellPosition].isPainted = true;
        if (mine) fieldCells[cellPosition].cellType = CellType.Mine;
        if (materialIndex == myMaterial)
            fieldCells[cellPosition].isMine = true;
        selectedCells[cellPosition].SetActive(true);
        selectedCells[cellPosition].GetComponent<MeshRenderer>().material = cellMaterials[materialIndex];
        cells[cellPosition].SetActive(false);
        paintedCellsCount++;
        lastPaintedCell = cellPosition;
        Debug.Log("SELECTED cell: " + cellPosition + " mat: " + materialIndex);
    }
    [PunRPC]
    void RPC_DeselectCell(Vector2 cellPosition)
    {
        fieldCells[cellPosition].isPainted = false;
        fieldCells[cellPosition].isMine = false;
        
        selectedCells[cellPosition].SetActive(false);
        cells[cellPosition].SetActive(true);
        paintedCellsCount--;
        Debug.Log("DESELECTED cell: " + cellPosition);
    }
    [PunRPC]
    void RPC_SaveFieldData()
    {
        loadFieldData = true;

        GameData.data.loadFieldData = loadFieldData;
        GameData.data.fieldGridSize = fieldGridSize;

        GameData.data.myMaterial = myMaterial;
        GameData.data.playersQueue.Clear();
        GameData.data.playersQueue.AddRange(playersQueue);
        GameData.data.currentPlayerScore = new int[playersScore.Length];
        playersScore.CopyTo(GameData.data.currentPlayerScore, 0);

        GameData.data.currentPlayerTurn = currentPlayerTurn;

        GameData.data.fieldCells.Clear();
        foreach (var cell in fieldCells)
        {
            GameData.data.fieldCells.Add(cell.Key, cell.Value);
        }
        
    }
    [PunRPC]
    void RPC_LoadFieldData()
    {
        fieldGridSize = GameData.data.fieldGridSize;
        myMaterial = GameData.data.myMaterial;

        playersQueue.Clear();
        playersQueue.AddRange(GameData.data.playersQueue);
        playersScore = new int[GameData.data.currentPlayerScore.Length];
        GameData.data.currentPlayerScore.CopyTo(playersScore, 0);

        if (PhotonNetwork.IsMasterClient)
        {
            AddEarnedScoreFromPreviousMatch();
        }

        currentPlayerTurn = GameData.data.currentPlayerTurn;

        foreach (var cell in GameData.data.fieldCells)
        {
            fieldCells.Add(cell.Key, cell.Value);
            
        }
    }

    #endregion
}
