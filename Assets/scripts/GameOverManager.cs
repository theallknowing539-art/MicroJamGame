using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections; // Required for Coroutines

public class GameOverManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button restartButton;

    private void Start()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (PlayerHealth.Instance != null)
        {
            PlayerHealth.Instance.OnDied += OnPlayerDied;
        }

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);
    }

    private void OnPlayerDied()
    {
        if (gameOverPanel == null) return;

        gameOverPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // --- NEW: DEATH CAMERA EFFECT ---
        StartCoroutine(DeathCameraSequence());
        
        // Slow down time slightly so the camera movement is visible
        Time.timeScale = 0.3f; 
    }

    private IEnumerator DeathCameraSequence()
    {
        // Get the camera transform
        Transform camTransform = Camera.main.transform;
        Vector3 startPos = camTransform.localPosition;
        Quaternion startRot = camTransform.localRotation;

        // Target: Drop slightly to the "ground" and look up at an angle
        Vector3 endPos = new Vector3(startPos.x, 0.2f, startPos.z); 
        Quaternion endRot = Quaternion.Euler(-60f, startRot.eulerAngles.y, 20f); 

        float elapsed = 0f;
        float duration = 1.0f; // Time in seconds for the fall

        while (elapsed < duration)
        {
            // Use unscaledDeltaTime because Time.timeScale is 0.3f
            elapsed += Time.unscaledDeltaTime; 
            float t = elapsed / duration;

            // Smoothly interpolate position and rotation
            camTransform.localPosition = Vector3.Lerp(startPos, endPos, t);
            camTransform.localRotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (PlayerHealth.Instance != null)
        {
            PlayerHealth.Instance.OnDied -= OnPlayerDied;
        }
    }
}