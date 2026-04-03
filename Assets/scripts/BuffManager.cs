// ================================================================
// BuffManager.cs
// Attach to: empty GameObject in scene
// ================================================================
using System.Collections.Generic;
using UnityEngine;
using System;

public class BuffManager : MonoBehaviour
{
    public static BuffManager Instance { get; private set; }

    [Header("All Available Cards")]
    [SerializeField] private List<BuffData> allBuffs = new List<BuffData>();

    [Header("Cards Shown Per Selection")]
    [SerializeField] private int cardsShownCount = 3;

    // events
    public event Action<List<BuffData>, int> OnCardSelectionStarted; // (cards, level)
    public event Action<BuffData>            OnBuffApplied;

    // active buffs the player has chosen
    private List<BuffData> _activeBuffs = new List<BuffData>();

    // current level being offered
    private int _currentOfferLevel = 0;

    // ----------------------------------------------------------------
    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ----------------------------------------------------------------
    private void OnEnable()
    {
        KillTracker.Instance.OnLevelThresholdReached += ShowCardSelection;
    }

    private void OnDisable()
    {
        if (KillTracker.Instance != null)
            KillTracker.Instance.OnLevelThresholdReached -= ShowCardSelection;
    }

    // ----------------------------------------------------------------
    public void ShowCardSelection(int level)
    {
        _currentOfferLevel = level;

        // pick random cards from the pool
        List<BuffData> available = new List<BuffData>(allBuffs);
        List<BuffData> offered   = new List<BuffData>();

        int count = Mathf.Min(cardsShownCount, available.Count);
        while (offered.Count < count && available.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, available.Count);
            offered.Add(available[randomIndex]);
            available.RemoveAt(randomIndex);
        }

        // freeze game
        Time.timeScale = 0f;

        // fire event so UI can show the cards
        OnCardSelectionStarted?.Invoke(offered, level);
    }

    // ----------------------------------------------------------------
    public void SelectBuff(BuffData buff)
    {
        if (buff == null) return;

        // remove if already active — no stacking, just replace
        _activeBuffs.RemoveAll(b => b.buffType == buff.buffType);
        _activeBuffs.Add(buff);

        ApplyBuff(buff, _currentOfferLevel);

        OnBuffApplied?.Invoke(buff);

        // unfreeze game
        Time.timeScale = 1f;
    }

    // ----------------------------------------------------------------
    private void ApplyBuff(BuffData buff, int level)
    {
        float value = buff.GetValue(level);

        switch (buff.buffType)
        {
            case BuffType.KrakensBelch:
                if (FPSCharacterController.Instance != null)
                    FPSCharacterController.Instance.SetGroundSlamKnockbackMultiplier(1f + value);
                break;

            case BuffType.ScurvyStrike:
                DamageGun gun = FindObjectOfType<DamageGun>();
                if (gun != null)
                    gun.SetDamageMultiplier(1f + value);
                break;

            case BuffType.EternalGrog:
                if (PlayerHealth.Instance != null)
                    PlayerHealth.Instance.IncreaseMaxHealth(
                        PlayerHealth.Instance.MaxHealth * value);
                break;

            case BuffType.BoardingDash:
                if (FPSCharacterController.Instance != null)
                {
                    FPSCharacterController.Instance.SetWalkSpeedMultiplier(1f + value);
                    FPSCharacterController.Instance.SetSprintSpeedMultiplier(1f + value);
                }
                break;

            case BuffType.IronRibs:
                if (PlayerHealth.Instance != null)
                    PlayerHealth.Instance.SetDamageReduction(value);
                break;
        }

        Debug.Log($"[BuffManager] Applied {buff.buffName} at level {level} " +
                  $"with value {value * 100f}%");
    }

    // ----------------------------------------------------------------
    public bool HasBuff(BuffType type)
    {
        return _activeBuffs.Exists(b => b.buffType == type);
    }
}