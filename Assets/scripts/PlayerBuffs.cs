using UnityEngine;
using System.Collections;

public class PlayerBuffs : MonoBehaviour 
{
    [Header("Current Stats")]
    public float moveSpeed = 5f;
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public float attackDamage = 10f;
    public float knockbackForce = 0f;
    
    [Header("Defense Stats")]
    public float shieldCapacity = 0f;    
    public float maxShieldCapacity = 0f; 
    public float damageReduction = 0f; // 0.2 = 20% reduction

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
                StartCoroutine(TemporarySpeedBoost(data.value, speedBoostDuration));
                break;

            case BuffType.HPBoost:
                maxHealth += data.value;
                currentHealth += data.value;
                break;

            case BuffType.AttackDamage:
                StartCoroutine(TemporaryDamageBoost(data.value, 15f));
                break;

            case BuffType.KnockbackForce:
                knockbackForce += data.value;
                break;

            case BuffType.DamageReduction:
                // Convert value (e.g. 10) to 0.10 and add to current reduction
                damageReduction += (data.value / 100f);
                // Cap reduction at 80% to keep the game challenging
                damageReduction = Mathf.Clamp(damageReduction, 0f, 0.8f);
                break;

            case BuffType.ShieldCapacity:
                maxShieldCapacity += data.value; 
                shieldCapacity += data.value;    
                break;
        }

        if (PlayerHealth.Instance != null)
            PlayerHealth.Instance.RefreshUI();

        Debug.Log($"Applied {data.buffType}. Reduction now: {damageReduction * 100}%");
    }

    private IEnumerator TemporarySpeedBoost(float boostPercentage, float duration)
    {
        float amountToAdd = moveSpeed * (boostPercentage / 100f);
        moveSpeed += amountToAdd; 
        yield return new WaitForSeconds(duration);
        moveSpeed -= amountToAdd; 
        if (moveSpeed < 5f) moveSpeed = 5f;
    }

    private IEnumerator TemporaryDamageBoost(float boostAmount, float duration)
    {
        attackDamage += boostAmount;
        yield return new WaitForSeconds(duration);
        attackDamage -= boostAmount;
    }
}