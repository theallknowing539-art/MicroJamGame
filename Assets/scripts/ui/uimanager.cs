using UnityEngine;

/// <summary>
/// GLOBAL UI MANAGER — ties all three screens together
/// Attach to a persistent "UIManager" GameObject (DontDestroyOnLoad optional)
/// 
/// ════════════════════════════════════════════════════════════════
///  QUICK SETUP GUIDE — READ THIS FIRST
/// ════════════════════════════════════════════════════════════════
///
/// FONTS (install via Package Manager or direct import):
///   • Title/Display : "UnifrakturMaguntia" (free on Google Fonts) — the gothic blackletter
///   • Body/UI       : "IM Fell English" or "Crimson Text" (Google Fonts)
///   • Caps/Labels   : "Cinzel" (Google Fonts)
///   Import as TextMeshPro Font Assets (Window > TextMeshPro > Font Asset Creator)
///
/// COLORS (use these hex values in your UI elements):
///   Background dark   : #0D0906
///   Card background   : #1A1208
///   Card border       : #8B6914
///   Gold primary      : #D4A017   (title "Rum & Bones", buttons)
///   Gold bright       : #F5C842   (highlight, large numbers)
///   Red accent        : #8B1A1A   (warning box, game over bg tint)
///   Red text          : #C0392B   (game over title, HP low warning)
///   Green stat        : #4CAF50   (wave number, rum drunk)
///   Warm cream        : #D4C5A0   (body text, flavor text)
///   Muted orange      : #C27A3A   (subtitle, taglines)
///   White/off-white   : #EDE3CC   (controls text)
///
/// BUTTON STYLES:
///   Gold "Set Sail" / "Back to Battle" / "Try Again":
///     Normal: #C9900A | Highlighted: #F5C842 | Pressed: #8B6914
///     Text: #1A0F00 (very dark, bold, Cinzel font, letter-spacing wide)
///   Dark secondary buttons:
///     Normal: #2A1F0E | Highlighted: #3D2E18 | Pressed: #1A1208
///     Border: #8B6914 (add an Outline component or Image border)
///     Text: #D4C5A0
///
/// BACKGROUND EFFECT (replicate the scanline/wood-panel look):
///   1. Add a black Panel as the bottom layer
///   2. On top: add another Panel, set alpha to ~30, use a
///      horizontal stripe sprite (or gradient) for the scanline effect
///   3. Add 2–4 small circular Images in corners (color #C9900A, size ~20px)
///      for the glowing dot decorations
///
/// CARD PANEL:
///   - Image component, color #1A1208, alpha 255
///   - Add a UI Outline (Script) or use a border Image sprite
///   - Border color #8B6914
///   - Add corner diamond sprites (✦) at each corner using TextMeshPro
///
/// INSTABILITY SLIDER (Pause Screen):
///   Fill color gradient: left=#4CAF50 → right=#FF6B35 (orange-red)
///   Background: #2A1F0E
///   No handle
///
/// ════════════════════════════════════════════════════════════════
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("=== SCREEN REFERENCES ===")]
    public MainMenuUI  mainMenuUI;
    public PauseMenuUI pauseMenuUI;
    public GameOverUI  gameOverUI;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // DontDestroyOnLoad(gameObject); // Uncomment if using across scenes
    }

    // ─── Called from your GameManager ─────────────────────────────────────────

    /// <summary>Show the pause screen with current game state.</summary>
    public void ShowPause(int hp, int wave, int kills, int rumLeft, float instability)
    {
        if (pauseMenuUI) pauseMenuUI.Show(hp, wave, kills, rumLeft, instability);
    }

    /// <summary>Show game over screen after player dies.</summary>
    public void ShowGameOver(int waves, int kills, int rumDrunk, int hangovers)
    {
        if (gameOverUI) gameOverUI.Show(waves, kills, rumDrunk, hangovers);
    }

    // ─── Example: hook this into your player/game controller ──────────────────
    // In your GameManager's Update():
    //
    //   if (Input.GetKeyDown(KeyCode.Escape) && !isPaused)
    //       UIManager.Instance.ShowPause(player.hp, waveManager.currentWave,
    //                                    killCount, player.rumLeft, player.instability);
    //
    //   void OnPlayerDied()
    //       UIManager.Instance.ShowGameOver(waveManager.currentWave,
    //                                       killCount, rumDrunk, hangovers);
}