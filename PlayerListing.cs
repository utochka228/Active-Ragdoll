using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerListing : MonoBehaviour
{
    public Text classText;
    public Toggle readyToggle;
    public ToggleGroup toggleGroup;
    public GameObject checkMark;
    public GameObject hostIcon;
    public bool isReady;

    void Start()
    {
        
    }

    public void OnToggle()
    {
        PhotonRoom.room.OnToggle(gameObject);
        PhotonRoom.room.OnReadyCheckChanged();
         
    }


    void DeActivate()
    {
        readyToggle.interactable = false;
    }
    
}
