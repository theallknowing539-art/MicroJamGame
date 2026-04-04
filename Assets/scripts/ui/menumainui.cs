using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// SETUP INSTRUCTIONS:
/// 1. Create a new Canvas (UI > Canvas) with Render Mode = Screen Space - Overlay
/// 2. Set Canvas Scaler: Scale With Screen Size, Reference: 1920x1080
/// 3. Attach this script to an empty GameObject called "MainMenuUI"
/// 4. Create a dark background Panel (color: #0D0906)
/// 5. Wire all fields in Inspector (see each region below)
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("=== PANELS ===")]
    public GameObject mainMenuPanel;      // The card/box in the center
    public GameObject settingsPanel;      // Settings overlay (create separately)

    [Header("=== TITLE ===")]
    public TextMeshProUGUI subtitleText;       // "- A Cursed Voyage -"
    public TextMeshProUGUI titleText;          // "Rum & Bones"
    public TextMeshProUGUI editionText;        // "MICRO JAM EDITION"

    [Header("=== FLAVOR TEXT ===")]
    public TextMeshProUGUI flavorText;
    // Set in Inspector text to:
    // "Ye find yerself aboard a cursed ship, knee-deep in rum\nand surrounded by ungrateful skeletons.\nDrink wisely. Shoot straighter. Try not to pass out."

    [Header("=== CONTROLS LIST ===")]
    // Each control row: label + action (created as children of controlsContainer)
    public Transform controlsContainer;   // Horizontal layout with 2 columns

    [Header("=== WARNING BOX ===")]
    public GameObject warningBox;         // Red background panel
    public TextMeshProUGUI warningText;
    // Text: "Warning: Excessive rum consumption may cause blackouts, hallucinations,\nand involuntary dancing. The ship's surgeon accepts no liability."

    [Header("=== BUTTONS ===")]
    public Button setSailButton;          // Gold button
    public Button adjustSailsButton;      // Dark button

    [Header("=== FOOTER ===")]
    public TextMeshProUGUI footerText;    // "MICRO JAM 2026 · WASD TO MOVE · DON'T DIE"

    [Header("=== SETTINGS ===")]
    public string gameSceneName = "GameScene"; // Name of your main game scene

    // ─── Awake / Start ────────────────────────────────────────────────────────

    void Awake()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    void Start()
    {
        PopulateStaticText();
        BindButtons();
        PlayIntroAnimation();
    }

    // ─── Static Text ──────────────────────────────────────────────────────────

    void PopulateStaticText()
    {
        if (subtitleText)  subtitleText.text  = "— A Cursed Voyage —";
        if (titleText)     titleText.text     = "Rum & Bones";
        if (editionText)   editionText.text   = "MICRO  JAM  EDITION";
        if (flavorText)    flavorText.text    =
            "Ye find yerself aboard a cursed ship, knee-deep in rum\n" +
            "and surrounded by ungrateful skeletons.\n" +
            "Drink wisely. Shoot straighter. Try not to pass out.";
        if (warningText)   warningText.text   =
            "⚠  Warning: Excessive rum consumption may cause blackouts, hallucinations,\n" +
            "and involuntary dancing. The ship's surgeon accepts no liability.";
        if (footerText)    footerText.text    = "MICRO JAM 2026  ·  WASD TO MOVE  ·  DON'T DIE";
    }

    // ─── Buttons ──────────────────────────────────────────────────────────────

    void BindButtons()
    {
        if (setSailButton)
            setSailButton.onClick.AddListener(OnSetSail);

        if (adjustSailsButton)
            adjustSailsButton.onClick.AddListener(OnAdjustSails);
    }

    void OnSetSail()
    {
        // Load the game scene — replace "GameScene" with your actual scene name
        SceneManager.LoadScene(gameSceneName);
    }

    void OnAdjustSails()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    // ─── Intro Animation (simple fade-in via CanvasGroup) ─────────────────────

    void PlayIntroAnimation()
    {
        var cg = mainMenuPanel?.GetComponent<CanvasGroup>();
        if (cg == null) return;
        cg.alpha = 0f;
        StartCoroutine(FadeIn(cg, 0.8f));
    }

    System.Collections.IEnumerator FadeIn(CanvasGroup cg, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Clamp01(t / duration);
            yield return null;
        }
        cg.alpha = 1f;
    }
}