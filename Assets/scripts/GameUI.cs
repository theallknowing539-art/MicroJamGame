using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GameUI : MonoBehaviour
{
    [Header("Health Bar")]
    [SerializeField] private Slider healthSlider;

    [Header("Instability Bar")]
    [SerializeField] private Slider instabilitySlider;

    [Header("Ammo")]
    [SerializeField] private Image ammoIcon;
    [SerializeField] private Sprite ammoSprite;
    [SerializeField] private TextMeshProUGUI ammoCurrentText;
    [SerializeField] private TextMeshProUGUI ammoReserveText;
    [SerializeField] private Gun _gun;

    [Header("Bottle Inventory")]
    [SerializeField] private Image bottleIcon;
    [SerializeField] private TextMeshProUGUI bottleCountText;
    [SerializeField] private ItemData rumBottleData;

    [Header("Wave Info")]
    [SerializeField] private TextMeshProUGUI waveCounterText;
    [SerializeField] private TextMeshProUGUI enemyCountText;
    [SerializeField] private TextMeshProUGUI waveStatusText;

    // ----------------------------------------------------------------
    private void Start()
    {
        // set icons immediately
        if (ammoIcon != null && ammoSprite != null)
            ammoIcon.sprite = ammoSprite;

        if (bottleIcon != null && rumBottleData != null && rumBottleData.icon != null)
            bottleIcon.sprite = rumBottleData.icon;

        StartCoroutine(SetupUI());
    }

    // ----------------------------------------------------------------
    private IEnumerator SetupUI()
    {
        yield return null;

        if (PlayerHealth.Instance != null)
            PlayerHealth.Instance.OnHealthChanged += HandleHealthChanged;

        if (DrunkManager.Instance != null)
            DrunkManager.Instance.OnInstabilityChanged += HandleInstabilityChanged;

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveStarted       += HandleWaveStarted;
            WaveManager.Instance.OnWaveCleared       += HandleWaveCleared;
            WaveManager.Instance.OnEnemyCountChanged += HandleEnemyCountChanged;
            WaveManager.Instance.OnBreakTick         += HandleBreakTick;
        }

        if (Inventory.Instance != null)
            Inventory.Instance.OnInventoryChanged += HandleInventoryChanged;

        if (_gun != null)
            _gun.OnAmmoChanged += HandleAmmoChanged;

        RefreshAll();
    }

    // ----------------------------------------------------------------
    private void OnDestroy()
    {
        if (PlayerHealth.Instance != null)
            PlayerHealth.Instance.OnHealthChanged -= HandleHealthChanged;

        if (DrunkManager.Instance != null)
            DrunkManager.Instance.OnInstabilityChanged -= HandleInstabilityChanged;

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveStarted       -= HandleWaveStarted;
            WaveManager.Instance.OnWaveCleared       -= HandleWaveCleared;
            WaveManager.Instance.OnEnemyCountChanged -= HandleEnemyCountChanged;
            WaveManager.Instance.OnBreakTick         -= HandleBreakTick;
        }

        if (Inventory.Instance != null)
            Inventory.Instance.OnInventoryChanged -= HandleInventoryChanged;

        if (_gun != null)
            _gun.OnAmmoChanged -= HandleAmmoChanged;
    }

    // ----------------------------------------------------------------
    private void HandleHealthChanged(float current, float max)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = max;
            healthSlider.value    = current;
        }
    }

    // ----------------------------------------------------------------
    private void HandleInstabilityChanged(float current, float max)
    {
        if (instabilitySlider != null)
        {
            instabilitySlider.maxValue = max;
            instabilitySlider.value    = current;
        }
    }

    // ----------------------------------------------------------------
    private void HandleAmmoChanged()
    {
        if (_gun == null) return;

        if (ammoCurrentText != null)
            ammoCurrentText.text = _gun.IsReloading
                ? "..."
                : _gun.CurrentAmmo.ToString();

        if (ammoReserveText != null)
            ammoReserveText.text = $"/ {_gun.ReserveAmmo}";
    }

    // ----------------------------------------------------------------
    private void HandleInventoryChanged()
    {
        if (rumBottleData == null || Inventory.Instance == null) return;

        int count = Inventory.Instance.GetCount(rumBottleData);

        if (bottleCountText != null)
            bottleCountText.text = $"x{count}";

        // hide icon and text when player has no bottles
        if (bottleIcon != null)
            bottleIcon.enabled = count > 0;

        if (bottleCountText != null)
            bottleCountText.enabled = count > 0;
    }

    // ----------------------------------------------------------------
    private void HandleWaveStarted(int wave, int total)
    {
        if (waveCounterText != null) waveCounterText.text = $"Wave {wave}";
        if (enemyCountText  != null) enemyCountText.text  = $"Enemies: {total}";
        if (waveStatusText  != null) waveStatusText.text  = "FIGHT!";
    }

    // ----------------------------------------------------------------
    private void HandleWaveCleared(int wave)
    {
        if (waveStatusText != null) waveStatusText.text = "WAVE CLEARED";
    }

    // ----------------------------------------------------------------
    private void HandleEnemyCountChanged(int remaining)
    {
        if (enemyCountText != null) enemyCountText.text = $"Enemies: {remaining}";
    }

    // ----------------------------------------------------------------
    private void HandleBreakTick(float seconds)
    {
        if (waveStatusText != null)
            waveStatusText.text = $"Next wave in {Mathf.CeilToInt(seconds)}s";
    }

    // ----------------------------------------------------------------
    public void RefreshAll()
    {
        if (PlayerHealth.Instance != null)
            HandleHealthChanged(
                PlayerHealth.Instance.CurrentHealth,
                PlayerHealth.Instance.MaxHealth);

        if (DrunkManager.Instance != null)
            HandleInstabilityChanged(
                DrunkManager.Instance.CurrentInstability,
                DrunkManager.Instance.MaxInstability);

        HandleInventoryChanged();
        HandleAmmoChanged();
    }
}

