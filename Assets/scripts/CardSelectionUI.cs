// ================================================================
// CardSelectionUI.cs
// Attach to: the card selection Canvas GameObject
// ================================================================
using System.Collections.Generic;
using UnityEngine;

public class CardSelectionUI : MonoBehaviour
{
    [Header("Card Slots")]
    [SerializeField] private BuffCardUI[] cardSlots;   // 3 slots

    // ----------------------------------------------------------------
    private void OnEnable()
    {
        BuffManager.Instance.OnCardSelectionStarted += ShowCards;
        BuffManager.Instance.OnBuffApplied          += HideCards;
    }

    private void OnDisable()
    {
        if (BuffManager.Instance != null)
        {
            BuffManager.Instance.OnCardSelectionStarted -= ShowCards;
            BuffManager.Instance.OnBuffApplied          -= HideCards;
        }
    }

    // ----------------------------------------------------------------
    private void Start()
    {
        // hide on start
        gameObject.SetActive(false);
    }

    // ----------------------------------------------------------------
    private void ShowCards(List<BuffData> cards, int level)
    {
        gameObject.SetActive(true);

        for (int i = 0; i < cardSlots.Length; i++)
        {
            if (i < cards.Count)
            {
                cardSlots[i].gameObject.SetActive(true);
                cardSlots[i].Setup(cards[i], level);
            }
            else
            {
                cardSlots[i].gameObject.SetActive(false);
            }
        }
    }

    // ----------------------------------------------------------------
    private void HideCards(BuffData selected)
    {
        gameObject.SetActive(false);
    }
}