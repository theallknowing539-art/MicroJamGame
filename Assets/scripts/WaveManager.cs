using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class WaveManager : MonoBehaviour
{
    // ----------------------------------------------------------------
    // Singleton
    // ----------------------------------------------------------------
    public static WaveManager Instance { get; private set; }

    // ----------------------------------------------------------------
    // Wave Definition
    // ----------------------------------------------------------------
    [System.Serializable]
    public class WaveData
    {
        public string waveName = "Wave 1";
        [Tooltip("How many enemies spawn in this wave.")]
        public int enemyCount = 5;
        [Tooltip("Delay in seconds between each enemy spawn.")]
        public float spawnInterval = 1.5f;
    }

    // ----------------------------------------------------------------
    // Settings
    // ----------------------------------------------------------------
    [Header("Wave Definitions")]
    [SerializeField] private List<WaveData> waves = new List<WaveData>();

    [Header("Scaling")]
    [SerializeField] private int enemiesAddedPerWave = 2;
    [SerializeField] private int endlessBaseEnemyCount = 10;

    [Header("Break Between Waves")]
    [SerializeField] private float breakDuration = 10f;

    // ----------------------------------------------------------------
    // Events
    // ----------------------------------------------------------------
    public event Action<int, int>  OnWaveStarted;
    public event Action<int>       OnWaveCleared;
    public event Action<float>     OnBreakTick;
    public event Action            OnAllWavesCleared;
    public event Action<int>       OnEnemyCountChanged;

    // ----------------------------------------------------------------
    // State
    // ----------------------------------------------------------------
    public int  CurrentWave        { get; private set; } = 0;
    public int  EnemiesRemaining   { get; private set; } = 0;
    public bool IsBreak            { get; private set; } = false;
    public bool IsRunning          { get; private set; } = false;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        StartCoroutine(RunWaves());
    }

    // ----------------------------------------------------------------
    // Main Wave Loop
    // ----------------------------------------------------------------
    private IEnumerator RunWaves()
    {
        IsRunning = true;
        yield return new WaitForSeconds(2f);

        while (true)
        {
            WaveData data = BuildWaveData(CurrentWave);

            // Start the wave
            CurrentWave++;
            EnemiesRemaining = data.enemyCount;
            IsBreak = false;

            OnWaveStarted?.Invoke(CurrentWave, data.enemyCount);

            if (EnemySpawner.Instance != null)
                StartCoroutine(EnemySpawner.Instance.SpawnWave(data.enemyCount, data.spawnInterval));

            // 1. Wait until all enemies in the current wave are dead
            yield return new WaitUntil(() => EnemiesRemaining <= 0);

            // 2. Trigger the Wave Cleared Event
            OnWaveCleared?.Invoke(CurrentWave);

            // 3. TRIGGER THE BUFF SELECTION
            // This pauses the game and shows the 3 cards for the current wave level
            if (BuffManager.Instance != null)
            {
                BuffManager.Instance.TriggerBuffSelection(CurrentWave);
            }

            // 4. Wait for the player to click a card
            // Since BuffManager sets Time.timeScale to 0, this loop effectively 
            // waits for the player to make a choice before moving to the break.
            yield return new WaitUntil(() => Time.timeScale > 0);

            // Check if game should end
            bool beyondDefinedWaves = CurrentWave >= waves.Count;
            if (beyondDefinedWaves && endlessBaseEnemyCount == 0)
            {
                OnAllWavesCleared?.Invoke();
                IsRunning = false;
                yield break;
            }

            // 5. Start the break countdown
            IsBreak = true;
            float timeLeft = breakDuration;
            while (timeLeft > 0f)
            {
                OnBreakTick?.Invoke(timeLeft);
                yield return new WaitForSeconds(1f);
                timeLeft -= 1f;
            }
            OnBreakTick?.Invoke(0f);
        }
    }

    private WaveData BuildWaveData(int waveIndex)
    {
        WaveData data = new WaveData();
        if (waveIndex < waves.Count)
        {
            WaveData defined = waves[waveIndex];
            data.waveName      = defined.waveName;
            data.enemyCount    = defined.enemyCount + (waveIndex * enemiesAddedPerWave);
            data.spawnInterval = defined.spawnInterval;
        }
        else
        {
            data.waveName      = $"Wave {waveIndex + 1}";
            data.enemyCount    = endlessBaseEnemyCount + (waveIndex * enemiesAddedPerWave);
            data.spawnInterval = Mathf.Max(0.3f, 1.5f - (waveIndex * 0.05f));
        }
        return data;
    }

    public void ReportEnemyDeath()
    {
        EnemiesRemaining = Mathf.Max(0, EnemiesRemaining - 1);
        OnEnemyCountChanged?.Invoke(EnemiesRemaining);
    }
}