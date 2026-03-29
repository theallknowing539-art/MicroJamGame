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

        currentHealth = Mathf.Clamp(currentHealth - amount, 0f, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
            Die();
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