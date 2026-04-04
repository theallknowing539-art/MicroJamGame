using UnityEngine;
using System;

public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance { get; private set; }

    // --- References ---
    private PlayerBuffs buffStats; 

    // --- Shortcuts for other scripts ---
    public float CurrentHealth => buffStats != null ? buffStats.currentHealth : 0f;
    public float MaxHealth => buffStats != null ? buffStats.maxHealth : 100f;
    public bool IsDead { get; private set; } = false;

    // --- Events (UI and other systems subscribe here) ---
    public event Action<float, float> OnHealthChanged;  // (current, max)
    public event Action<float, float> OnShieldChanged;  // (current, max)
    public event Action OnDied;

    private void Awake()
    {
        if (Instance != null && Instance != this) 
        { 
            Destroy(gameObject); 
            return; 
        }
        Instance = this;
    }

    private void Start()
    {
        buffStats = GetComponent<PlayerBuffs>();

        if (buffStats == null)
        {
            Debug.LogError("FATAL ERROR: PlayerBuffs script is missing from the Player object!");
        }
        else
        {
            // Initialize the UI with starting values
            RefreshUI();
        }
    }

    /// <summary>
    /// Updates the UI by triggering the Health and Shield events.
    /// </summary>
    public void RefreshUI()
    {
        if (buffStats != null)
        {
            OnHealthChanged?.Invoke(buffStats.currentHealth, buffStats.maxHealth);
            OnShieldChanged?.Invoke(buffStats.shieldCapacity, buffStats.maxShieldCapacity);
        }
    }

    public void TakeDamage(float incomingDamage)
    {
        if (IsDead || buffStats == null) return;

        // 1. Reduce damage by Iron Ribs % first (Damage Reduction)
        // If damageReduction is 0.2f, player takes 80% damage.
        float finalDamage = incomingDamage * (1f - buffStats.damageReduction);

        // 2. CHECK THE SHIELD (Brine Barrier)
        if (buffStats.shieldCapacity > 0)
        {
            buffStats.shieldCapacity -= finalDamage;
            
            if (buffStats.shieldCapacity < 0) 
            {
                // Shield broke! Apply leftover negative damage to health
                buffStats.currentHealth += buffStats.shieldCapacity; 
                buffStats.shieldCapacity = 0;
            }
        }
        else
        {
            // 3. NO SHIELD? Damage health directly
            buffStats.currentHealth -= finalDamage;
        }

        // Clamp values so they don't go out of bounds
        buffStats.currentHealth = Mathf.Clamp(buffStats.currentHealth, 0f, buffStats.maxHealth);
        
        // Update the UI Sliders
        RefreshUI();

        if (buffStats.currentHealth <= 0f) 
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (IsDead || buffStats == null) return;
        
        buffStats.currentHealth = Mathf.Clamp(buffStats.currentHealth + amount, 0f, buffStats.maxHealth);
        RefreshUI();
    }

    private void Die()
    {
        if (IsDead) return;
        IsDead = true;
        OnDied?.Invoke();
        Debug.Log("<color=red>Player has died!</color>");
    }
}