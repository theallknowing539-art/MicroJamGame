using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using System.Collections;

public class DrunkPostEffect : MonoBehaviour
{
    // ================================================================
    // References
    // ================================================================
    [Header("References")]
    [SerializeField] private PostProcessVolume volume;   // Changed from public to SerializeField (better practice)

    // ================================================================
    // Existing Effects
    // ================================================================
    [Header("Lens Distortion")]
    [Tooltip("Distortion at 0% instability (should be 0 for sober look).")]
    [SerializeField] private float soberDistortion = 0f;
    [Tooltip("Distortion at 100% instability. Negative = barrel distortion (recommended for drunk).")]
    [SerializeField] private float drunkDistortion = -45f;

    [Header("Chromatic Aberration")]
    [Tooltip("Aberration intensity at 0% instability.")]
    [SerializeField] private float soberAberration = 0f;
    [Tooltip("Aberration intensity at 100% instability. (0–1)")]
    [SerializeField] private float drunkAberration = 0.85f;

    [Header("Vignette")]
    [SerializeField] private bool vignetteEnabled = true;
    [Tooltip("Vignette intensity at 0% instability.")]
    [SerializeField] private float soberVignette = 0.2f;
    [Tooltip("Vignette intensity at 100% instability.")]
    [SerializeField] private float drunkVignette = 0.6f;
    [Tooltip("Vignette color at max drunkenness (sickly green works great)")]
    [SerializeField] private Color drunkVignetteColor = new Color(0.12f, 0.35f, 0.12f);

    // ================================================================
    // New Effects (Color Grading + Film Grain)
    // ================================================================
    [Header("Color Grading")]
    [Tooltip("Saturation at sober state (0 is normal, negative = slightly desaturated)")]
    [SerializeField] private float soberSaturation = 5f;
    [Tooltip("Saturation at full drunk (negative values remove color - recommended -30 to -60)")]
    [SerializeField] private float drunkSaturation = -45f;

    [Tooltip("Overall tint when fully drunk (slightly yellowish / warm for drunk feel)")]
    [SerializeField] private Color drunkTint = new Color(1.08f, 1.02f, 0.88f);   // mild warm tint

    [Header("Film Grain")]
    [SerializeField] private bool grainEnabled = true;
    [Tooltip("Grain at sober state")]
    [SerializeField] private float soberGrain = 0f;
    [Tooltip("Grain at full drunk (0.6 ~ 1.0 looks good)")]
    [SerializeField] private float drunkGrain = 0.75f;

    // ================================================================
    // Smoothing & Hangover
    // ================================================================
    [Header("Smoothing")]
    [Tooltip("How fast effects increase when getting drunk")]
    [SerializeField] private float blendInSpeed = 3.5f;
    [Tooltip("How fast effects fade when sobering up")]
    [SerializeField] private float blendOutSpeed = 1.8f;

    [Header("Hangover Pulse")]
    [Tooltip("Extra distortion intensity during hangover")]
    [SerializeField] private float hangoverPulseAmount = 25f;
    [SerializeField] private float hangoverPulseSpeed = 5.5f;

    // ================================================================
    // Private variables
    // ================================================================
    private LensDistortion _lensDistortion;
    private ChromaticAberration _chromaticAberration;
    private Vignette _vignette;
    private ColorGrading _colorGrading;
    private Grain _filmGrain;

    private float _currentT = 0f;
    private float _targetT = 0f;
    private bool _isHangover = false;
    private float _hangoverTimer = 0f;
    private Color _soberVignetteColor = Color.black;

