// ================================================================
// PickupBehaviour.cs
// Attach to: Rum Bottle world prefab
// Requires: Collider (Is Trigger = off) on the same GameObject
// ================================================================
using UnityEngine;

public class PickupBehaviour : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemData _itemData;

    // ----------------------------------------------------------------
    // IInteractable
    // ----------------------------------------------------------------
    public string InteractionHint => $"Pick up {_itemData.itemName}";

    public void Interact()
    {
        if (_itemData == null)
        {
            Debug.LogWarning("[PickupBehaviour] No ItemData assigned.");
            return;
        }

        Inventory.Instance.AddItem(_itemData);
        Destroy(gameObject);
    }
}