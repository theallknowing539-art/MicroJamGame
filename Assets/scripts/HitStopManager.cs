using System.Collections;
using UnityEngine;

public class HitStopManager : MonoBehaviour
{
    private static HitStopManager _instance;
    public static HitStopManager Instance {
        get {
            if (_instance == null) {
                _instance = new GameObject("HitStopManager").AddComponent<HitStopManager>();
            }
            return _instance;
        }
    }

    public void Stop(float duration) {
        StartCoroutine(Wait(duration));
    }

    private IEnumerator Wait(float duration) {
        Time.timeScale = 0.02f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }
}