using Photon.Pun;
using RichTextPlugin;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum GameMode { Time, Survival, DeathMatch, WaveInvasion };
public enum Map { Bahams, Dust, Miragge, Nuke };
public enum Class { Captain, Cook, Shooter, Pirate };
public class GMSetting : MonoBehaviour
{
    public static GMSetting setting;
    

    public GameMode mode;
    public Map map;
    public Class characterClass;

    public Action MoneyChanged; //Когда деньги изменились
    public int money = 100;
    public int startMoney = 100;
    public Text moneyText;
    public List<InventoryItemBase> inventoryData = new List<InventoryItemBase>();
    public List<InventorySlot> slotsData = new List<InventorySlot>();

    public bool isFirstLaunch;
    public bool isGameEnded;
    public int freeSlotsForBuy;
    public Inventory inventory;
    public Transform itemPool;

    public GameObject gameDataPrefab;

    public PhotonView PV;

    void Awake()
    {
        if (GMSetting.setting == null)
        {
            GMSetting.setting = this;
        }
        else
        {
            if(GMSetting.setting != this)
            {
                Destroy(GMSetting.setting.gameObject);
                GMSetting.setting = this;
            }
        }

        if (GameData.data != null)
            LoadData();

        if (isFirstLaunch)
        {
            GameObject dataObj = Instantiate(gameDataPrefab);
            DontDestroyOnLoad(dataObj);
        }
        
        DontDestroyOnLoad(this);
    }
    void Start()
    {
        if (moneyText != null)
            UpdateMoneyUI();
    }
    void Update()
    {
        if (inventory == null)
            inventory = Inventory.instance;
        //Проверить почему он осутствует, когда не впервые заходим на сцену
        //if (itemPool == null && !isFirstLaunch) 
        //itemPool = GameObject.FindGameObjectWithTag("playerItemPool").transform;

        if (Input.GetKeyDown(KeyCode.M))
            AddMoney(13);

    }
    public void ClearData()
    {
        inventoryData.RemoveAll(x => true);
        slotsData.RemoveAll(x => true);
    }
    void LoadData()
    {
        isGameEnded = GameData.data.isGameEnded;
        isFirstLaunch = GameData.data.isFirstLaunch;
        money = GameData.data.money;
        freeSlotsForBuy = GameData.data.freeSlotsForBuy;

        foreach (InventorySlot slot in GameData.data.slotsData)
        {
            if (slot != null)
            {
                slotsData.Add(slot);
            }
        }
        foreach (InventoryItemBase item in GameData.data.inventoryData)
        {
            if (item != null)
                inventoryData.Add(item);
        }
    }

    public void SaveData()
    {
        GameData.data.isGameEnded = isGameEnded;
        GameData.data.isFirstLaunch = isFirstLaunch;
        GameData.data.money = money;
        GameData.data.ClearData();

        foreach (InventoryItemBase item in inventoryData)
        {
            if (item != null)
                GameData.data.inventoryData.Add(item);
        }

        foreach (InventorySlot slot in slotsData)
        {
            if (slot != null)
                GameData.data.slotsData.Add(slot);
        }
    }

    public void AddMoney(int count)
    {
        PV.RPC("RPC_AddMoney", RpcTarget.AllBuffered, count);
    }
    [PunRPC]
    void RPC_AddMoney(int count)
    {
        money += count;
        UpdateMoneyUI();
        MoneyHasBeenChanged();
    }
    public void TakeMoney(int count)
    {
        PV.RPC("RPC_TakeMoney", RpcTarget.AllBuffered, count);
    }

    [PunRPC]
    void RPC_TakeMoney(int count)
    {
        money -= count;
        UpdateMoneyUI();
        MoneyHasBeenChanged();
    }
    public void UpdateMoneyUI()
    {
        moneyText.text = "Team money: " + RichText.Paint(money.ToString(), Color.yellow, false, false);
    }
    void MoneyHasBeenChanged()
    {
        if (MoneyChanged != null)
            MoneyChanged();
    }
}
