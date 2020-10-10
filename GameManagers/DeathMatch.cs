using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathMatch : GameManager
{
    public static DeathMatch DM;
    public Transform[] spawnPointsTeamOne;
    public Transform[] spawnPointsTeamTwo;
    public GameObject teamPanel;

    public float StartTime;
    public float currentTime;
    public float waitTime;

    public List<PhotonPlayer> players = new List<PhotonPlayer>();
    void Awake()
    {
        DM = this;
    }

    public void UpdateTeam(int whichTeam) //Когда выбрал команду
    {
        nextPlayersTeam = whichTeam;
        PhotonRoom.room.myPhotonNetworkPlayer.GetComponent<PhotonPlayer>().CallGetTeam();
        teamPanel.SetActive(false);
    }

    public void RespawnPlayer(GameObject hud, GameObject camera, GameObject itemPool)
    {
        PhotonPlayer pl = null;
        foreach (var player in players)
        {
            if (player.PV.IsMine)
            {
                pl = player;
            }
            foreach (var item in PhotonNetwork.PlayerList)
            {
                if (item.IsLocal)
                {
                    StartCoroutine(WaitForRespawn(pl, waitTime, hud, camera, itemPool));
                }
            }
        }
    }

    IEnumerator WaitForRespawn(PhotonPlayer player, float waitTime, GameObject hud, GameObject camera, GameObject itemPool)
    {
        yield return new WaitForSeconds(waitTime);

        Destroy(hud);

        if(GMSetting.setting.isFirstLaunch)
            Destroy(itemPool);

        Destroy(camera);
        player.canSpawnPlayer = true;
    }
}
