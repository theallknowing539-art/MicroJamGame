// ================================================================
// Inventory.cs
// Attach to: Player GameObject
// ================================================================
using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; }

    // the actual storage — ItemData as key, count as value
    private Dictionary<ItemData, int> _items = new Dictionary<ItemData, int>();

    // fires whenever anything in the inventory changes
    // any UI or system that cares subscribes to this
    public event Action OnInventoryChanged;

    // ----------------------------------------------------------------
    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ----------------------------------------------------------------
    public void AddItem(ItemData item)
    {
        if (item == null) return;

        if (_items.ContainsKey(item))
            _items[item]++;
        else
            _items[item] = 1;

        Debug.Log($"[Inventory] Added {item.itemName}. Count: {_items[item]}");
        OnInventoryChanged?.Invoke();
    }

    // ----------------------------------------------------------------
    public void UseItem(ItemData item)
    {
        if (item == null) return;

        if (!_items.ContainsKey(item) || _items[item] <= 0)
        {
            Debug.Log($"[Inventory] No {item.itemName} to use.");
            return;
        }

        // apply effects
        PlayerHealth.Instance.Heal(item.healAmount);
        DrunkManager.Instance.RaiseInstability(item.instabilityIncrease);

        // remove one from inventory
        _items[item]--;
        if (_items[item] <= 0)
            _items.Remove(item);

        Debug.Log($"[Inventory] Used {item.itemName}.");
        OnInventoryChanged?.Invoke();
    }

    // ----------------------------------------------------------------
    // Returns how many of a given item the player has
    // UI calls this to display the count
    // ----------------------------------------------------------------
    public int GetCount(ItemData item)
    {
        if (item == null) return 0;
        return _items.ContainsKey(item) ? _items[item] : 0;
    }

    // ----------------------------------------------------------------
    // Returns true if the player has at least one of the given item
    // ----------------------------------------------------------------
    public bool HasItem(ItemData item)
    {
        return GetCount(item) > 0;
    }
}