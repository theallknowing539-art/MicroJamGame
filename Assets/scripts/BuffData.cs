using UnityEngine;

public enum BuffType
{
    [InspectorName("Krakens Belch")]
    KnockbackForce,

    [InspectorName("Scurvy Strike")]
    AttackDamage,

    [InspectorName("Eternal Grog")]
    HPBoost,

    [InspectorName("Boarding Dash")]
    MovementSpeed,

    [InspectorName("Brine Barrier")]
    ShieldCapacity,

    [InspectorName("Iron Ribs")]
    DamageReduction
}

[CreateAssetMenu(fileName = "NewBuffData", menuName = "Game/Buff Data")]
public class BuffData : ScriptableObject
{

    [Header("Spawn Settings")]
    public int requiredWave = 1;
    
    [Header("Display Info")]
    public string buffName;
    
    [TextArea(2, 5)]
    public string description; // e.g., "+5% Knockback Force"
    public Sprite cardSprite;  // Drag the corresponding card image here

    [Header("Settings")]
    public BuffType buffType;
    public int level;          // 1, 2, or 3
    
    [Tooltip("The raw percentage or flat value (e.g., 5 for 5%)")]
    public float value;        

    /// <summary>
    /// Returns the value as a multiplier (e.g., 5 becomes 0.05).
    /// Useful for math: CurrentSpeed * (1 + GetMultiplier())
    /// </summary>
    public float GetMultiplier()
    {
        return value / 100f;
    }
}