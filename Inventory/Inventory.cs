using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory instance;
    public List<InventoryItemBase> inventory = new List<InventoryItemBase>();

    private int SLOTS = 9;
    public IList<InventorySlot> mSlots = new List<InventorySlot>();

    public event EventHandler<InventoryEventArgs> ItemAdded;
    public event EventHandler<InventoryEventArgs> ItemRemoved;
    public event EventHandler<InventoryEventArgs> ItemUsed;

    public bool canUpdateUI; //Вспомогательная переменная для обновления ХУДА
    private bool isLoadingData;
    void Awake()
    {
        if (Inventory.instance == null)
        {
            Inventory.instance = this;
        }
        else
        {
            if (Inventory.instance != this)
            {
                Destroy(Inventory.instance.gameObject);
                Inventory.instance = this;
            }
        }
        DontDestroyOnLoad(this);
    }

    void Start()
    {
        if (GMSetting.setting.isFirstLaunch)
        {
            for (int i = 0; i < SLOTS; i++)
            {
                mSlots.Add(new InventorySlot(i));
            }
        }
    }

    private InventorySlot FindStackableSlot(InventoryItemBase item)
    {
        foreach (InventorySlot slot in mSlots)
        {
            if (slot.IsStackable(item))
                return slot;
        }
        return null;
    }

    private InventorySlot FindNextEmptySlot()
    {
        foreach (InventorySlot slot in mSlots)
        {
            if (slot.IsEmpty)
                return slot;
        }
        return null;
    }

    public void AddItem(InventoryItemBase item)
    {
        InventorySlot freeSlot = FindStackableSlot(item);
        if (freeSlot == null)
        {
            freeSlot = FindNextEmptySlot();
        }
        if (freeSlot != null)
        {
            if(!isLoadingData)
                freeSlot.AddItem(item);

            inventory.Add(item);

            if (ItemAdded != null)
            {
                ItemAdded(this, new InventoryEventArgs(item));
            }
        }
    }

    internal void UseItem(InventoryItemBase item)
    {
        if (ItemUsed != null)
        {
            ItemUsed(this, new InventoryEventArgs(item));
        }

        item.OnUse();
    }

    public void RemoveItem(InventoryItemBase item)
    {
        foreach (InventorySlot slot in mSlots)
        {
            if (slot.Remove(item))
            {
                inventory.Remove(item);
                if (ItemRemoved != null)
                {
                    ItemRemoved(this, new InventoryEventArgs(item));
                }
                break;
            }

        }
    }

    public int GetFreeSlotsCount()
    {
        int result = 0;
        foreach (InventorySlot slot in mSlots)
        {
            if (slot.IsEmpty) result++;
        }
        return result;
    }
    
    void LoadData(object sender, EventArgs e)
    {
        isLoadingData = true;
        foreach (InventorySlot slot in GMSetting.setting.slotsData)
        {
            if (slot != null)
            {
                mSlots.Add(slot);
            }
        }
        foreach (InventoryItemBase item in GMSetting.setting.inventoryData)
        {
            if(item != null)
            {
                GameObject obj = PhotonNetwork.Instantiate("ItemPrefabs/" + item.prefabName,
                    item.transform.position, item.transform.rotation).gameObject;
                InventoryItemBase newItem = obj.GetComponent<InventoryItemBase>();
                newItem.Slot = mSlots[item.Slot.Id];
                obj.gameObject.SetActive(false);
                obj.transform.SetParent(GMSetting.setting.itemPool);
                Destroy(item.gameObject);
                
                AddItem(newItem);
            }
                
        }
        isLoadingData = false;
        canUpdateUI = true;

       
    }

    public void SaveData()
    {
        GMSetting.setting.ClearData();
        GameData.data.freeSlotsForBuy = GetFreeSlotsCount();
        foreach(InventoryItemBase item in inventory)
        {
            if (item != null)
                GMSetting.setting.inventoryData.Add(item);
        }
        
        foreach(InventorySlot slot in mSlots)
        {
            if (slot != null)
                GMSetting.setting.slotsData.Add(slot);
        }
    }
}
