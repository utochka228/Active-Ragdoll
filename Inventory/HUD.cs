using Photon.Pun;
using RichTextPlugin;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class HUD : MonoBehaviour
{
    public static HUD instance;
    public Inventory Inventory;
    public Transform inventoryPanel;
    public GameObject MessagePanel;

    public Text moneyText;

    private PhotonView PV;
    void Awake()
    {
        if (instance == null)
            instance = this;

        
    }
	// Use this for initialization
	void Start ()
    {
        Inventory = GMSetting.setting.inventory;
        PV = transform.parent.GetComponent<PhotonView>();
        if (PV.IsMine)
        {
            transform.SetParent(null);
            GMSetting.setting.moneyText = moneyText;
            GMSetting.setting.UpdateMoneyUI();
        }
        else
        {
            Destroy(gameObject);
        }
        
        Inventory.ItemAdded += InventoryScript_ItemAdded;
        Inventory.ItemRemoved += Inventory_ItemRemoved;

        if(Inventory.canUpdateUI) //Обновляем ХУД после загрузки инвентаря на сцене
            UpdateHUD();

    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
            inventoryPanel.gameObject.SetActive(!inventoryPanel.gameObject.activeSelf);

        if (inventoryPanel == null)
            inventoryPanel = GameObject.FindGameObjectWithTag("inventoryPanel").transform;

    }

    void UpdateHUD()
    {
        foreach(InventoryItemBase item in Inventory.inventory)
        {
            InventoryScript_ItemAdded(Inventory, new InventoryEventArgs(item));
        }
        Inventory.canUpdateUI = false;
    }

    private void InventoryScript_ItemAdded(object sender, InventoryEventArgs e)
    {
        int index = -1;
        if(inventoryPanel != null)
        {
            foreach (Transform slot in inventoryPanel)
            {
                index++;

                // Border... Image
                Transform imageTransform = slot.GetChild(0).GetChild(0);
                Transform textTransform = slot.GetChild(0).GetChild(1);
                Image image = imageTransform.GetComponent<Image>();
                Text txtCount = textTransform.GetComponent<Text>();
                ItemDragHandler itemDragHandler = imageTransform.GetComponent<ItemDragHandler>();

                if (index == e.Item.Slot.Id)
                {
                    e.Item.SetItemSlotID = e.Item.Slot.Id;
                    image.enabled = true;
                    image.sprite = e.Item.Image;

                    int itemCount = e.Item.Slot.Count;
                    if (itemCount > 1)
                        txtCount.text = itemCount.ToString();
                    else
                        txtCount.text = "";


                    // Store a reference to the item
                    itemDragHandler.Item = e.Item;

                    break;
                }
            }
        }
    }

    private void Inventory_ItemRemoved(object sender, InventoryEventArgs e)
    {
        int index = -1;
        if(inventoryPanel != null)
        {
            foreach (Transform slot in inventoryPanel)
            {
                if (slot == null)
                {
                    Debug.Log("SLOT IS NULL");
                    continue;
                }
                index++;

                Transform imageTransform = slot.GetChild(0).GetChild(0);
                Transform textTransform = slot.GetChild(0).GetChild(1);

                Image image = imageTransform.GetComponent<Image>();
                Text txtCount = textTransform.GetComponent<Text>();

                ItemDragHandler itemDragHandler = imageTransform.GetComponent<ItemDragHandler>();

                // We found the item in the UI
                if (itemDragHandler.Item == null)
                    continue;

                // Found the slot to remove from
                if (e.Item.Slot.Id == index)
                {
                    int itemCount = e.Item.Slot.Count;
                    itemDragHandler.Item = e.Item.Slot.FirstItem;

                    if (itemCount < 2)
                    {
                        txtCount.text = "";
                    }
                    else
                    {
                        txtCount.text = itemCount.ToString();
                    }

                    if (itemCount == 0)
                    {
                        image.enabled = false;
                        image.sprite = null;
                    }
                    break;
                }

            }
        }
    }

    private bool mIsMessagePanelOpened = false;

    public bool IsMessagePanelOpened
    {
        get { return mIsMessagePanelOpened; }
    }

    public void OpenMessagePanel(InteractableItemBase item)
    {
        MessagePanel.SetActive(true);

        Text mpText = MessagePanel.transform.Find("Text").GetComponent<Text>();
        mpText.text = item.InteractText + " " + RichText.Paint(item.Name, item.itemNameColor, true, false);


        mIsMessagePanelOpened = true;
    }

    public void OpenMessagePanel(string text)
    {
        MessagePanel.SetActive(true);

        Text mpText = MessagePanel.transform.Find("Text").GetComponent<Text>();
        mpText.text = text;


        mIsMessagePanelOpened = true;
    }

    public void CloseMessagePanel()
{
        MessagePanel.SetActive(false);

        mIsMessagePanelOpened = false;
    }
    public void GameEnded()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonRoom.room.PV.RPC("RPC_DoLastActions", RpcTarget.All); 

            PhotonNetwork.CurrentRoom.IsVisible = true;
            PhotonNetwork.CurrentRoom.IsOpen = true;
            PhotonNetwork.LoadLevel(0);
        }
    }
    //KillPanel
    public Transform gameKills;
    public void SpawnPanel(string killer, string killed)
    {
        GameObject killerPanel = Resources.Load("KillPanel") as GameObject;
        KillerPanel panel = Instantiate(killerPanel, gameKills).GetComponent<KillerPanel>();
        panel.killer.text = killer;
        panel.killed.text = killed;
    }
}
