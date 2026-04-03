// ================================================================
// PlayerHealth.cs
// Attach to: Player GameObject
// ================================================================
using UnityEngine;
using System;

public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance { get; private set; }

    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    private float _damageReduction = 0f;   // 0 = no reduction, 0.2 = 20% reduction


    // UI and other systems subscribe to this
    public event Action<float, float> OnHealthChanged;  // (current, max)
    public event Action OnDied; 

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead { get; private set; } = false;

    // ----------------------------------------------------------------
    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        currentHealth = maxHealth;
    }

    // ----------------------------------------------------------------
public void TakeDamage(float amount)
{
    if (IsDead) return;

    // apply damage reduction from Iron Ribs buff
    float reducedAmount = amount * (1f - _damageReduction);
    currentHealth = Mathf.Clamp(currentHealth - reducedAmount, 0f, maxHealth);
    OnHealthChanged?.Invoke(currentHealth, maxHealth);

    if (currentHealth <= 0f)
        Die();
}
public void IncreaseMaxHealth(float amount)
{
    maxHealth     += amount;
    currentHealth += amount;    // heal the difference too
    OnHealthChanged?.Invoke(currentHealth, maxHealth);
}

public void SetDamageReduction(float reduction)
{
    _damageReduction = Mathf.Clamp(reduction, 0f, 1f);
}

    // ----------------------------------------------------------------
    public void Heal(float amount)
    {
        if (IsDead) return;

        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // ----------------------------------------------------------------
    private void Die()
    {
        IsDead = true;
        OnDied?.Invoke();
        Debug.Log("Player died.");
        // disable controller, show game over screen, etc.
    }
}