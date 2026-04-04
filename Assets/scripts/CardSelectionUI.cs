using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardSelectionUI : MonoBehaviour
{
    [Header("Card Slots")]
    [SerializeField] private BuffCardUI[] cardSlots; // Ensure this array size is 6 in the Inspector

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
        // Show the panel
        transform.localScale = Vector3.one;

        // Loop through your 6 UI slots
        for (int i = 0; i < cardSlots.Length; i++)
        {
            // If the manager sent a card for this index, set it up
            if (i < cards.Count)
            {
                cardSlots[i].gameObject.SetActive(true);
                cardSlots[i].Setup(cards[i], level);
            }
            else
            {
                // If the manager sent fewer than 6 cards, hide the extra slots
                cardSlots[i].gameObject.SetActive(false);
            }
        }
    }

    public void HideCards(BuffData selected)
    {
        transform.localScale = Vector3.zero;
    }
}