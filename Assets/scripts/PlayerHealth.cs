using UnityEngine;
using System;
using System.Collections;

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

    // --- STEP 1: IRON RIBS (Damage Reduction) ---
    float finalDamage = incomingDamage * (1f - buffStats.damageReduction);

    // --- STEP 2: EFFECTS (The "Juice") ---
    // We trigger these IMMEDIATELY so the player feels the impact
    if (HitStopManager.Instance != null) 
        HitStopManager.Instance.Stop(0.12f); // Freeze time briefly

    if (CameraShake.Instance != null)
        CameraShake.Instance.HitShake(0.6f, 0.25f); // Stronger shake than enemies

    // --- STEP 3: BRINE BARRIER (Shield Logic) ---
    if (buffStats.shieldCapacity > 0)
    {
        float excessDamage = finalDamage - buffStats.shieldCapacity;
        buffStats.shieldCapacity -= finalDamage;
        
        if (buffStats.shieldCapacity <= 0) 
        {
            buffStats.shieldCapacity = 0;
            // Apply leftover damage to health if shield broke
            if (excessDamage > 0) buffStats.currentHealth -= excessDamage;
        }
    }
    else
    {
        // --- STEP 4: HEALTH ---
        buffStats.currentHealth -= finalDamage;
    }

    // --- STEP 5: CLEANUP & UI ---
    buffStats.currentHealth = Mathf.Clamp(buffStats.currentHealth, 0f, buffStats.maxHealth);
    RefreshUI();

    // Trigger Red Flash for player (UI overlay or screen effect)
    StartCoroutine(FlashRed()); 

    if (buffStats.currentHealth <= 0f) Die();
}

private IEnumerator FlashRed()
{
    // If you have a UI Image for damage, enable it here
    // damageImage.enabled = true;
    
    Debug.Log("Player Screen Flash!"); 
    
    // Use Realtime so it works during HitStop
    yield return new WaitForSecondsRealtime(0.1f);
    
    // damageImage.enabled = false;
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