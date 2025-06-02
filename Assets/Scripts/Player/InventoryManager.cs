using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    public enum ItemType
    {
        Revolver,
        Notebook
    }

    [Header("Revolver References")]
    [SerializeField] private GameObject _revolverActive;
    [SerializeField] private GameObject _revolverInactive;

    [Header("Notebook References")]
    [SerializeField] private GameObject _notebookActive;
    [SerializeField] private GameObject _notebookInactive;

    private Dictionary<ItemType, (GameObject activeObj, GameObject inactiveObj)> _items;

    private ItemType? _equippedItem = null;
    public bool IsEquipped => _equippedItem.HasValue;
    public ItemType? EquippedItem => _equippedItem;

    public PlayerInteract playerInt;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        InitializeItems();
    }

    private void InitializeItems()
    {
        _items = new Dictionary<ItemType, (GameObject, GameObject)>
        {
            { ItemType.Revolver, (_revolverActive, _revolverInactive) },
            { ItemType.Notebook, (_notebookActive, _notebookInactive) }
        };

        foreach (var item in _items.Values)
        {
            if (item.activeObj != null) item.activeObj.SetActive(false);
            if (item.inactiveObj != null) item.inactiveObj.SetActive(true);
        }
    }

    private void Update()
    {
        if (!IsEquipped) return;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            DropCurrentItem();
        }
    }


    public void Equip(ItemType itemType)
    {
        if (_equippedItem.HasValue)
        {
            SetItemActive(_equippedItem.Value, false);
            SetItemInactive(_equippedItem.Value, true);
        }

        SetItemInactive(itemType, false);
        SetItemActive(itemType, true);
        playerInt.StopAllCoroutines();
        playerInt._guidText.text = "";
        playerInt._interactionText.text = "";

        playerInt.enabled = false;
        _equippedItem = itemType;
    }

    public void DropCurrentItem()
    {
        var itemType = _equippedItem.Value;

        if (itemType == ItemType.Notebook)
        {
            StartCoroutine(DropItemWithDelay(itemType));
        }
        else
        {
            SetItemActive(itemType, false);
            SetItemInactive(itemType, true);
            _equippedItem = null;
            playerInt.enabled = true;
        }
        
    }

    private IEnumerator DropItemWithDelay(ItemType itemType)
    {
        if (_items.TryGetValue(itemType, out var item))
        {
            OpenNotebook note = item.activeObj.GetComponentInChildren<OpenNotebook>();
            if (note.IsOpen)
            {
                note.CloseNotebook();
                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                 note.CloseNotebook();
            }
        }
        SetItemActive(itemType, false);
        SetItemInactive(itemType, true);
        playerInt.enabled = true;
        _equippedItem = null;
    }

    private void SetItemActive(ItemType itemType, bool isActive)
    {
        if (_items.TryGetValue(itemType, out var item))
        {
            if (item.activeObj != null)
                item.activeObj.SetActive(isActive);
        }
    }

    private void SetItemInactive(ItemType itemType, bool isActive)
    {
        if (_items.TryGetValue(itemType, out var item))
        {
            if (item.inactiveObj != null)
                item.inactiveObj.SetActive(isActive);
        }
    }

    public bool IsItemEquipped(ItemType itemType)
    {
        return _equippedItem.HasValue && _equippedItem.Value == itemType;
    }

}
