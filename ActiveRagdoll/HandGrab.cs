using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandGrab : MonoBehaviour
{
    public int key;
    public float maxGrabbingMass = 10f;

    public Player myLocalPlayer;

    private GameObject myGrabbedGameObject;

    private bool inZone, grab;
    private IGrabble grabItem;

    void Start()
    {
        foreach (var player in PhotonRoom.room.photonPlayers)
        {
            if (player.IsLocal)
            {
                myLocalPlayer = player;
                break;
            }
        }
    }

    void OnTriggerStay(Collider other)
    {
        Rigidbody otherRigid = other.GetComponent<Rigidbody>();
        grabItem = other.GetComponent<IGrabble>();

        if (grabItem != null)
        {
            inZone = true;
            if (grab)
            {
                if (otherRigid != null && otherRigid.mass <= maxGrabbingMass)
                {
                    grabItem.GrabItem(transform);
                    myGrabbedGameObject = other.gameObject;
                } 

                grab = false;
            }
        } 
        
    }
    void OnTriggerExit(Collider other)
    {
        if (other.tag == "Grab")
            inZone = false;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(key))
        {
            if(myGrabbedGameObject != null)
            {
                grabItem.UnGrabItem();
                myGrabbedGameObject = null;
            }
            else if (inZone)
            {
                grab = true;
            }
        }
    }
}
