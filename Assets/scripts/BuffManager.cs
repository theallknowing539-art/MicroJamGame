using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class BuffManager : MonoBehaviour
{
    public static BuffManager Instance { get; private set; }

    [Header("All Possible Buffs")]
    [SerializeField] private List<BuffData> allBuffs = new List<BuffData>();

    public event Action<List<BuffData>, int> OnCardSelectionStarted;
    public event Action<BuffData> OnBuffApplied;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
{
    // Press B to test cards for the CURRENT wave
    if (Input.GetKeyDown(KeyCode.B))
    {
        if (WaveManager.Instance != null)
        {
            // This grabs the real wave number from your WaveManager
            int waveToTest = WaveManager.Instance.CurrentWave;
            
            // If the wave hasn't started yet (is 0), default to 1 so it doesn't error
            if (waveToTest == 0) waveToTest = 1;

            Debug.Log($"[Cheat] Testing cards for Wave {waveToTest}");
            TriggerBuffSelection(waveToTest); 
        }
        else
        {
            // Fallback if WaveManager is missing
            TriggerBuffSelection(1); 
        }
    }
}

    public void TriggerBuffSelection(int currentWave)
    {
        Debug.Log($"[BuffManager] Filtering cards for Wave: {currentWave}");

        // 1. Filter strictly for the current wave
        List<BuffData> waveSpecificCards = allBuffs.FindAll(b => b.requiredWave == currentWave);

        // 2. ERROR CHECK: If this list is empty, your Assets are not set up correctly!
        if (waveSpecificCards.Count == 0)
        {
            Debug.LogError($"[BuffManager] Zero cards found with Required Wave = {currentWave}! Check your Assets in the Project folder.");
            // We return here so we don't show the wrong cards
            return; 
        }

        List<BuffData> selectedCards = new List<BuffData>();
        List<BuffData> pool = new List<BuffData>(waveSpecificCards);

        // 3. Pick up to 6 cards
        for (int i = 0; i < 6 && pool.Count > 0; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, pool.Count);
            selectedCards.Add(pool[randomIndex]);
            pool.RemoveAt(randomIndex);
        }

        Time.timeScale = 0f;
        OnCardSelectionStarted?.Invoke(selectedCards, currentWave);
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ApplyBuff(BuffData selectedBuff)
    {
        Debug.Log($"Applied Buff: {selectedBuff.buffName}");
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        OnBuffApplied?.Invoke(selectedBuff);
    }
}