using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections; // Required for the delay

public class GameUI : MonoBehaviour
{
    [Header("Health Bar")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthText;

    [Header("Instability Bar")]
    [SerializeField] private Slider instabilitySlider;
    [SerializeField] private TextMeshProUGUI instabilityText;

    [Header("Wave Info")]
    [SerializeField] private TextMeshProUGUI waveCounterText;
    [SerializeField] private TextMeshProUGUI enemyCountText;
    [SerializeField] private TextMeshProUGUI waveStatusText;

    [Header("Bottle Inventory")]
    [SerializeField] private TextMeshProUGUI bottleCountText;
    [SerializeField] private ItemData rumBottleData;

    private void Start()
    {
        // Use a Coroutine to wait 1 frame ensures Singletons are fully ready
        StartCoroutine(SetupUI());
    }

    private IEnumerator SetupUI()
    {
        yield return null; // Wait one frame

        if (PlayerHealth.Instance != null) PlayerHealth.Instance.OnHealthChanged += HandleHealthChanged;
        if (DrunkManager.Instance != null) DrunkManager.Instance.OnInstabilityChanged += HandleInstabilityChanged;
        
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveStarted += HandleWaveStarted;
            WaveManager.Instance.OnWaveCleared += HandleWaveCleared;
            WaveManager.Instance.OnEnemyCountChanged += HandleEnemyCountChanged;
            WaveManager.Instance.OnBreakTick += HandleBreakTick;
        }

        if (Inventory.Instance != null) Inventory.Instance.OnInventoryChanged += HandleInventoryChanged;

        RefreshAll();
    }

    private void OnDestroy()
    {
        if (PlayerHealth.Instance != null) PlayerHealth.Instance.OnHealthChanged -= HandleHealthChanged;
        if (DrunkManager.Instance != null) DrunkManager.Instance.OnInstabilityChanged -= HandleInstabilityChanged;
        if (Inventory.Instance != null) Inventory.Instance.OnInventoryChanged -= HandleInventoryChanged;
    }

    private void HandleHealthChanged(float current, float max)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = max;
            healthSlider.value = current;
        }
        if (healthText != null) healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }

    private void HandleInstabilityChanged(float current, float max)
    {
        if (instabilitySlider != null)
        {
            instabilitySlider.maxValue = max;
            instabilitySlider.value = current;
        }
        if (instabilityText != null)
        {
            float pct = (max > 0) ? (current / max) * 100f : 0;
            instabilityText.text = $"{Mathf.RoundToInt(pct)}%";
        }
    }

    private void HandleWaveStarted(int wave, int total)
    {
        if (waveCounterText != null) waveCounterText.text = $"Wave {wave}";
        if (enemyCountText != null) enemyCountText.text = $"Enemies: {total}";
        if (waveStatusText != null) waveStatusText.text = "FIGHT!";
    }

    private void HandleWaveCleared(int wave)
    {
        if (waveStatusText != null) waveStatusText.text = "WAVE CLEARED";
    }

    private void HandleEnemyCountChanged(int remaining)
    {
        if (enemyCountText != null) enemyCountText.text = $"Enemies: {remaining}";
    }

    private void HandleBreakTick(float seconds)
    {
        if (waveStatusText != null) waveStatusText.text = $"Next wave in {Mathf.CeilToInt(seconds)}s";
    }

    private void HandleInventoryChanged()
    {
        if (bottleCountText == null || Inventory.Instance == null) return;
        int count = (rumBottleData != null) ? Inventory.Instance.GetCount(rumBottleData) : 0;
        bottleCountText.text = $"x{count}";
    }

    public void RefreshAll()
    {
        if (PlayerHealth.Instance != null) HandleHealthChanged(PlayerHealth.Instance.CurrentHealth, PlayerHealth.Instance.MaxHealth);
        if (DrunkManager.Instance != null) HandleInstabilityChanged(DrunkManager.Instance.CurrentInstability, DrunkManager.Instance.MaxInstability);
        HandleInventoryChanged();
    }
}