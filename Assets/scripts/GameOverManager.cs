using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject gameOverPanel;
    public Button restartButton;

    [Header("Movement References")]
    public MonoBehaviour fpsController;
    public MonoBehaviour weaponSway;

    private void Awake()
    {
        Instance = this;
        // Make sure the panel is hidden at the start
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        
        // Setup the button click event via code
        if (restartButton != null) restartButton.onClick.AddListener(RestartGame);
    }

    public void TriggerGameOver()
{
    if (gameOverPanel != null) gameOverPanel.SetActive(true);

    // Disable the FPS Controller script specifically
    if (fpsController != null) fpsController.enabled = false;
    
    // Disable the Sway/Weapon holder so it doesn't "bounce" back
    if (weaponSway != null) {
        weaponSway.gameObject.SetActive(false); // Just hide the gun entirely
        weaponSway.enabled = false;
    }

    Time.timeScale = 0f; 
    Cursor.lockState = CursorLockMode.None;
    Cursor.visible = true;
}

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}