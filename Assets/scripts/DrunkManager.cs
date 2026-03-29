// ================================================================
// DrunkManager.cs
// Attach to: empty GameObject in scene
// ================================================================
using UnityEngine;
using System;
using System.Collections;

public class DrunkManager : MonoBehaviour
{
    public static DrunkManager Instance { get; private set; }

    [Header("Instability")]
    [SerializeField] private float maxInstability = 100f;
    [SerializeField] private float currentInstability = 0f;

    [Header("Hangover")]
    [SerializeField] private float hangoverThreshold = 80f;    // instability level that triggers hangover
    [SerializeField] private float hangoverDuration = 3f;
    [SerializeField] private float instabilityDecayRate = 2f;  // instability lost per second naturally

    // events
    public event Action<float, float> OnInstabilityChanged;    // (current, max)
    public event Action OnHangoverStarted;
    public event Action OnHangoverEnded;

    public float CurrentInstability => currentInstability;
    public float MaxInstability => maxInstability;
    public bool IsHangover { get; private set; } = false;

    // ----------------------------------------------------------------
    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ----------------------------------------------------------------
    private void Update()
    {
        // instability naturally decays over time so the player
        // can recover without needing stabilizing items
        if (currentInstability > 0f && !IsHangover)
        {
            currentInstability = Mathf.Clamp(
                currentInstability - instabilityDecayRate * Time.deltaTime,
                0f, maxInstability
            );
            OnInstabilityChanged?.Invoke(currentInstability, maxInstability);
        }
    }

    // ----------------------------------------------------------------
    public void RaiseInstability(float amount)
    {
        if (IsHangover) return;

        currentInstability = Mathf.Clamp(currentInstability + amount, 0f, maxInstability);
        OnInstabilityChanged?.Invoke(currentInstability, maxInstability);

        CheckHangover();
    }

    // ----------------------------------------------------------------
    public void LowerInstability(float amount)
    {
        currentInstability = Mathf.Clamp(currentInstability - amount, 0f, maxInstability);
        OnInstabilityChanged?.Invoke(currentInstability, maxInstability);
    }

    // ----------------------------------------------------------------
    private void CheckHangover()
    {
        if (!IsHangover && currentInstability >= hangoverThreshold)
            StartCoroutine(HangoverRoutine());
    }

    // ----------------------------------------------------------------
    private IEnumerator HangoverRoutine()
    {
        IsHangover = true;
        OnHangoverStarted?.Invoke();
        Debug.Log("Hangover started!");

        // lock player movement
        if (FPSCharacterController.Instance != null)
            FPSCharacterController.Instance.SetMovementLocked(true);

        yield return new WaitForSeconds(hangoverDuration);

        // reset instability after sobering up a bit
        currentInstability = hangoverThreshold * 0.4f;
        OnInstabilityChanged?.Invoke(currentInstability, maxInstability);

        IsHangover = false;
        OnHangoverEnded?.Invoke();
        Debug.Log("Hangover ended.");

        // unlock movement
        if (FPSCharacterController.Instance != null)
            FPSCharacterController.Instance.SetMovementLocked(false);
    }
}