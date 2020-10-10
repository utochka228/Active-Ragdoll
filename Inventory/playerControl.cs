using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.EventSystems;
using System.Linq;

public class playerControl : MonoBehaviour
{
    #region Private Members

    private Animator _animator;

    private CharacterController _characterController;

    private float Gravity = 20.0f;

    private Vector3 _moveDirection = Vector3.zero;

    private InventoryItemBase mCurrentItem = null;

    #endregion

    #region Public Members

    public float Speed = 5.0f;

    public float RotationSpeed = 240.0f;

    private Inventory Inventory;

    private GameObject Hand;

    public HUD hud;

    public float JumpSpeed = 7.0f;

    public PhotonView photonView;

    public GameObject Camera;

    public Transform itemPool;

    private Character character;

    public bool canMove;

    void Awake()
    {
        if (!photonView.IsMine)
            this.enabled = false;

        if (GMSetting.setting.isFirstLaunch)
        {
            if (photonView.IsMine)
            {
                GameObject obj = Resources.Load("PlayerItemPool") as GameObject;
                itemPool = Instantiate(obj).transform;
                itemPool.SetParent(null);
                DontDestroyOnLoad(itemPool);
            }
            else
            {
                Camera.SetActive(false);
            }
        }
        else
        {
            if (photonView.IsMine)
            {
                itemPool = GameObject.FindGameObjectWithTag("playerItemPool").transform;
            }
                
        }
        if(photonView.IsMine)
            GMSetting.setting.itemPool = itemPool;
    }
    #endregion

    // Use this for initialization
    void Start()
    {
        Inventory = GMSetting.setting.inventory;
        photonView = GetComponent<PhotonView>();

        _animator = GetComponent<Animator>();
        _characterController = GetComponent<CharacterController>();
        Inventory.ItemUsed += Inventory_ItemUsed;
        Inventory.ItemRemoved += Inventory_ItemRemoved;

        InvokeRepeating("IncreaseHunger", 0, HungerRate);

        
        character = GetComponent<Character>();
        if(PlayerInfo.PI.maxHealthStat != null)
            character.MaxHealth.AddModifier(PlayerInfo.PI.maxHealthStat);
        if (PlayerInfo.PI.damageStat != null)
            character.Damage.AddModifier(PlayerInfo.PI.damageStat);
        if (PlayerInfo.PI.speedStat != null)
            character.Speed.AddModifier(PlayerInfo.PI.speedStat);
        if (PlayerInfo.PI.defenseStat != null)
            character.Defense.AddModifier(PlayerInfo.PI.defenseStat);
    }

    #region Inventory

    private void Inventory_ItemRemoved(object sender, InventoryEventArgs e)
    {
        InventoryItemBase item = e.Item;

        GameObject goItem = (item as MonoBehaviour).gameObject;
        goItem.SetActive(true);
        goItem.transform.parent = null;

    }

    private void SetItemActive(InventoryItemBase item, bool active)
    {
        GameObject currentItem = (item as MonoBehaviour).gameObject;
        currentItem.SetActive(active);
        currentItem.transform.parent = active ? Hand.transform : null;
        currentItem.GetComponent<Rigidbody>().isKinematic = true;
    }

    private void Inventory_ItemUsed(object sender, InventoryEventArgs e)
    {
        if (e.Item.ItemType != EItemType.Consumable)
        {
            // If the player carries an item, un-use it (remove from player's hand)
            if (mCurrentItem != null)
            {
                SetItemActive(mCurrentItem, false);
            }

            InventoryItemBase item = e.Item;

            // Use item (put it to hand of the player)
            SetItemActive(item, true);

            mCurrentItem = e.Item;
        }

    }

    private int Attack_1_Hash = Animator.StringToHash("Base Layer.Attack_1");

