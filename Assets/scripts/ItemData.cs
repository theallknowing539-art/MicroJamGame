// ================================================================
// ItemData.cs
// Create assets: Right Click → Create → Game → Item Data
// ================================================================
using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Game/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Info")]
    public string itemName = "Item";
    public Sprite icon;

    [Header("Effects on Use")]
    public float healAmount = 25f;
    public float instabilityIncrease = 20f;

    [Header("World")]
    public GameObject pickupPrefab;
}