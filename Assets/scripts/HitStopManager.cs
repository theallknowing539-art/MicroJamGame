using System.Collections;
using UnityEngine;

public class HitStopManager : MonoBehaviour
{
    public static HitStopManager Instance { get; private set; }

    private bool _isWaiting = false;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Stop(float duration)
    {
        if (_isWaiting) return;
        StartCoroutine(Wait(duration));
    }

    private IEnumerator Wait(float duration)
    {
        _isWaiting = true;
        
        // Record the current time scale
        float originalTimeScale = Time.timeScale;
        
        // Freeze the game
        Time.timeScale = 0.0f;
        
        // Wait for REAL seconds (because Time.timeScale is 0!)
        yield return new WaitForSecondsRealtime(duration);
        
        // Unfreeze
        Time.timeScale = originalTimeScale;
        
        _isWaiting = false;
    }
}