// ================================================================
// PlayerUsage.cs
// Attach to: Player GameObject
// ================================================================
using UnityEngine;

public class PlayerUsage : MonoBehaviour
{
    [Header("Item References")]
    [SerializeField] private ItemData rumBottleData;    // drag your RumBottle ItemData asset here

    [Header("Input")]
    [SerializeField] private KeyCode useItemKey = KeyCode.Q;

    // ----------------------------------------------------------------
    private void Update()
    {
        if (Input.GetKeyDown(useItemKey))
            UseRumBottle();
    }

    // ----------------------------------------------------------------
    private void UseRumBottle()
    {
        // DrunkManager blocks usage during hangover
        if (DrunkManager.Instance.IsHangover)
        {
            Debug.Log("[PlayerUsage] Cannot drink during hangover.");
            return;
        }

        Inventory.Instance.UseItem(rumBottleData);
    }
}