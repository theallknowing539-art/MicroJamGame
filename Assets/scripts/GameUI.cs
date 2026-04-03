using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GameUI : MonoBehaviour
{
    [Header("Drunk Subtitles")]
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private float subtitleDisplayTime = 2f;

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

    private void Start()
    {
        if (ammoIcon != null && ammoSprite != null)
            ammoIcon.sprite = ammoSprite;

        if (bottleIcon != null && rumBottleData != null && rumBottleData.icon != null)
            bottleIcon.sprite = rumBottleData.icon;

        StartCoroutine(SetupUI());
    }

    private IEnumerator SetupUI()
    {
        yield return null;

        if (PlayerHealth.Instance != null)
            PlayerHealth.Instance.OnHealthChanged += HandleHealthChanged;

        // FIXED: Subscribing to static event (No .Instance)
        DrunkManager.OnInstabilityChanged += HandleInstabilityChanged;
        DrunkManager.OnInstabilityChanged += HandleDrunkDialogue;

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

    private void OnDestroy()
    {
        if (PlayerHealth.Instance != null)
            PlayerHealth.Instance.OnHealthChanged -= HandleHealthChanged;

        // FIXED: Unsubscribing from static event (No .Instance)
        DrunkManager.OnInstabilityChanged -= HandleInstabilityChanged;
        DrunkManager.OnInstabilityChanged -= HandleDrunkDialogue;

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

    private void HandleHealthChanged(float current, float max)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = max;
            healthSlider.value    = current;
        }
    }

    private void HandleInstabilityChanged(float current, float max)
    {
        if (instabilitySlider != null)
        {
            instabilitySlider.maxValue = max;
            instabilitySlider.value    = current;
        }
    }

    private void HandleAmmoChanged()
    {
        if (_gun == null) return;

        if (ammoCurrentText != null)
            ammoCurrentText.text = _gun.IsReloading ? "..." : _gun.CurrentAmmo.ToString();

        if (ammoReserveText != null)
            ammoReserveText.text = $"/ {_gun.ReserveAmmo}";
    }

    private void HandleInventoryChanged()
    {
        if (rumBottleData == null || Inventory.Instance == null) return;

        int count = Inventory.Instance.GetCount(rumBottleData);
        if (bottleCountText != null) bottleCountText.text = $"x{count}";
        if (bottleIcon != null) bottleIcon.enabled = count > 0;
        if (bottleCountText != null) bottleCountText.enabled = count > 0;
    }

    private void HandleWaveStarted(int wave, int total)
    {
        if (waveCounterText != null) waveCounterText.text = $"Wave {wave}";
        if (enemyCountText  != null) enemyCountText.text  = $"Enemies: {total}";
        if (waveStatusText  != null) waveStatusText.text  = "FIGHT!";
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
        if (waveStatusText != null)
            waveStatusText.text = $"Next wave in {Mathf.CeilToInt(seconds)}s";
    }

    public void RefreshAll()
    {
        if (PlayerHealth.Instance != null)
            HandleHealthChanged(PlayerHealth.Instance.CurrentHealth, PlayerHealth.Instance.MaxHealth);

        if (DrunkManager.Instance != null)
            HandleInstabilityChanged(DrunkManager.Instance.CurrentInstability, DrunkManager.Instance.MaxInstability);

        HandleInventoryChanged();
        HandleAmmoChanged();
    }

    // --- DRUNK DIALOGUE LOGIC ---

    private void HandleDrunkDialogue(float current, float max)
{
    // Calculate the percentage (0.0 to 1.0)
    float percentage = current / max; 
    string slurredText = "";

    // 1. PRIORITY: Check for Hangover first (The "BAWK" moment)
    // We check Instance.IsHangover to see if the DrunkManager is currently in the "Locked" state
    if (DrunkManager.Instance != null && DrunkManager.Instance.IsHangover) 
    {
        slurredText = "BAWK?! *hic*"; 
    }
    // 2. Threshold: 100% (Passed out/Speechless)
    else if (percentage >= 1.0f) 
    {
        slurredText = "...";
    }
    // 3. Threshold: 80% (Extreme Denial)
    else if (percentage >= 0.8f) 
    {
        slurredText = "I'M FINE TRUST ME";
    }
    // 4. Threshold: 60% (Confusion)
    else if (percentage >= 0.6f) 
    {
        slurredText = "wha'z goin on...";
    }
    // 5. Threshold: 30% (The first sign)
    else if (percentage >= 0.3f) 
    {
        slurredText = "*hic*";
    }

    // Only trigger the coroutine if we actually have text to show
    if (!string.IsNullOrEmpty(slurredText))
    {
        StopAllCoroutines(); 
        StartCoroutine(ShowSubtitle(slurredText));
    }
}

    private IEnumerator ShowSubtitle(string text)
    {
        if (subtitleText == null) yield break;

        subtitleText.text = text;
        subtitleText.gameObject.SetActive(true);
        subtitleText.transform.localScale = Vector3.one * 1.2f; 
        
        yield return new WaitForSeconds(0.1f);
        subtitleText.transform.localScale = Vector3.one; 
        
        yield return new WaitForSeconds(subtitleDisplayTime);
        subtitleText.text = "";
    }
}