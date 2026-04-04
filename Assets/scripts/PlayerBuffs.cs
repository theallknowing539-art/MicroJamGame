using UnityEngine;
using System.Collections;

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

    [Header("Settings")]
    [SerializeField] private float speedBoostDuration = 15f;

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
                // Start the 15-second timer for Boarding Dash
                StartCoroutine(TemporarySpeedBoost(data.value, speedBoostDuration));
                break;

            case BuffType.HPBoost:
                // Health stays permanent (Eternal Grog)
                maxHealth += data.value;
                currentHealth += data.value;
                break;

            case BuffType.AttackDamage:
                //attackDamage += data.value; // Use this for Permanent
                StartCoroutine(TemporaryDamageBoost(data.value, 15f)); // Use this ONLY if you want a timer
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

        // Refresh UI immediately for health/shield changes
        if (PlayerHealth.Instance != null)
        {
            PlayerHealth.Instance.RefreshUI();
        }

        Debug.Log($"Applied {data.buffType} card!");
    }

    private IEnumerator TemporarySpeedBoost(float boostPercentage, float duration)
    {
        // Calculate the actual amount to add based on current speed
        float amountToAdd = moveSpeed * (boostPercentage / 100f);
        
        moveSpeed += amountToAdd; 
        Debug.Log($"<color=cyan>SPEED BOOST ACTIVE:</color> +{boostPercentage}% for {duration}s");

        // Wait for 15 seconds
        yield return new WaitForSeconds(duration);

        // Remove the boost
        moveSpeed -= amountToAdd; 
        
        // Ensure we don't go below the base speed due to rounding
        if (moveSpeed < 5f) moveSpeed = 5f;

        Debug.Log("<color=orange>SPEED BOOST EXPIRED:</color> Returning to normal speed.");
        
        // Refresh UI in case you have a speed bar or icon
        if (PlayerHealth.Instance != null)
        {
            PlayerHealth.Instance.RefreshUI(); 
        }
    }

    private IEnumerator TemporaryDamageBoost(float boostAmount, float duration)
{
    attackDamage += boostAmount;
    Debug.Log($"<color=red>DAMAGE BOOST ACTIVE!</color> +{boostAmount} DMG");

    yield return new WaitForSeconds(duration);

    attackDamage -= boostAmount;
    Debug.Log("<color=white>Damage Boost Expired.</color>");
}
}