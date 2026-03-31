// ================================================================
// DropTable.cs
// Create assets: Right Click → Create → Game → Drop Table
// ================================================================
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDropTable", menuName = "Game/Drop Table")]
public class DropTable : ScriptableObject
{
    [System.Serializable]
    public class DropEntry
    {
        public ItemData item;
        [Range(0f, 1f)] public float dropChance;
    }

    [SerializeField] private List<DropEntry> entries = new List<DropEntry>();

    // ----------------------------------------------------------------
    // Call this on enemy death — returns an ItemData to spawn
    // or null if nothing drops
    // ----------------------------------------------------------------
    public ItemData Roll()
    {
        foreach (DropEntry entry in entries)
        {
            float roll = Random.Range(0f, 1f);
            if (roll <= entry.dropChance)
                return entry.item;
        }
        return null;
    }
}