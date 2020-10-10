using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.IO;

public class PhotonPlayer : MonoBehaviour
{
    public PhotonView PV;
    public GameObject myAvatar;
    public int myTeam;
    public int myClass;

    public bool canSpawnPlayer = true;

    void Start()
    {
        PV = GetComponent<PhotonView>();
        if(PV.IsMine) myClass = (int)GMSetting.setting.characterClass;

        //DeathMatch.DM.players.Add(this);

        
    }

    public void CallGetTeam()
    {
        if (PV.IsMine)
        {
            PV.RPC("RPC_GetTeam", RpcTarget.AllBuffered);
        }
    }

    // Update is called once per frame
    void Update()
    {
        //if(EndlessWater.instance.viewer == null && myAvatar != null)
        //{
        //    if (PV.IsMine)
        //        EndlessWater.instance.viewer = myAvatar.transform;
        //}

        if (canSpawnPlayer)
        {
            if (myAvatar == null && myTeam != 0 && GameManager.GM.mode == GameMode.DeathMatch)
            {
                if (myTeam == 1)
                {
                    int spawnPicker = Random.Range(0, DeathMatch.DM.spawnPointsTeamOne.Length);
                    if (PV.IsMine)
                    {
                        myAvatar = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "PlayerAvatar"),
                        DeathMatch.DM.spawnPointsTeamOne[spawnPicker].transform.position, DeathMatch.DM.spawnPointsTeamOne[spawnPicker].transform.rotation, 0);
                        canSpawnPlayer = false;
                    }
                }
                else
                {
                    int spawnPicker = Random.Range(0, DeathMatch.DM.spawnPointsTeamTwo.Length);
                    if (PV.IsMine)
                    {
                        myAvatar = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "PlayerAvatar"),
                        DeathMatch.DM.spawnPointsTeamTwo[spawnPicker].transform.position, DeathMatch.DM.spawnPointsTeamTwo[spawnPicker].transform.rotation, 0);
                        canSpawnPlayer = false;
                    }
                }
            }
            if(myAvatar == null && GameManager.GM.mode != GameMode.DeathMatch)
            {
                int spawnPicker = Random.Range(0, GameManager.GM.spawnPoints.Length);
                if (PV.IsMine)
                {
                    myAvatar = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "PlayerAvatar"),
                  GameManager.GM.spawnPoints[spawnPicker].transform.position, GameManager.GM.spawnPoints[spawnPicker].transform.rotation, 0);
                }
            }
        }
    }

    [PunRPC]
    void RPC_GetTeam()
    {
        myTeam = GameManager.GM.nextPlayersTeam;
    }
}
