using UnityEngine;
using System;

public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance { get; private set; }
    public float CurrentHealth => buffStats != null ? buffStats.currentHealth : 0;
public float MaxHealth => buffStats != null ? buffStats.maxHealth : 100;

    private PlayerBuffs buffStats; // Reference to our data script

    // UI and other systems subscribe to this
    public event Action<float, float> OnHealthChanged;  // (current, max)
    public event Action<float, float> OnShieldChanged;  // (current, max) - NEW for UI
    public event Action OnDied; 

    public bool IsDead { get; private set; } = false;

    // Add this inside the PlayerHealth class
public void RefreshUI()
{
    if (buffStats != null)
    {
        // This "pokes" the GameUI to update the sliders
        OnHealthChanged?.Invoke(buffStats.currentHealth, buffStats.maxHealth);
        
        // If you have a shield bar, poke that too
        OnShieldChanged?.Invoke(buffStats.shieldCapacity, buffStats.maxShieldCapacity);
    }
}
    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
{
    buffStats = GetComponent<PlayerBuffs>();

    if (buffStats == null)
    {
        Debug.LogError("FATAL ERROR: PlayerBuffs script is missing from the Player object! Please attach it.");
    }
    else
    {
        Debug.Log("Success: PlayerHealth is connected to PlayerBuffs.");
        // Initialize the Health Bar UI
        OnHealthChanged?.Invoke(buffStats.currentHealth, buffStats.maxHealth);
    }
}

    public void TakeDamage(float incomingDamage)
    {
        if (IsDead || buffStats == null) return;

        // 1. Apply Iron Ribs Reduction first
        float finalDamage = incomingDamage * (1f - buffStats.damageReduction);

        // 2. Handle Shield (Brine Barrier)
        if (buffStats.shieldCapacity > 0)
        {
            buffStats.shieldCapacity -= finalDamage;
            
            if (buffStats.shieldCapacity < 0) 
            {
                // Shield broke! Apply leftover damage to health
                buffStats.currentHealth += buffStats.shieldCapacity; // Adding a negative = subtracting
                buffStats.shieldCapacity = 0;
            }
            // Optional: Tell UI the shield changed
            OnShieldChanged?.Invoke(buffStats.shieldCapacity, 100f); // Replace 100 with max shield if you have it
        }
        else
        {
            // 3. No shield? Damage goes straight to health
            buffStats.currentHealth -= finalDamage;
        }

        // Clamp values and update UI
        buffStats.currentHealth = Mathf.Clamp(buffStats.currentHealth, 0f, buffStats.maxHealth);
        OnHealthChanged?.Invoke(buffStats.currentHealth, buffStats.maxHealth);

        if (buffStats.currentHealth <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        if (IsDead || buffStats == null) return;
        buffStats.currentHealth = Mathf.Clamp(buffStats.currentHealth + amount, 0f, buffStats.maxHealth);
        OnHealthChanged?.Invoke(buffStats.currentHealth, buffStats.maxHealth);
    }

    private void Die()
    {
        IsDead = true;
        OnDied?.Invoke();
        Debug.Log("Player died.");
    }
}