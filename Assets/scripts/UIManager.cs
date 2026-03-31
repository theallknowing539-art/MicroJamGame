using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Health Bar")]
    [SerializeField] private Slider healthBar;

    [Header("Instability Bar")]
    [SerializeField] private Slider instabilityBar;
    [SerializeField] private Image instabilityFill;

    [Header("Ammo")]
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private GameObject reloadingText;

    [Header("Bottle Count")]
    [SerializeField] private TextMeshProUGUI bottleCountText;
    [SerializeField] private ItemData rumBottleData;

    [Header("Wave Info")]
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private TextMeshProUGUI breakCountdownText;

    [Header("Gun Reference")]
    [SerializeField] private Gun _gun;

    // ----------------------------------------------------------------
    private void Start()
    {
        // Subscribe to PlayerHealth
        if (PlayerHealth.Instance != null)
            PlayerHealth.Instance.OnHealthChanged += HandleHealthChanged;

        // Subscribe to DrunkManager
        if (DrunkManager.Instance != null)
            DrunkManager.Instance.OnInstabilityChanged += HandleInstabilityChanged;

        // Subscribe to WaveManager
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveStarted += HandleWaveStarted;
            WaveManager.Instance.OnBreakTick += HandleBreakTick;
        }

        // Subscribe to Gun
        if (_gun != null)
            _gun.OnAmmoChanged += HandleAmmoChanged;

        // Subscribe to Inventory
        if (Inventory.Instance != null)
            Inventory.Instance.OnInventoryChanged += HandleInventoryChanged;

        // Hide break countdown at start
        if (breakCountdownText != null)
            breakCountdownText.gameObject.SetActive(false);
    }

    // ----------------------------------------------------------------
    private void OnDestroy()
    {
        // Unsubscribe from everything to avoid errors
        if (PlayerHealth.Instance != null)
            PlayerHealth.Instance.OnHealthChanged -= HandleHealthChanged;

        if (DrunkManager.Instance != null)
            DrunkManager.Instance.OnInstabilityChanged -= HandleInstabilityChanged;

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveStarted -= HandleWaveStarted;
            WaveManager.Instance.OnBreakTick -= HandleBreakTick;
        }

        if (_gun != null)
            _gun.OnAmmoChanged -= HandleAmmoChanged;

        if (Inventory.Instance != null)
            Inventory.Instance.OnInventoryChanged -= HandleInventoryChanged;
    }

    // ----------------------------------------------------------------
    // Health Bar
    private void HandleHealthChanged(float current, float max)
    {
        if (healthBar != null)
            healthBar.value = current / max;
    }

    // ----------------------------------------------------------------
    // Instability Bar — changes color green → yellow → red
    private void HandleInstabilityChanged(float current, float max)
    {
        if (instabilityBar != null)
            instabilityBar.value = current / max;

        // Change color based on instability level
        if (instabilityFill != null)
        {
            float ratio = current / max;

            if (ratio < 0.4f)
                instabilityFill.color = Color.green;
            else if (ratio < 0.7f)
                instabilityFill.color = Color.yellow;
            else
                instabilityFill.color = Color.red;
        }
    }

    // ----------------------------------------------------------------
    // Ammo Counter
    private void HandleAmmoChanged()
    {
        if (ammoText != null)
            ammoText.text = _gun.CurrentAmmo + " / " + _gun.ReserveAmmo;

        if (reloadingText != null)
            reloadingText.SetActive(_gun.IsReloading);
    }

    // ----------------------------------------------------------------
    // Bottle Count
    private void HandleInventoryChanged()
    {
        if (bottleCountText != null && rumBottleData != null)
            bottleCountText.text = Inventory.Instance.GetCount(rumBottleData).ToString();
    }

    // ----------------------------------------------------------------
    // Wave Started
    private void HandleWaveStarted(int waveNumber, int totalEnemies)
    {
        if (waveText != null)
            waveText.text = "Wave " + waveNumber;

        // Hide countdown when wave starts
        if (breakCountdownText != null)
            breakCountdownText.gameObject.SetActive(false);
    }

    // ----------------------------------------------------------------
    // Break Countdown between waves
    private void HandleBreakTick(float secondsRemaining)
    {
        if (breakCountdownText != null)
        {
            breakCountdownText.gameObject.SetActive(true);
            breakCountdownText.text = "Next wave in: " + Mathf.CeilToInt(secondsRemaining) + "s";
        }
    }
}