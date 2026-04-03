// ================================================================
// KillTracker.cs
// Attach to: empty GameObject in scene
// ================================================================
using UnityEngine;
using System.Collections.Generic;

public class KillTracker : MonoBehaviour
{
    public static KillTracker Instance { get; private set; }

    [Header("Thresholds")]
    [SerializeField] private int[] levelThresholds = { 10, 50, 120, 250 };
    [SerializeField] private int[] levels = {1,2,3};

    public int TotalKills    { get; private set; } = 0;
    public int CurrentLevel  { get; private set; } = 0;

    // public event Action<int> OnKillCountChanged;    // (totalKills)
    // public event Action<int> OnLevelThresholdReached; // (level 1-4)

    // ----------------------------------------------------------------
    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ----------------------------------------------------------------
    // Call this from Enemy.cs on death
    // ----------------------------------------------------------------
    public void RegisterKill()
    {
        TotalKills++;

        CheckThreshold();
    }

    // ----------------------------------------------------------------
    private void CheckThreshold()
    {
        if (CurrentLevel >= levelThresholds.Length) return;

        if (TotalKills >= levelThresholds[CurrentLevel])
        {
            CurrentLevel++;
            Debug.Log($"[KillTracker] Level {CurrentLevel} threshold reached!");
            // OnLevelThresholdReached?.Invoke(CurrentLevel);
        }
    }
}