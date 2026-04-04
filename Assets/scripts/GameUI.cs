using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GameUI : MonoBehaviour
{
    [Header("Drunk Subtitles")]
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private float subtitleDisplayTime = 2f;

    [Header("Bars")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider shieldSlider; // Assigned in Inspector
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
        // Wait one frame to ensure Singletons and Player scripts are initialized
        yield return null;

        if (PlayerHealth.Instance != null)
        {
            PlayerHealth.Instance.OnHealthChanged += HandleHealthChanged;
            PlayerHealth.Instance.OnShieldChanged += HandleShieldChanged;
        }

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

        // Final step: Force the UI to show the correct values immediately
        RefreshAll();
    }

    private void OnDestroy()
    {
        if (PlayerHealth.Instance != null)
        {
            PlayerHealth.Instance.OnHealthChanged -= HandleHealthChanged;
            PlayerHealth.Instance.OnShieldChanged -= HandleShieldChanged;
        }

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

    // --- HEALTH & SHIELD LOGIC ---

    private void HandleHealthChanged(float current, float max)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = max; 
            healthSlider.value    = current;
            Debug.Log($"[UI] Health: {current}/{max}");
        }
    }

    private void HandleShieldChanged(float current, float max)
    {
        if (shieldSlider != null)
        {
            // Show the bar if we have a shield, hide it if it's 0
            shieldSlider.gameObject.SetActive(current > 0);
            shieldSlider.maxValue = max;
            shieldSlider.value    = current;
            Debug.Log($"[UI] Shield: {current}/{max}");
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

    // --- REFRESH LOGIC ---

    public void RefreshAll()
    {
        if (PlayerHealth.Instance != null)
        {
            // Force health slider to update
            HandleHealthChanged(PlayerHealth.Instance.CurrentHealth, PlayerHealth.Instance.MaxHealth);
            
            // Look for shield data in PlayerBuffs to update shield slider
            var buffs = PlayerHealth.Instance.GetComponent<PlayerBuffs>();
            if (buffs != null)
            {
                HandleShieldChanged(buffs.shieldCapacity, buffs.maxShieldCapacity);
            }
        }

        if (DrunkManager.Instance != null)
            HandleInstabilityChanged(DrunkManager.Instance.CurrentInstability, DrunkManager.Instance.MaxInstability);

        HandleInventoryChanged();
        HandleAmmoChanged();
    }

    // --- AMMO & INVENTORY ---

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

    // --- WAVE LOGIC ---

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

    // --- DRUNK DIALOGUE LOGIC ---

    private void HandleDrunkDialogue(float current, float max)
    {
        float percentage = current / max; 
        string slurredText = "";

        if (DrunkManager.Instance != null && DrunkManager.Instance.IsHangover) 
            slurredText = "BAWK?! *hic*"; 
        else if (percentage >= 1.0f) 
            slurredText = "...";
        else if (percentage >= 0.8f) 
            slurredText = "I'M FINE TRUST ME";
        else if (percentage >= 0.6f) 
            slurredText = "wha'z goin on...";
        else if (percentage >= 0.3f) 
            slurredText = "*hic*";

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
        yield return new WaitForSeconds(subtitleDisplayTime);
        subtitleText.text = "";
    }
}