// ================================================================
// PickupBehaviour.cs — updated
// ================================================================
using UnityEngine;

public class PickupBehaviour : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemData _itemData;

    public string InteractionHint => $"Pick up {_itemData.itemName}";

    public void Interact()
    {
        if (_itemData == null)
        {
            Debug.LogWarning("[PickupBehaviour] No ItemData assigned.");
            return;
        }

        switch (_itemData.itemType)
        {
            case ItemType.RumBottle:
                Inventory.Instance.AddItem(_itemData);
                break;

            case ItemType.Ammo:
                // find the gun and add ammo directly to reserve
                Gun gun = FindObjectOfType<Gun>();
                if (gun != null)
                {
                    gun.AddReserveAmmo(_itemData.ammoAmount);
                    Debug.Log($"[PickupBehaviour] Added {_itemData.ammoAmount} ammo to reserve.");
                }
                break;
        }

        Destroy(gameObject);
    }
}