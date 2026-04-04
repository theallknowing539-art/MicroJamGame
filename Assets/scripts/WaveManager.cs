using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [System.Serializable]
    public class WaveData
    {
        public string waveName = "Wave 1";
        public int enemyCount = 5;
        public float spawnInterval = 1.5f;
    }

    [Header("Wave Settings")]
    [SerializeField] private List<WaveData> waves = new List<WaveData>();
    [SerializeField] private int maxWaves = 3; 
    [SerializeField] private float breakDuration = 5f;
    
    [Header("Victory UI")]
    [SerializeField] private GameObject victoryPanel;

    // --- Note: Headers must be above variables, not actions ---
    public event Action<int, int>  OnWaveStarted;
    public event Action<int>       OnWaveCleared;
    public event Action<float>     OnBreakTick;
    public event Action            OnAllWavesCleared;
    public event Action<int>       OnEnemyCountChanged;

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

    private IEnumerator RunWaves()
    {
        IsRunning = true;
        yield return new WaitForSeconds(2f);

        while (CurrentWave < maxWaves)
        {
            WaveData data = BuildWaveData(CurrentWave);

            CurrentWave++;
            EnemiesRemaining = data.enemyCount;
            IsBreak = false;

            OnWaveStarted?.Invoke(CurrentWave, data.enemyCount);

            if (EnemySpawner.Instance != null)
                StartCoroutine(EnemySpawner.Instance.SpawnWave(data.enemyCount, data.spawnInterval));

            yield return new WaitUntil(() => EnemiesRemaining <= 0);

            OnWaveCleared?.Invoke(CurrentWave);

            if (BuffManager.Instance != null)
            {
                BuffManager.Instance.TriggerBuffSelection(CurrentWave);
            }

            yield return new WaitUntil(() => Time.timeScale > 0);

            if (CurrentWave >= maxWaves)
            {
                WinGame();
                yield break; 
            }

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

    private void WinGame()
    {
        Debug.Log("<color=gold>VICTORY!</color>");
        IsRunning = false;
        
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (victoryPanel != null)
            victoryPanel.SetActive(true);
        
        OnAllWavesCleared?.Invoke();
    }

    private WaveData BuildWaveData(int waveIndex)
    {
        if (waveIndex < waves.Count) return waves[waveIndex];
        return new WaveData { waveName = $"Wave {waveIndex + 1}", enemyCount = 10, spawnInterval = 1f };
    }

    public void ReportEnemyDeath()
    {
        EnemiesRemaining = Mathf.Max(0, EnemiesRemaining - 1);
        OnEnemyCountChanged?.Invoke(EnemiesRemaining);
    }
}