    // ================================================================
    private void Awake()
    {
        if (volume == null)
        {
            Debug.LogError("[DrunkPostEffect] No PostProcessVolume assigned! Drag your Volume into the field.");
            enabled = false;
            return;
        }

        PostProcessProfile profile = volume.profile;

        // Get all effects
        profile.TryGetSettings(out _lensDistortion);
        profile.TryGetSettings(out _chromaticAberration);
        if (vignetteEnabled) profile.TryGetSettings(out _vignette);
        profile.TryGetSettings(out _colorGrading);
        if (grainEnabled) profile.TryGetSettings(out _filmGrain);

        // Store original vignette color
        if (_vignette != null)
            _soberVignetteColor = _vignette.color.value;

        // Optional warnings
        if (_colorGrading == null) Debug.LogWarning("[DrunkPostEffect] Color Grading not found in profile.");
        if (grainEnabled && _filmGrain == null) Debug.LogWarning("[DrunkPostEffect] Film Grain not found in profile.");
    }

    // ================================================================
    private void OnEnable()
    {
        StartCoroutine(SubscribeWhenReady());
    }

    private void OnDisable()
    {
        if (DrunkManager.Instance != null)
        {
            DrunkManager.Instance.OnInstabilityChanged -= HandleInstabilityChanged;
            DrunkManager.Instance.OnHangoverStarted -= HandleHangoverStarted;
            DrunkManager.Instance.OnHangoverEnded -= HandleHangoverEnded;
        }
    }

    // ================================================================
    private IEnumerator SubscribeWhenReady()
    {
        yield return new WaitUntil(() => DrunkManager.Instance != null);

        DrunkManager.Instance.OnInstabilityChanged += HandleInstabilityChanged;
        DrunkManager.Instance.OnHangoverStarted += HandleHangoverStarted;
        DrunkManager.Instance.OnHangoverEnded += HandleHangoverEnded;

        _targetT = DrunkManager.Instance.CurrentInstability / DrunkManager.Instance.MaxInstability;
        _currentT = _targetT;
        ApplyEffects(_currentT);
    }

    // ================================================================
    private void Update()
    {
        float speed = (_targetT > _currentT) ? blendInSpeed : blendOutSpeed;
        _currentT = Mathf.MoveTowards(_currentT, _targetT, speed * Time.deltaTime);

        float displayT = _currentT;

        if (_isHangover)
        {
            _hangoverTimer += Time.deltaTime * hangoverPulseSpeed;
            float pulse = Mathf.Sin(_hangoverTimer) * 0.5f + 0.5f; // 0 to 1
            displayT = Mathf.Clamp01(_currentT + pulse * (hangoverPulseAmount / 100f));
        }

        ApplyEffects(displayT);
    }

    // ================================================================
    private void ApplyEffects(float t)
    {
        // Lens Distortion
        if (_lensDistortion != null)
            _lensDistortion.intensity.value = Mathf.Lerp(soberDistortion, drunkDistortion, t);

        // Chromatic Aberration
        if (_chromaticAberration != null)
            _chromaticAberration.intensity.value = Mathf.Lerp(soberAberration, drunkAberration, t);

        // Vignette
        if (_vignette != null && vignetteEnabled)
        {
            _vignette.intensity.value = Mathf.Lerp(soberVignette, drunkVignette, t);
            _vignette.color.value = Color.Lerp(_soberVignetteColor, drunkVignetteColor, t);
        }

        // Color Grading
        if (_colorGrading != null)
        {
            _colorGrading.saturation.value = Mathf.Lerp(soberSaturation, drunkSaturation, t);
            _colorGrading.colorFilter.value = Color.Lerp(Color.white, drunkTint, t);   // Better than .tint for overall color
        }

        // Film Grain
        if (_filmGrain != null && grainEnabled)
        {
            _filmGrain.intensity.value = Mathf.Lerp(soberGrain, drunkGrain, t);
        }
    }

    // ================================================================
    private void HandleInstabilityChanged(float current, float max)
    {
        _targetT = (max > 0f) ? (current / max) : 0f;
    }

    private void HandleHangoverStarted()
    {
        _isHangover = true;
        _hangoverTimer = 0f;
    }

    private void HandleHangoverEnded()
    {
        _isHangover = false;
    }
}