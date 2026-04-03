using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class GameOverManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button restartButton;

    [Header("Movement References")]
    [Tooltip("Drag the Player object here to disable movement/look on death")]
    [SerializeField] private MonoBehaviour fpsController; 
    [Tooltip("Drag the weaponHolder object here to disable sway on death")]
    [SerializeField] private MonoBehaviour weaponSway;

    private void Start()
    {
        // 1. Ensure UI is hidden at start
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        // 2. Subscribe to Player Death
        if (PlayerHealth.Instance != null)
        {
            PlayerHealth.Instance.OnDied += OnPlayerDied;
        }

        // 3. Setup Restart Button
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);
    }

    private void OnPlayerDied()
    {
        if (gameOverPanel == null) return;

        // 4. Show the Death UI
        gameOverPanel.SetActive(true);
        
        // 5. Release the Mouse
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 6. STOP THE CAMERA FIGHT
        // This stops the mouse from fighting the falling animation
        if (fpsController != null) fpsController.enabled = false;
        if (weaponSway != null) weaponSway.enabled = false;

        // 7. Start the "Fall to the floor" sequence
        StartCoroutine(DeathCameraSequence());
        
        // 8. Dramatic Slow Motion
        Time.timeScale = 0.3f; 
    }

    private IEnumerator DeathCameraSequence()
    {
        Transform camTransform = Camera.main.transform;
        Vector3 startPos = camTransform.localPosition;
        Quaternion startRot = camTransform.localRotation;

        // Target: Drop to "ground height" and tilt up at the sky/skeletons
        Vector3 endPos = new Vector3(startPos.x, 0.2f, startPos.z); 
        Quaternion endRot = Quaternion.Euler(-60f, startRot.eulerAngles.y, 20f); 

        float elapsed = 0f;
        float duration = 1.2f; 

        while (elapsed < duration)
        {
            // We use unscaledDeltaTime because Time.timeScale is slowed down
            elapsed += Time.unscaledDeltaTime; 
            float t = elapsed / duration;

            // Ease out the movement so it feels like a heavy fall
            float smoothT = Mathf.Sin(t * Mathf.PI * 0.5f);

            camTransform.localPosition = Vector3.Lerp(startPos, endPos, smoothT);
            camTransform.localRotation = Quaternion.Slerp(startRot, endRot, smoothT);
            yield return null;
        }
    }

    public void RestartGame()
    {
        // CRITICAL: Reset time before loading!
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent errors on scene reload
        if (PlayerHealth.Instance != null)
        {
            PlayerHealth.Instance.OnDied -= OnPlayerDied;
        }
    }
}