    public bool IsAttacking
    {
        get
        {
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.fullPathHash == Attack_1_Hash)
            {
                return true;
            }
            return false;
        }
    }

    public void DropCurrentItem()
    {
        _animator.SetTrigger("tr_drop");

        GameObject goItem = (mCurrentItem as MonoBehaviour).gameObject;

        Inventory.RemoveItem(mCurrentItem);

        // Throw animation
        Rigidbody rbItem = goItem.GetComponent<Rigidbody>();
        if (rbItem != null)
        {
            rbItem.AddForce(transform.forward * 2.0f, ForceMode.Impulse);
            rbItem.isKinematic = false;
            Invoke("DoDropItem", 0.25f);
        }

    }

    public void DoDropItem()
    {

        // Remove Rigidbody
        //Destroy((mCurrentItem as MonoBehaviour).GetComponent<Rigidbody>());

        mCurrentItem = null;
    }

    #endregion

    #region Health & Hunger

    [Tooltip("Amount of health")]
    public int Health = 100;

    [Tooltip("Amount of food")]
    public int Food = 100;

    [Tooltip("Rate in seconds in which the hunger increases")]
    public float HungerRate = 0.5f;

    public void IncreaseHunger()
    {
        //Food--;
        //if (Food < 0)
          //  Food = 0;


        if (IsDead)
        {
            CancelInvoke();
            _animator.SetTrigger("death");
        }
    }

    public bool IsDead
    {
        get
        {
            return Health == 0 || Food == 0;
        }
    }

    public bool IsArmed
    {
        get
        {
            if (mCurrentItem == null)
                return false;

            return mCurrentItem.ItemType == EItemType.Weapon;
        }
    }

    #endregion


    void FixedUpdate()
    {
        if (!IsDead)
        {
            // Drop item
            if (mCurrentItem != null && Input.GetKeyDown(KeyCode.R))
            {
                DropCurrentItem();
            }
        }
    }

    private bool mIsControlEnabled = true;

    public void EnableControl()
    {
        mIsControlEnabled = true;
    }

    public void DisableControl()
    {
        mIsControlEnabled = false;
    }
    
    // Update is called once per frame
    void Update()
    {
        if (!photonView.IsMine) return;

        if (canMove)
        {
            if (Hand == null)
            {
                Hand = GameObject.FindGameObjectWithTag("Hand").gameObject;
            }

            if (!IsDead && mIsControlEnabled)
            {
                // Interact with the item
                if (mInteractItem != null && Input.GetKeyDown(KeyCode.F))
                {
                    // Interact animation
                    mInteractItem.OnInteractAnimation(_animator);
                }

                // Execute action with item
                if (mCurrentItem != null && Input.GetMouseButtonDown(0))
                {
                    // Dont execute click if mouse pointer is over uGUI element
                    if (!EventSystem.current.IsPointerOverGameObject())
                    {
                        // TODO: Logic which action to execute has to come from the particular item
                        _animator.SetTrigger("attack_1");
                    }
                }

                // Get Input for axis
                float h = Input.GetAxis("Horizontal");
                float v = Input.GetAxis("Vertical");

                // Calculate the forward vector
                Vector3 camForward_Dir = Vector3.Scale(Camera.transform.forward, new Vector3(1, 0, 1)).normalized;
                Vector3 move = v * camForward_Dir + h * Camera.transform.right;

                if (move.magnitude > 1f) move.Normalize();

                // Calculate the rotation for the player
                move = transform.InverseTransformDirection(move);

                // Get Euler angles
                float turnAmount = Mathf.Atan2(move.x, move.z);

                transform.Rotate(0, turnAmount * RotationSpeed * Time.deltaTime, 0);

                if (_characterController.isGrounded)
                {
                    _moveDirection = transform.forward * move.magnitude;

                    _moveDirection *= Speed;

                    if (Input.GetButton("Jump"))
                    {
                        _animator.SetBool("is_in_air", true);
                        _moveDirection.y = JumpSpeed;

                    }
                    else
                    {
                        _animator.SetBool("is_in_air", false);
                        _animator.SetBool("run", move.magnitude > 0);
                    }
                }

                _moveDirection.y -= Gravity * Time.deltaTime;

                _characterController.Move(_moveDirection * Time.deltaTime);
            }
        }
        
    }

    public void InteractWithItem()
    {
        if (mInteractItem != null)
        {
            mInteractItem.OnInteract();

            if (mInteractItem is InventoryItemBase)
            {
                InventoryItemBase inventoryItem = mInteractItem as InventoryItemBase;
                Inventory.AddItem(inventoryItem);
                UpdateMInteractItem(mInteractItem);
                inventoryItem.OnPickup();

                if (inventoryItem.UseItemAfterPickup)
                {
                    Inventory.UseItem(inventoryItem);
                }
            }
        }

        //hud.CloseMessagePanel();

        //mInteractItem = null;
    }

    private InteractableItemBase mInteractItem = null;
    private List<InteractableItemBase> interactableItemsInRange = new List<InteractableItemBase>();
    private void OnTriggerEnter(Collider other)
    {
        InteractableItemBase item = other.GetComponent<InteractableItemBase>();

        if (item != null)
        {
            if (item.CanInteract(other))
            {
                interactableItemsInRange.Add(item);

                
                if (interactableItemsInRange.Count > 0)
                {
                    SetCloserItem();
                }
                else
                {
                    mInteractItem = item;
                    if (photonView.IsMine)
                        hud.OpenMessagePanel(mInteractItem);
                }
            }
        }
    }

    void SetCloserItem()
    {
        float minDistance = interactableItemsInRange.Where(x=> 
        x.GetDistanceToPlayer(transform.position) < 2f).Min(x => x.distance);
        var closerItem = interactableItemsInRange.Where(x => x.distance == minDistance);
        foreach (InteractableItemBase itemInRange in closerItem)
        {
            mInteractItem = itemInRange;
            if(photonView.IsMine)
                hud.OpenMessagePanel(mInteractItem);
        }
    }

    void UpdateMInteractItem(InteractableItemBase item)
    {
        interactableItemsInRange.Remove(item);
        if (interactableItemsInRange.Count > 1)
        {
            SetCloserItem();
        }
        else if (interactableItemsInRange.Count > 0)
        {
            mInteractItem = interactableItemsInRange[0];
            if (photonView.IsMine)
                hud.OpenMessagePanel(mInteractItem);
        }
        if (interactableItemsInRange.Count == 0)
        {
            if (photonView.IsMine)
                hud.CloseMessagePanel();
            mInteractItem = null;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        InteractableItemBase item = other.GetComponent<InteractableItemBase>();
        if (item != null)
        {
            UpdateMInteractItem(item);
        }
    }
}
