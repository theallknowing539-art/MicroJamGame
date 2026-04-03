using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using System.Collections;

public class DrunkPostEffect : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PostProcessVolume volume;

    [Header("Lens Distortion")]
    [SerializeField] private float soberDistortion = 0f;
    [SerializeField] private float drunkDistortion = -45f;

    [Header("Chromatic Aberration")]
    [SerializeField] private float soberAberration = 0f;
    [SerializeField] private float drunkAberration = 0.85f;

    [Header("Vignette")]
    [SerializeField] private bool vignetteEnabled = true;
    [SerializeField] private float soberVignette = 0.2f;
    [SerializeField] private float drunkVignette = 0.6f;
    [SerializeField] private Color drunkVignetteColor = new Color(0.12f, 0.35f, 0.12f);

    [Header("Color Grading")]
    [SerializeField] private float soberSaturation = 5f;
    [SerializeField] private float drunkSaturation = -45f;
    [SerializeField] private Color drunkTint = new Color(1.08f, 1.02f, 0.88f);

    [Header("Film Grain")]
    [SerializeField] private bool grainEnabled = true;
    [SerializeField] private float soberGrain = 0f;
    [SerializeField] private float drunkGrain = 0.75f;

    [Header("Smoothing")]
    [SerializeField] private float blendInSpeed = 3.5f;
    [SerializeField] private float blendOutSpeed = 1.8f;

    [Header("Hangover Pulse")]
    [SerializeField] private float hangoverPulseAmount = 25f;
    [SerializeField] private float hangoverPulseSpeed = 5.5f;

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

    private void Awake()
    {
        if (volume == null)
        {
            Debug.LogError("[DrunkPostEffect] No PostProcessVolume assigned!");
            enabled = false;
            return;
        }

        PostProcessProfile profile = volume.profile;
        profile.TryGetSettings(out _lensDistortion);
        profile.TryGetSettings(out _chromaticAberration);
        if (vignetteEnabled) profile.TryGetSettings(out _vignette);
        profile.TryGetSettings(out _colorGrading);
        if (grainEnabled) profile.TryGetSettings(out _filmGrain);

        if (_vignette != null)
            _soberVignetteColor = _vignette.color.value;
    }

    private void OnEnable()
    {
        // 1. Static Event (No .Instance) - Points to the correct function name
        DrunkManager.OnInstabilityChanged += HandleInstabilityChanged;

        // 2. Instance Events for Hangover
        StartCoroutine(SubscribeToHangover());
    }

    private void OnDisable()
    {
        // 1. Static Event cleanup
        DrunkManager.OnInstabilityChanged -= HandleInstabilityChanged;

        // 2. Instance Event cleanup
        if (DrunkManager.Instance != null)
        {
            DrunkManager.Instance.OnHangoverStarted -= HandleHangoverStarted;
            DrunkManager.Instance.OnHangoverEnded -= HandleHangoverEnded;
        }
    }

    private IEnumerator SubscribeToHangover()
    {
        yield return new WaitUntil(() => DrunkManager.Instance != null);

        DrunkManager.Instance.OnHangoverStarted += HandleHangoverStarted;
        DrunkManager.Instance.OnHangoverEnded += HandleHangoverEnded;

        // Sync initial state
        _targetT = DrunkManager.Instance.CurrentInstability / DrunkManager.Instance.MaxInstability;
        _currentT = _targetT;
    }

    private void Update()
    {
        float speed = (_targetT > _currentT) ? blendInSpeed : blendOutSpeed;
        _currentT = Mathf.MoveTowards(_currentT, _targetT, speed * Time.deltaTime);

        float displayT = _currentT;

        if (_isHangover)
        {
            _hangoverTimer += Time.deltaTime * hangoverPulseSpeed;
            float pulse = Mathf.Sin(_hangoverTimer) * 0.5f + 0.5f;
            displayT = Mathf.Clamp01(_currentT + pulse * (hangoverPulseAmount / 100f));
        }

        ApplyEffects(displayT);
    }

    private void ApplyEffects(float t)
    {
        if (_lensDistortion != null) _lensDistortion.intensity.value = Mathf.Lerp(soberDistortion, drunkDistortion, t);
        if (_chromaticAberration != null) _chromaticAberration.intensity.value = Mathf.Lerp(soberAberration, drunkAberration, t);
        if (_vignette != null && vignetteEnabled)
        {
            _vignette.intensity.value = Mathf.Lerp(soberVignette, drunkVignette, t);
            _vignette.color.value = Color.Lerp(_soberVignetteColor, drunkVignetteColor, t);
        }
        if (_colorGrading != null)
        {
            _colorGrading.saturation.value = Mathf.Lerp(soberSaturation, drunkSaturation, t);
            _colorGrading.colorFilter.value = Color.Lerp(Color.white, drunkTint, t);
        }
        if (_filmGrain != null && grainEnabled) _filmGrain.intensity.value = Mathf.Lerp(soberGrain, drunkGrain, t);
    }

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