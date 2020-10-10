using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager GM;
    public GameMode mode;
    public int nextPlayersTeam;
    public Transform[] spawnPoints;


    public override void OnEnable()
    {
        if(GameManager.GM == null)
        {
            GameManager.GM = this;
        }
        mode = GMSetting.setting.mode;
    }
    public virtual void CheckAlivePlayers()
    {

    }
}
