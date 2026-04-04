using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardSelectionUI : MonoBehaviour
{
    [Header("Card Slots")]
    // This array contains your 6 slots from the Inspector
    [SerializeField] private BuffCardUI[] cardSlots; 

    private void OnEnable()
    {
        StartCoroutine(WaitAndSubscribe());
    }

    private IEnumerator WaitAndSubscribe()
    {
        while (BuffManager.Instance == null)
        {
            yield return null; 
        }

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

    private void Start()
    {
        // Hide UI at start
        transform.localScale = Vector3.zero;
    }

    public void ShowCards(List<BuffData> cards, int level)
    {
        // 1. Show the panel
        transform.localScale = Vector3.one;

        // 2. Loop through every physical slot you have (all 6)
        for (int i = 0; i < cardSlots.Length; i++)
        {
            // 3. Only show a slot if we have a card for it AND we haven't hit 3 cards yet
            // (The BuffManager already limits cards.Count to 3, so this is extra safe)
            if (i < cards.Count && i < 3)
            {
                cardSlots[i].gameObject.SetActive(true);
                cardSlots[i].Setup(cards[i], level);
            }
            else
            {
                // 4. Hide Slot 4, 5, and 6 (or any slot without a card)
                cardSlots[i].gameObject.SetActive(false);
            }
        }
    }

    public void HideCards(BuffData selected)
    {
        transform.localScale = Vector3.zero;
    }
}