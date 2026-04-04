using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// GAME OVER SCREEN (Davy Jones Sends His Regards)
/// 
/// SETUP:
/// 1. Create a new Scene or a full-screen overlay Panel
/// 2. Attach this script to the root Panel or a manager GO
/// 3. Wire all fields in Inspector
/// 4. Call Show() from your GameManager when player dies
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("=== HEADER ===")]
    public TextMeshProUGUI perishedLabel;      // "— Ye have perished, ye scurvy dog —"
    public TextMeshProUGUI titleLine1;         // "Davy Jones"
    public TextMeshProUGUI titleLine2;         // "Sends His Regards"
    public TextMeshProUGUI taglineText;        // "At least ye died with yer boots on. And a bottle in hand."

    [Header("=== FINAL LOG ===")]
    public TextMeshProUGUI logHeader;          // "YE FINAL LOG"

    public TextMeshProUGUI wavesValue;         // e.g. "5"
    public TextMeshProUGUI wavesLabel;         // "WAVES"
    public TextMeshProUGUI killsValue;         // e.g. "83"
    public TextMeshProUGUI killsLabel;         // "KILLS"
    public TextMeshProUGUI rumDrunkValue;      // e.g. "12"
    public TextMeshProUGUI rumDrunkLabel;      // "RUM DRUNK"
    public TextMeshProUGUI hangoversValue;     // e.g. "4"
    public TextMeshProUGUI hangoversLabel;     // "HANGOVERS"

    [Header("=== FINAL WORDS ===")]
    public TextMeshProUGUI finalWordsHeader;   // "✦ FINAL WORDS ✦"
    public TextMeshProUGUI finalWordsText;     // Randomized epitaph

    [Header("=== BUTTONS ===")]
    public Button tryAgainButton;
    public Button mainMenuButton;

    [Header("=== FOOTER ===")]
    public TextMeshProUGUI footerText;         // "MICRO JAM 2026 · REST IN PIECES"

    [Header("=== SCENE NAMES ===")]
    public string gameSceneName  = "GameScene";
    public string mainMenuSceneName = "MainMenu";

    // ─── Pool of pirate epitaphs ──────────────────────────────────────────────

    static readonly string[] Epitaphs = {
        "\"She had the heart of a lion and the aim of a very drunk lion.\nWe'll miss her. The skeletons won't.\"",
        "\"He sailed into eternity, rum in hand, dignity overboard.\"",
        "\"Fought bravely. Reloaded slowly. Drank quickly.\"",
        "\"The sea takes us all. She just took him first and kept the bottle.\"",
        "\"He didn't fear death. He feared running out of rum before it.\"",
        "\"May the waves be smooth and the skeletons fewer where ye go.\"",
        "\"Died as he lived: confused, armed, and slightly seasick.\"",
        "\"He aimed for the stars. He hit the deck instead.\"",
    };

    // ─── Awake ────────────────────────────────────────────────────────────────

    void Awake()
    {
        gameObject.SetActive(false);
    }

    // ─── Called by GameManager on player death ────────────────────────────────

    public void Show(int wavesReached, int totalKills, int rumConsumed, int hangovers)
    {
        gameObject.SetActive(true);
        Time.timeScale = 0f;

        PopulateText();
        PopulateStats(wavesReached, totalKills, rumConsumed, hangovers);
        BindButtons();
    }

    // ─── Static Text ──────────────────────────────────────────────────────────

    void PopulateText()
    {
        if (perishedLabel)    perishedLabel.text    = "— Ye have perished, ye scurvy dog —";
        if (titleLine1)       titleLine1.text       = "Davy Jones";
        if (titleLine2)       titleLine2.text       = "Sends His Regards";
        if (taglineText)      taglineText.text      = "At least ye died with yer boots on. And a bottle in hand.";
        if (logHeader)        logHeader.text        = "YE FINAL LOG";
        if (wavesLabel)       wavesLabel.text       = "WAVES";
        if (killsLabel)       killsLabel.text       = "KILLS";
        if (rumDrunkLabel)    rumDrunkLabel.text    = "RUM DRUNK";
        if (hangoversLabel)   hangoversLabel.text   = "HANGOVERS";
        if (finalWordsHeader) finalWordsHeader.text = "✦  FINAL WORDS  ✦";
        if (footerText)       footerText.text       = "MICRO JAM 2026  ·  REST IN PIECES";

        // Random epitaph
        if (finalWordsText)
            finalWordsText.text = Epitaphs[Random.Range(0, Epitaphs.Length)];
    }

    void PopulateStats(int waves, int kills, int rum, int hangovers)
    {
        if (wavesValue)     wavesValue.text     = waves.ToString();
        if (killsValue)     killsValue.text     = kills.ToString();
        if (rumDrunkValue)  rumDrunkValue.text  = rum.ToString();
        if (hangoversValue) hangoversValue.text = hangovers.ToString();
    }

    // ─── Buttons ──────────────────────────────────────────────────────────────

    void BindButtons()
    {
        if (tryAgainButton)
        {
            tryAgainButton.onClick.RemoveAllListeners();
            tryAgainButton.onClick.AddListener(() =>
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(gameSceneName);
            });
        }

        if (mainMenuButton)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(() =>
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(mainMenuSceneName);
            });
        }
    }
}