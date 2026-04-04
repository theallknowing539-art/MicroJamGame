using UnityEngine;
using System;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance { get; private set; }
    private PlayerBuffs buffStats; 

    [Header("Game Over UI")]
    public GameObject gameOverPanel; // Drag your Game Over UI here in the Inspector

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

        float finalDamage = incomingDamage * (1f - buffStats.damageReduction);

        if (HitStopManager.Instance != null) 
            HitStopManager.Instance.Stop(0.12f); 

        if (CameraShake.Instance != null)
            CameraShake.Instance.HitShake(0.6f, 0.25f); 

        if (buffStats.shieldCapacity > 0)
        {
            float excessDamage = finalDamage - buffStats.shieldCapacity;
            buffStats.shieldCapacity -= finalDamage;
            
            if (buffStats.shieldCapacity <= 0) 
            {
                buffStats.shieldCapacity = 0;
                if (excessDamage > 0) buffStats.currentHealth -= excessDamage;
            }
        }
        else
        {
            buffStats.currentHealth -= finalDamage;
        }

        buffStats.currentHealth = Mathf.Clamp(buffStats.currentHealth, 0f, buffStats.maxHealth);
        RefreshUI();

        StartCoroutine(FlashRed()); 

        if (buffStats.currentHealth <= 0f) Die();
    }

    private IEnumerator FlashRed()
    {
        Debug.Log("Player Screen Flash!"); 
        yield return new WaitForSecondsRealtime(0.1f);
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

    // 1. Trigger the camera fall animation (that we wrote earlier)
    StartCoroutine(PlayerDeathAnimation());

    // 2. Tell the Manager to show the UI and disable movement
    if (GameOverManager.Instance != null)
    {
        GameOverManager.Instance.TriggerGameOver();
    }
}

    private IEnumerator PlayerDeathAnimation()
{
    float duration = 1.0f; 
    float elapsed = 0f;

    // Get the actual Camera Transform
    Transform cam = Camera.main.transform;
    Vector3 startPos = cam.localPosition;
    Quaternion startRot = cam.localRotation;

    // The floor height and looking up angle
    Vector3 targetPos = new Vector3(startPos.x, 0.2f, startPos.z);
    Quaternion targetRot = Quaternion.Euler(-60f, startRot.eulerAngles.y, startRot.eulerAngles.z);

    while (elapsed < duration)
    {
        elapsed += Time.unscaledDeltaTime; 
        float percent = elapsed / duration;

        // Smoothly move
        cam.localPosition = Vector3.Lerp(startPos, targetPos, percent);
        cam.localRotation = Quaternion.Slerp(startRot, targetRot, percent);

        yield return null;
    }

    // --- CRITICAL: LOCK IT HERE ---
    cam.localPosition = targetPos;
    cam.localRotation = targetRot;

    // Disable the Camera Shake script so it doesn't move the camera anymore
    if (CameraShake.Instance != null) CameraShake.Instance.enabled = false;

    yield return new WaitForSecondsRealtime(0.5f);
    
    if (GameOverManager.Instance != null)
        GameOverManager.Instance.TriggerGameOver();
}

    // --- ADDED THE MISSING GAMEOVER FUNCTION ---
    private void GameOver()
    {
        Time.timeScale = 0f; // Pause the game
        Cursor.lockState = CursorLockMode.None; // Free the mouse
        Cursor.visible = true;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }

    
}