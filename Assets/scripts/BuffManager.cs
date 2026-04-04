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
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        // Press B to test selection logic
        if (Input.GetKeyDown(KeyCode.B))
        {
            int waveToTest = (WaveManager.Instance != null) ? WaveManager.Instance.CurrentWave : 1;
            if (waveToTest == 0) waveToTest = 1;

            Debug.Log($"[Cheat] Testing random selection for Wave {waveToTest}");
            TriggerBuffSelection(waveToTest); 
        }
    }

    public void TriggerBuffSelection(int currentWave)
{
    // 1. Filter the cards
    List<BuffData> eligiblePool = allBuffs.FindAll(b => b.requiredWave == currentWave);

    if (eligiblePool.Count == 0)
    {
        Debug.LogWarning($"[BuffManager] No cards found for Wave {currentWave}! Falling back to Wave 1.");
        eligiblePool = allBuffs.FindAll(b => b.requiredWave == 1);
    }

    // --- MOVE THE DEFINITION UP HERE ---
    List<BuffData> selectedCards = new List<BuffData>();
    List<BuffData> tempPool = new List<BuffData>(eligiblePool);

    // 2. Pick 3 unique cards
    for (int i = 0; i < 3; i++)
    {
        if (tempPool.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, tempPool.Count);
            selectedCards.Add(tempPool[randomIndex]);
            tempPool.RemoveAt(randomIndex);
        }
    }

    // --- NOW CALL FREEZE (After selectedCards is ready) ---
    if (FPSCharacterController.Instance != null)
    {
        FPSCharacterController.Instance.FreezePlayer(true);
    }

    // 3. UI AND PAUSE
    Time.timeScale = 0f;
    OnCardSelectionStarted?.Invoke(selectedCards, currentWave);
    
    Cursor.lockState = CursorLockMode.None;
    Cursor.visible = true;
}

    public void ApplyBuff(BuffData selectedBuff)
    {
        if (selectedBuff == null) return;

        Debug.Log($"<color=green>Buff Applied:</color> {selectedBuff.buffName}");
        
        // Resume game and hide cursor
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Notify PlayerBuffs and other systems
        OnBuffApplied?.Invoke(selectedBuff);

        if (FPSCharacterController.Instance != null)
        FPSCharacterController.Instance.FreezePlayer(false);

    Time.timeScale = 1f;
    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false;
    
    OnBuffApplied?.Invoke(selectedBuff);
    }
}