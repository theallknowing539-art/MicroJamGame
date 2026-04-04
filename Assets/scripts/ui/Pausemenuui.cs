using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// PAUSE MENU (Hiccup!) — attach to a Pause Panel GameObject
/// 
/// SETUP:
/// 1. Create a Panel as child of Canvas — this is your "pauseRoot"
/// 2. Attach this script to it
/// 3. This panel starts INACTIVE; the game enables it when player presses ESC
/// 4. Wire all fields in Inspector
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    [Header("=== TITLE ===")]
    public TextMeshProUGUI hiccupTitle;       // "Hiccup!"
    public TextMeshProUGUI subtitleText;      // "— The battle waits (reluctantly) —"

    [Header("=== STATS ===")]
    public TextMeshProUGUI hpValue;           // e.g. "74"
    public TextMeshProUGUI hpLabel;           // "HP"
    public TextMeshProUGUI waveValue;         // "3"
    public TextMeshProUGUI waveLabel;         // "CURRENT"
    public TextMeshProUGUI killsValue;        // "47"
    public TextMeshProUGUI killsLabel;        // "KILLS"
    public TextMeshProUGUI rumValue;          // "3"
    public TextMeshProUGUI rumLabel;          // "RUM LEFT"

    [Header("=== INSTABILITY BAR ===")]
    public Slider instabilitySlider;          // Range 0-1
    public TextMeshProUGUI instabilityLabel;  // "INSTABILITY"
    public TextMeshProUGUI instabilityState;  // e.g. "FEELIN' IT..."
    // Instability states based on value:
    //   0.0–0.25  → "STONE COLD SOBER"
    //   0.25–0.5  → "FEELIN' IT..."
    //   0.5–0.75  → "PROPERLY HAMMERED"
    //   0.75–1.0  → "SEEING DOUBLE"

    [Header("=== SECOND BAR (optional decoration) ===")]
    public Slider secondaryBar;              // The green bar seen in screenshot (rum level?)

    [Header("=== BUTTONS ===")]
    public Button backToBattleButton;
    public Button settingsButton;
    public Button abandonShipButton;

    [Header("=== SCENE NAMES ===")]
    public string mainMenuSceneName = "MainMenu";

    [Header("=== FOOTER ===")]
    public TextMeshProUGUI footerText;       // "ESC TO RESUME · THE SKELETONS ARE JUDGING YOU"

    // ─── Reference to live game state — set these from your GameManager ──────

    [HideInInspector] public int   currentHP       = 100;
    [HideInInspector] public int   currentWave     = 1;
    [HideInInspector] public int   currentKills    = 0;
    [HideInInspector] public int   currentRumLeft  = 5;
    [HideInInspector] public float instability     = 0f; // 0–1

    // ─── Awake ────────────────────────────────────────────────────────────────

    void Awake()
    {
        gameObject.SetActive(false); // Starts hidden
    }

    // ─── Called by GameManager to open the pause menu ─────────────────────────

    public void Show(int hp, int wave, int kills, int rumLeft, float instabilityVal)
    {
        currentHP      = hp;
        currentWave    = wave;
        currentKills   = kills;
        currentRumLeft = rumLeft;
        instability    = instabilityVal;

        gameObject.SetActive(true);
        Time.timeScale = 0f;  // Freeze the game

        RefreshStats();
        BindButtons();

        if (footerText) footerText.text = "ESC TO RESUME  ·  THE SKELETONS ARE JUDGING YOU";
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        Time.timeScale = 1f;
    }

    // ─── Refresh Stats Display ────────────────────────────────────────────────

    void RefreshStats()
    {
        if (hiccupTitle)  hiccupTitle.text  = "Hiccup!";
        if (subtitleText) subtitleText.text = "— The battle waits (reluctantly) —";

        if (hpValue)    hpValue.text    = currentHP.ToString();
        if (hpLabel)    hpLabel.text    = "HP";
        if (waveValue)  waveValue.text  = currentWave.ToString();
        if (waveLabel)  waveLabel.text  = "CURRENT";
        if (killsValue) killsValue.text = currentKills.ToString();
        if (killsLabel) killsLabel.text = "KILLS";
        if (rumValue)   rumValue.text   = currentRumLeft.ToString();
        if (rumLabel)   rumLabel.text   = "RUM LEFT";

        if (instabilitySlider) instabilitySlider.value = instability;
        if (instabilityLabel)  instabilityLabel.text   = "INSTABILITY";
        if (instabilityState)  instabilityState.text   = GetInstabilityText(instability);
    }

    string GetInstabilityText(float val)
    {
        if (val < 0.25f) return "STONE COLD SOBER";
        if (val < 0.50f) return "FEELIN' IT...";
        if (val < 0.75f) return "PROPERLY HAMMERED";
        return "SEEING DOUBLE";
    }

    // ─── Buttons ──────────────────────────────────────────────────────────────

    void BindButtons()
    {
        if (backToBattleButton) { backToBattleButton.onClick.RemoveAllListeners(); backToBattleButton.onClick.AddListener(Hide); }
        if (settingsButton)     { settingsButton.onClick.RemoveAllListeners();     settingsButton.onClick.AddListener(OpenSettings); }
        if (abandonShipButton)  { abandonShipButton.onClick.RemoveAllListeners();  abandonShipButton.onClick.AddListener(AbandonShip); }
    }

    void OpenSettings()
    {
        Debug.Log("Settings opened — wire your SettingsPanel here");
    }

    void AbandonShip()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // ─── ESC toggle (call from Update in your GameManager or InputHandler) ────

    void Update()
    {
        // Only handle ESC if this panel is already open (close it)
        if (Input.GetKeyDown(KeyCode.Escape) && gameObject.activeSelf)
            Hide();
    }
}