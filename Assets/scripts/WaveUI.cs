using UnityEngine;
using TMPro; // If using TextMeshPro

public class WaveUI : MonoBehaviour
{
    public TextMeshProUGUI waveText;

    void Update()
    {
        if (WaveManager.Instance != null)
        {
            waveText.text = $"Wave: {WaveManager.Instance.CurrentWave}\n" +
                            $"Enemies: {WaveManager.Instance.EnemiesRemaining}\n" +
                            $"Status: {(WaveManager.Instance.IsBreak ? "Resting..." : "FIGHT!")}";
        }
    }
}