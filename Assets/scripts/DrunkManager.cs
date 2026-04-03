using UnityEngine;
using System;
using System.Collections;
using UnityEngine.Audio; // MUST HAVE THIS FOR MIXER

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

    [Header("Audio Warp Settings")]
    [SerializeField] private AudioMixer mainMixer;
    [SerializeField] private string pitchParam = "MyDrunkPitch";
    // If you add a Reverb Send to your mixer, expose it and name it here:
    [SerializeField] private string reverbParam = "DrunkReverb"; 

    [Header("Drunk Headbob — Sober")]
    [SerializeField] private float soberVerticalAmplitude = 0.05f;
    [SerializeField] private float soberVerticalFrequency = 8f;
    [SerializeField] private float soberTiltAmplitude = 1.5f;

    [Header("Drunk Headbob — Drunk")]
    [SerializeField] private float drunkVerticalAmplitude = 0.15f;
    [SerializeField] private float drunkVerticalFrequency = 4f;
    [SerializeField] private float drunkTiltAmplitude = 8f;

    // events
    public event Action<float, float> OnInstabilityChanged;
    public event Action OnHangoverStarted;
    public event Action OnHangoverEnded;

    public float CurrentInstability => currentInstability;
    public float MaxInstability => maxInstability;
    public bool IsHangover { get; private set; } = false;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        if (currentInstability > 0f && !IsHangover)
        {
            currentInstability = Mathf.Clamp(
                currentInstability - instabilityDecayRate * Time.deltaTime,
                0f, maxInstability
            );
            OnInstabilityChanged?.Invoke(currentInstability, maxInstability);

            UpdateDrunkEffects();
        }
    }

    public void RaiseInstability(float amount)
    {
        if (IsHangover) return;
        currentInstability = Mathf.Clamp(currentInstability + amount, 0f, maxInstability);
        OnInstabilityChanged?.Invoke(currentInstability, maxInstability);
        UpdateDrunkEffects();
        CheckHangover();
    }

    public void LowerInstability(float amount)
    {
        currentInstability = Mathf.Clamp(currentInstability - amount, 0f, maxInstability);
        OnInstabilityChanged?.Invoke(currentInstability, maxInstability);
        UpdateDrunkEffects();
    }

    // Combined function to handle Visual (Bob) and Audio (Pitch)
    private void UpdateDrunkEffects()
    {
        float t = currentInstability / maxInstability;
        UpdateDrunkBob(t);
        UpdateAudioWarp(t);
    }

    private void UpdateDrunkBob(float t)
    {
        if (FPSCharacterController.Instance == null) return;

        FPSCharacterController.HeadbobProfile drunkProfile = new FPSCharacterController.HeadbobProfile
        {
            profileName       = "Drunk",
            verticalAmplitude = Mathf.Lerp(soberVerticalAmplitude, drunkVerticalAmplitude, t),
            verticalFrequency = Mathf.Lerp(soberVerticalFrequency, drunkVerticalFrequency, t),
            tiltAmplitude     = Mathf.Lerp(soberTiltAmplitude,     drunkTiltAmplitude,     t),
            tiltFrequency     = Mathf.Lerp(soberVerticalFrequency, drunkVerticalFrequency, t),
            bobSmoothSpeed    = Mathf.Lerp(10f, 4f, t),
            returnSpeed       = Mathf.Lerp(6f,  2f, t)
        };

        FPSCharacterController.Instance.SetHeadbobProfile(drunkProfile);
    }

    private void UpdateAudioWarp(float t)
    {
        if (mainMixer == null) return;

        // 1. Pitch Bending: 1.0 (Normal) to 0.85 (Slow/Deep)
        float targetPitch = Mathf.Lerp(1.0f, 0.85f, t);
        mainMixer.SetFloat(pitchParam, targetPitch);

        // 2. Reverb: -80dB (Silent) to 0dB (Full Echo)
        // Note: You must add a Reverb effect to your Mixer for this!
        float reverbLevel = Mathf.Lerp(-80f, 0f, t);
        mainMixer.SetFloat(reverbParam, reverbLevel);
    }

    private void CheckHangover()
    {
        if (!IsHangover && currentInstability >= hangoverThreshold)
            StartCoroutine(HangoverRoutine());
    }

    private IEnumerator HangoverRoutine()
    {
        IsHangover = true;
        OnHangoverStarted?.Invoke();
        
        // During hangover, max out the warp!
        UpdateAudioWarp(1f);

        yield return new WaitForSeconds(hangoverDuration);

        currentInstability = hangoverThreshold * 0.4f;
        OnInstabilityChanged?.Invoke(currentInstability, maxInstability);

        UpdateDrunkEffects();

        IsHangover = false;
        OnHangoverEnded?.Invoke();
    }
}