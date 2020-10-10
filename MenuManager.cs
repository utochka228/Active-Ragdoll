using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class MenuManager : MonoBehaviourPunCallbacks
{
    public Text LogText;

    // Start is called before the first frame updates
    void Start()
    {
        Log("Не играыайте вмЫ цфе, тут ви21рус!!!21кывсф");

        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.GameVersion = "1";
        PhotonNetwork.ConnectUsingSettings();
    }
    public override void OnConnectedToMaster()
    {
        Log("Connected To Master \n     Taras.exe => pizd`uk(lox);\n                Virus.type{korona}.Send(100);");
        Log("Уже поздно вашому пк пезда, скиньте грыш выдьмаковы з долини ыз квытыв))");
    }

    public void CreateRoom()
    {
        PhotonNetwork.CreateRoom(null, new Photon.Realtime.RoomOptions { MaxPlayers = 5});
    }

    public void JoinRoom()
    {
        PhotonNetwork.JoinRandomRoom();
    }

    public void ExitGame()
    {
        Application.Quit();
    }
    public override void OnJoinedRoom()
    {
        Log("Joined the room");

        PhotonNetwork.LoadLevel("Lobby");
    }
    // Update is called once per frame
    private void Log(string message)
    {
        Debug.Log(message);
        LogText.text += "\n";
        LogText.text += message;
    }
}
