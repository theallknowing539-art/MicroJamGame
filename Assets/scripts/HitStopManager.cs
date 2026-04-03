using UnityEngine;
using System.Collections;

public class HitStopManager : MonoBehaviour
{

    
    public static HitStopManager Instance;
    private void Awake()
    {
        Instance = this;
    }

    public void Stop(float duration)
    {
        StartCoroutine(Wait(duration));
    }

    IEnumerator Wait(float duration)
    {
        Time.timeScale = 0.0f; // This is what freezes the game!
        
        // Wait using "Realtime" because the game clock is now paused
        yield return new WaitForSecondsRealtime(duration);
        
        Time.timeScale = 1.0f; // This unfreezes the game
    }
}