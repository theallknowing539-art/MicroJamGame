using UnityEngine;
using System;

public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance { get; private set; }
    private PlayerBuffs buffStats; 

    public float CurrentHealth => buffStats != null ? buffStats.currentHealth : 0f;
    public float MaxHealth => buffStats != null ? buffStats.maxHealth : 100f;
    public bool IsDead { get; private set; } = false;

    public event Action<float, float> OnHealthChanged;
    public event Action<float, float> OnShieldChanged;
    public event Action OnDied;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        buffStats = GetComponent<PlayerBuffs>();
        if (buffStats != null) RefreshUI();
    }

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

        Debug.Log($"[IRON RIBS TEST] Raw Damage: {incomingDamage} | Reduction: {buffStats.damageReduction * 100}% | Final Damage: {incomingDamage * (1f - buffStats.damageReduction)}");


        // --- STEP 1: IRON RIBS (Damage Reduction) ---
        // If reduction is 0.2 (20%), we multiply incoming damage by 0.8 (80%)
        float finalDamage = incomingDamage * (1f - buffStats.damageReduction);

        // --- STEP 2: BRINE BARRIER (Shield) ---
        if (buffStats.shieldCapacity > 0)
        {
            buffStats.shieldCapacity -= finalDamage;
            
            if (buffStats.shieldCapacity < 0) 
            {
                // Shield depleted, apply remaining damage to health
                buffStats.currentHealth += buffStats.shieldCapacity; 
                buffStats.shieldCapacity = 0;
            }
        }
        else
        {
            // --- STEP 3: HEALTH ---
            buffStats.currentHealth -= finalDamage;
        }

        buffStats.currentHealth = Mathf.Clamp(buffStats.currentHealth, 0f, buffStats.maxHealth);
        RefreshUI();

        if (buffStats.currentHealth <= 0f) Die();
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
    }
}