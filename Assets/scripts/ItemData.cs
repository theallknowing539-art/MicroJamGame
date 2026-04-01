// ================================================================
// ItemData.cs — updated
// ================================================================
using UnityEngine;

public enum ItemType
{
    RumBottle,
    Ammo
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Game/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Info")]
    public string itemName = "Item";
    public Sprite icon;
    public ItemType itemType;

    [Header("Rum Bottle Effects")]
    public float healAmount = 25f;
    public float instabilityIncrease = 20f;

    [Header("Ammo")]
    public int ammoAmount = 6;

    [Header("World")]
    public GameObject pickupPrefab;
}