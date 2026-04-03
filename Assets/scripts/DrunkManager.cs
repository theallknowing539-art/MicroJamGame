using UnityEngine;
using System;
using System.Collections;
using UnityEngine.Audio;
using Unity.VisualScripting;

public class DrunkManager : MonoBehaviour
{
    public static DrunkManager Instance { get; private set; }

    // Private Audio Reference
    private AudioSource _audioSource;

    [Header("Hangover Audio")]
    [SerializeField] private AudioClip hangoverSFX; // Drag your chicken cluck / horn here
    [Range(0f, 1f)] [SerializeField] private float hangoverVolume = 1.0f;

    [Header("Instability")]
    [SerializeField] private float maxInstability = 100f;
    [SerializeField] private float currentInstability = 0f;

    [Header("Hangover Settings")]
    [SerializeField] private float hangoverThreshold = 80f;
    [SerializeField] private float hangoverDuration = 3f;
    [SerializeField] private float instabilityDecayRate = 2f;

    [Header("Audio Warp Settings")]
    [SerializeField] private AudioMixer mainMixer;
    [SerializeField] private string pitchParam = "MyDrunkPitch";
    [SerializeField] private string reverbParam = "DrunkReverb"; 

    [Header("Drunk Headbob — Sober")]
    [SerializeField] private float soberVerticalAmplitude = 0.05f;
    [SerializeField] private float soberVerticalFrequency = 8f;
    [SerializeField] private float soberTiltAmplitude = 1.5f;

    [Header("Drunk Headbob — Drunk")]
    [SerializeField] private float drunkVerticalAmplitude = 0.15f;
    [SerializeField] private float drunkVerticalFrequency = 4f;
    [SerializeField] private float drunkTiltAmplitude = 8f;

    // Events
    public static event System.Action<float, float> OnInstabilityChanged;
    public event Action OnHangoverStarted;
    public event Action OnHangoverEnded;

    public float CurrentInstability => currentInstability;
    public float MaxInstability => maxInstability;
    
    [SerializeField] private GameObject player;
    public bool IsHangover { get; private set; } = false;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        // Initialize Audio Source
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void Update()
    {
        // Only decay if we are sober enough to move
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
        float targetPitch = Mathf.Lerp(1.0f, 0.85f, t);
        mainMixer.SetFloat(pitchParam, targetPitch);
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
        //disable fps controller
        player.GetComponent<FPSCharacterController>().enabled = false;
        // Play the Cluck!
        if (_audioSource != null && hangoverSFX != null)
        {
            _audioSource.PlayOneShot(hangoverSFX, hangoverVolume);
        }
        
        UpdateAudioWarp(1f); // Max distortion during hangover

        yield return new WaitForSeconds(hangoverDuration);
        //enable fps controller
        player.GetComponent<FPSCharacterController>().enabled = true;


        // RESET LOGIC: Drop to 40% so the player can trigger it again
        currentInstability = maxInstability * 0.4f;
        OnInstabilityChanged?.Invoke(currentInstability, maxInstability);

        UpdateDrunkEffects();

        IsHangover = false;
        OnHangoverEnded?.Invoke();
    }
}