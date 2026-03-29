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
    // Wave Definition — designer fills these out in the inspector
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

    [Header("Scaling — applied on top of wave definitions")]
    [Tooltip("Extra enemies added per wave beyond the defined list.")]
    [SerializeField] private int enemiesAddedPerWave = 2;

    [Tooltip("After all defined waves are cleared, keep spawning with this many enemies " +
             "plus the scaling above. Set to 0 to end the game after the last wave.")]
    [SerializeField] private int endlessBaseEnemyCount = 10;

    [Header("Break Between Waves")]
    [SerializeField] private float breakDuration = 10f;    // seconds the player gets to breathe

    // ----------------------------------------------------------------
    // Events — UI and other systems subscribe to these
    // ----------------------------------------------------------------
    public event Action<int, int>  OnWaveStarted;       // (waveNumber, totalEnemies)
    public event Action<int>       OnWaveCleared;        // (waveNumber)
    public event Action<float>     OnBreakTick;          // (secondsRemaining) — fires every second
    public event Action            OnAllWavesCleared;    // fires if endless mode is off and waves run out
    public event Action<int>       OnEnemyCountChanged;  // (enemiesRemainingThisWave)

    // ----------------------------------------------------------------
    // State
    // ----------------------------------------------------------------
    public int  CurrentWave        { get; private set; } = 0;
    public int  EnemiesRemaining   { get; private set; } = 0;
    public bool IsBreak            { get; private set; } = false;
    public bool IsRunning          { get; private set; } = false;

    // ----------------------------------------------------------------
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
    // Main loop
    // ----------------------------------------------------------------
    private IEnumerator RunWaves()
    {
        IsRunning = true;

        // short delay before the first wave so the scene can finish loading
        yield return new WaitForSeconds(2f);

        while (true)
        {
            // build the wave data for this wave number
            WaveData data = BuildWaveData(CurrentWave);

            // start the wave
            CurrentWave++;
            EnemiesRemaining = data.enemyCount;
            IsBreak = false;

            OnWaveStarted?.Invoke(CurrentWave, data.enemyCount);

            // tell the spawner to start spawning
            if (EnemySpawner.Instance != null)
                StartCoroutine(EnemySpawner.Instance.SpawnWave(data.enemyCount, data.spawnInterval));

            // wait until all enemies are dead
            yield return new WaitUntil(() => EnemiesRemaining <= 0);

            OnWaveCleared?.Invoke(CurrentWave);

            // check if we've run out of defined waves and endless mode is off
            bool beyondDefinedWaves = CurrentWave >= waves.Count;
            if (beyondDefinedWaves && endlessBaseEnemyCount == 0)
            {
                OnAllWavesCleared?.Invoke();
                IsRunning = false;
                yield break;
            }

            // break countdown
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

    // ----------------------------------------------------------------
    // Builds the WaveData for the given wave index.
    // Uses the designer-defined list first, then scales endlessly.
    // ----------------------------------------------------------------
    private WaveData BuildWaveData(int waveIndex)
    {
        WaveData data = new WaveData();

        if (waveIndex < waves.Count)
        {
            // use the hand-crafted wave, then add scaling on top
            WaveData defined = waves[waveIndex];
            data.waveName      = defined.waveName;
            data.enemyCount    = defined.enemyCount + (waveIndex * enemiesAddedPerWave);
            data.spawnInterval = defined.spawnInterval;
        }
        else
        {
            // endless — keep scaling past the defined list
            data.waveName      = $"Wave {waveIndex + 1}";
            data.enemyCount    = endlessBaseEnemyCount + (waveIndex * enemiesAddedPerWave);
            data.spawnInterval = Mathf.Max(0.3f, 1.5f - (waveIndex * 0.05f)); // gets slightly faster
        }

        return data;
    }

    // ----------------------------------------------------------------
    // Call this from the enemy's death script
    // ----------------------------------------------------------------
    public void ReportEnemyDeath()
    {
        EnemiesRemaining = Mathf.Max(0, EnemiesRemaining - 1);
        OnEnemyCountChanged?.Invoke(EnemiesRemaining);
    }
}