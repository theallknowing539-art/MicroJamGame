using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance;

    [Header("Current Multipliers")]
    public float damageMultiplier = 1f;
    public float speedMultiplier = 1f;
    public float knockbackMultiplier = 1f;
    public float damageResistance = 0f; // 0.1 = 10% reduction
    public float maxHealthBonus = 0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void AddBuff(BuffData buff)
    {
        float bonus = buff.GetMultiplier(); // e.g., 0.05 for 5%

        switch (buff.buffType)
        {
            case BuffType.AttackDamage:
                damageMultiplier += bonus;
                break;
            case BuffType.MovementSpeed:
                speedMultiplier += bonus;
                // Update player movement script here if needed
                break;
            case BuffType.KnockbackForce:
                knockbackMultiplier += bonus;
                break;
            case BuffType.DamageReduction:
                damageResistance += bonus;
                break;
            case BuffType.HPBoost:
                maxHealthBonus += (bonus * 100); // Adding flat health or % based on your preference
                break;
        }

        Debug.Log($"Stat Updated: {buff.buffType} is now {bonus}% stronger!");
    }
}