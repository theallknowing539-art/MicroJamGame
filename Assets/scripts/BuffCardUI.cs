using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuffCardUI : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public Image cardIcon;
    public Button selectButton;

    private BuffData _currentBuff;

    // This now matches the (BuffData, int) signature your UI is calling
    public void Setup(BuffData data, int level)
    {
        _currentBuff = data;

        if (titleText != null) titleText.text = data.buffName;
        if (descriptionText != null) descriptionText.text = data.description;
        if (cardIcon != null) cardIcon.sprite = data.cardSprite;

        // Clear old clicks and add the new one
        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(HandleClick);
    }

    private void HandleClick()
    {
        // Tell the BuffManager that the player chose THIS buff
        if (BuffManager.Instance != null)
        {
            BuffManager.Instance.ApplyBuff(_currentBuff);
        }
    }
}