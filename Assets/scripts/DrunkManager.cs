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
    [SerializeField] private float hangoverThreshold = 80f;
    [SerializeField] private float hangoverDuration = 3f;
    [SerializeField] private float instabilityDecayRate = 2f;

    [Header("Drunk Headbob — Sober")]
    [SerializeField] private float soberVerticalAmplitude = 0.05f;
    [SerializeField] private float soberVerticalFrequency = 8f;
    [SerializeField] private float soberTiltAmplitude = 1.5f;

    [Header("Drunk Headbob — Drunk")]
    [SerializeField] private float drunkVerticalAmplitude = 0.15f;
    [SerializeField] private float drunkVerticalFrequency = 4f;     // slower = wobblier
    [SerializeField] private float drunkTiltAmplitude = 8f;         // more tilt = more drunk

    // events
    public event Action<float, float> OnInstabilityChanged;
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
        if (currentInstability > 0f && !IsHangover)
        {
            currentInstability = Mathf.Clamp(
                currentInstability - instabilityDecayRate * Time.deltaTime,
                0f, maxInstability
            );
            OnInstabilityChanged?.Invoke(currentInstability, maxInstability);

            // update headbob every frame as instability decays
            UpdateDrunkBob();
        }
    }

    // ----------------------------------------------------------------
    public void RaiseInstability(float amount)
    {
        if (IsHangover) return;

        currentInstability = Mathf.Clamp(currentInstability + amount, 0f, maxInstability);
        OnInstabilityChanged?.Invoke(currentInstability, maxInstability);

        UpdateDrunkBob();
        CheckHangover();
    }

    // ----------------------------------------------------------------
    public void LowerInstability(float amount)
    {
        currentInstability = Mathf.Clamp(currentInstability - amount, 0f, maxInstability);
        OnInstabilityChanged?.Invoke(currentInstability, maxInstability);

        UpdateDrunkBob();
    }

    // ----------------------------------------------------------------
    private void UpdateDrunkBob()
    {
        if (FPSCharacterController.Instance == null) return;

        // t goes from 0 (sober) to 1 (fully drunk)
        float t = currentInstability / maxInstability;

        // interpolate all bob values between sober and drunk
        FPSCharacterController.HeadbobProfile drunkProfile = new FPSCharacterController.HeadbobProfile
        {
            profileName       = "Drunk",
            verticalAmplitude = Mathf.Lerp(soberVerticalAmplitude, drunkVerticalAmplitude, t),
            verticalFrequency = Mathf.Lerp(soberVerticalFrequency, drunkVerticalFrequency, t),
            tiltAmplitude     = Mathf.Lerp(soberTiltAmplitude,     drunkTiltAmplitude,     t),
            tiltFrequency     = Mathf.Lerp(soberVerticalFrequency, drunkVerticalFrequency, t),
            bobSmoothSpeed    = Mathf.Lerp(10f, 4f, t),    // drunk feels more sluggish
            returnSpeed       = Mathf.Lerp(6f,  2f, t)     // slower return when drunk
        };

        FPSCharacterController.Instance.SetHeadbobProfile(drunkProfile);
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

        if (FPSCharacterController.Instance != null)
            FPSCharacterController.Instance.SetMovementLocked(true);

        yield return new WaitForSeconds(hangoverDuration);

        currentInstability = hangoverThreshold * 0.4f;
        OnInstabilityChanged?.Invoke(currentInstability, maxInstability);

        // reset bob back towards sober after hangover
        UpdateDrunkBob();

        IsHangover = false;
        OnHangoverEnded?.Invoke();

        if (FPSCharacterController.Instance != null)
            FPSCharacterController.Instance.SetMovementLocked(false);
    }
}