using UnityEngine;

public class PlayerBuffs : MonoBehaviour 
{
    [Header("Current Stats")]
    public float moveSpeed = 5f;
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public float attackDamage = 10f;
    public float knockbackForce = 5f;
    
    [Header("Defense Stats")]
    public float shieldCapacity = 0f;    
    public float maxShieldCapacity = 0f; 
    public float damageReduction = 0f;   

    void Start() 
    {
        if (BuffManager.Instance != null) 
            BuffManager.Instance.OnBuffApplied += ApplySelectedBuff;
            
        currentHealth = maxHealth;
    }

    public void ApplySelectedBuff(BuffData data) 
    {
        switch (data.buffType) 
        {
            case BuffType.MovementSpeed:
                moveSpeed += moveSpeed * (data.value / 100f);
                break;

            case BuffType.HPBoost:
                maxHealth += data.value;
                currentHealth += data.value; // Heal by the amount increased
                break;

            case BuffType.AttackDamage:
                attackDamage += data.value;
                break;

            case BuffType.KnockbackForce:
                knockbackForce += data.value;
                break;

            case BuffType.DamageReduction:
                damageReduction += (data.value / 100f);
                damageReduction = Mathf.Min(damageReduction, 0.8f);
                break;

            case BuffType.ShieldCapacity:
                maxShieldCapacity += data.value; 
                shieldCapacity += data.value;    
                break;
        }

        // --- CRITICAL: TELL HEALTH TO REFRESH THE UI ---
        if (PlayerHealth.Instance != null)
        {
            PlayerHealth.Instance.RefreshUI();
        }

        Debug.Log($"Applied {data.buffType}. Stats and UI updated!");
    }
}