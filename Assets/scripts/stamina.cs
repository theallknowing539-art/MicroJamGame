using UnityEngine;
using UnityEngine.UI;
using System;

public class Stamina : MonoBehaviour
{
    public static Stamina Instance { get; private set; }

    [Header("Stamina")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float currentStamina;

    [Header("Drain Rates")]
    [SerializeField] private float sprintDrainRate = 15f;
    [SerializeField] private float slideCost = 20f;
    [SerializeField] private float groundSlamCost = 25f;
    [SerializeField] private float jumpCost = 10f;

    [Header("Regeneration")]
    [SerializeField] private float regenRate = 10f;
    [SerializeField] private float regenDelay = 1.5f;

    [Header("Exhaustion")]
    [SerializeField] private float exhaustionThreshold = 30f;
    [SerializeField] private float exhaustionRegenRate = 5f;
    [SerializeField] private float minJumpStamina = 10f;

    [Header("UI")]
    [SerializeField] private Slider staminaSlider;
    [SerializeField] private float sliderSmoothSpeed = 5f;

    public event Action<float, float> OnStaminaChanged;
    public event Action OnExhausted;
    public event Action OnRecovered;

    public float CurrentStamina => currentStamina;
    public float MaxStamina     => maxStamina;
    public bool IsExhausted     { get; private set; } = false;
    public bool CanJump         => currentStamina >= minJumpStamina && !IsExhausted;

    private float _regenTimer   = 0f;
    private float _sliderTarget = 1f;

    // ----------------------------------------------------------------
    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        currentStamina = maxStamina;
    }

    // ----------------------------------------------------------------
    private void Start()
    {
        if (staminaSlider != null)
        {
            staminaSlider.minValue    = 0f;
            staminaSlider.maxValue    = 1f;
            staminaSlider.value       = 1f;
            staminaSlider.interactable = false;
        }
    }

    // ----------------------------------------------------------------
    private void Update()
    {
        HandleRegen();
        HandleSprintDrain();
        UpdateSlider();
    }

    // ----------------------------------------------------------------
    private void UpdateSlider()
    {
        if (staminaSlider == null) return;

        _sliderTarget = currentStamina / maxStamina;

        staminaSlider.value = Mathf.Lerp(
            staminaSlider.value,
            _sliderTarget,
            Time.deltaTime * sliderSmoothSpeed
        );
    }

    // ----------------------------------------------------------------
    private void HandleSprintDrain()
    {
        if (FPSCharacterController.Instance == null) return;

        if (FPSCharacterController.Instance.IsSprinting)
            UseStamina(sprintDrainRate * Time.deltaTime);
    }

    // ----------------------------------------------------------------
    private void HandleRegen()
    {
        if (currentStamina >= maxStamina) return;

        if (_regenTimer > 0f)
        {
            _regenTimer -= Time.deltaTime;
            return;
        }

        float rate = IsExhausted ? exhaustionRegenRate : regenRate;
        currentStamina = Mathf.Clamp(currentStamina + rate * Time.deltaTime, 0f, maxStamina);
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);

        if (IsExhausted && currentStamina >= exhaustionThreshold)
        {
            IsExhausted = false;
            OnRecovered?.Invoke();
        }
    }

    // ----------------------------------------------------------------
    public bool UseStamina(float amount)
    {
        if (IsExhausted) return false;
        if (currentStamina <= 0f) return false;

        currentStamina = Mathf.Clamp(currentStamina - amount, 0f, maxStamina);
        _regenTimer    = regenDelay;
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);

        if (currentStamina <= 0f)
        {
            IsExhausted = true;
            OnExhausted?.Invoke();
        }

        return true;
    }

    // ----------------------------------------------------------------
    public bool UseJumpStamina()       => UseStamina(jumpCost);
    public bool UseSlideStamina()      => UseStamina(slideCost);
    public bool UseGroundSlamStamina() => UseStamina(groundSlamCost);
}
