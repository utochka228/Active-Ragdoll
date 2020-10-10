using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EItemType
{
    Default,
    Consumable,
    Weapon,
    Bomb
}
[RequireComponent(typeof(DestroyItem))]
public class InteractableItemBase : MonoBehaviour
{
    public string Name;
    public Sprite Image;
    public string description;
    public int shopPrice;

    public string InteractText = "Press F to pickup the item";
    public Color itemNameColor = Color.white;

    public EItemType ItemType;

    protected PhotonView _pv;
    public float distance;

    public string prefabName;
    void Awake()
    {
        _pv = GetComponent<PhotonView>();
        if (PhotonNetwork.IsMasterClient)
        {
            if (_pv.ViewID == 3)
            {
                _pv.ViewID = PhotonNetwork.AllocateViewID(_pv.IsSceneView);
            }
        }
        
    }
    

    public virtual void OnInteractAnimation(Animator animator)
    {
        animator.SetTrigger("tr_pickup");
    }

    public virtual void OnInteract()
    {
    }

    public virtual bool CanInteract(Collider other)
    {
        return true;   
    }

    public float GetDistanceToPlayer(Vector3 playerPos)
    {
        distance = Vector3.Distance(playerPos, transform.position);
        return distance;
    }
}

public class InventoryItemBase : InteractableItemBase
{
    private int itemID;
    public int GetItemSlotID
    {
        get
        {
            return itemID;
        }
    }
    public int SetItemSlotID
    {
        set
        {
            if (value >= 0)
                itemID = value;
        }
    }
    public InventorySlot Slot
    {
        get; set;
    }

    public virtual void OnUse()
    {
        transform.localPosition = PickPosition;
        transform.localEulerAngles = PickRotation;
    }

    public virtual void OnDrop()
    {
        RaycastHit hit = new RaycastHit();
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, 1000))
        {
            ActivateItem(true);
            gameObject.transform.position = hit.point;
            gameObject.transform.eulerAngles = DropRotation;
            //PV.TransferOwnership(0);
        }
    }

    public virtual void OnPickup()
    {
        transform.SetParent(GMSetting.setting.itemPool);
        ActivateItem(false);
        //PV.TransferOwnership(PhotonNetwork.LocalPlayer);
    }

    public virtual void ActivateItem(bool state)
    {
    }

    public Vector3 PickPosition;

    public Vector3 PickRotation;

    public Vector3 DropRotation;

    public bool UseItemAfterPickup = false;

}
