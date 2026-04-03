// ================================================================
// BuffData.cs
// Right click → Create → Game → Buff Data
// Create one asset per card (5 total)
// ================================================================
using UnityEngine;

[CreateAssetMenu(fileName = "NewBuff", menuName = "Game/Buff Data")]
public class BuffData : ScriptableObject
{
    [Header("Card Info")]
    public string buffName;
    public string description;
    public Sprite icon;
    public BuffType buffType;

    [Header("Level Values (index 0 = level 1, 1 = level 2, 2 = level 3)")]
    [Tooltip("Percentage values as decimals. 0.05 = 5%, 0.10 = 10%, 0.20 = 20%")]
    public float[] levelValues = { 0.05f, 0.10f, 0.20f };

    // ----------------------------------------------------------------
    // Returns the value for a given level (1 to 4)
    // Level 4 reuses level 3's value since there are only 3 tiers
    // ----------------------------------------------------------------
    public float GetValue(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, levelValues.Length - 1);
        return levelValues[index];
    }

    // ----------------------------------------------------------------
    // Returns description with the correct percentage for this level
    // ----------------------------------------------------------------
    public string GetDescription(int level)
    {
        float value = GetValue(level) * 100f;
        return $"{description} {value}%";
    }
}