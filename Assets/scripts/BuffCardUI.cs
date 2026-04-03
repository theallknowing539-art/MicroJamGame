// ================================================================
// BuffCardUI.cs
// Attach to: each card GameObject in the selection Canvas
// ================================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuffCardUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image      cardIcon;
    [SerializeField] private TextMeshProUGUI cardName;
    [SerializeField] private TextMeshProUGUI cardDescription;
    [SerializeField] private TextMeshProUGUI cardLevel;
    [SerializeField] private Button     selectButton;

    private BuffData _buffData;

    // ----------------------------------------------------------------
    public void Setup(BuffData data, int level)
    {
        _buffData = data;

        if (cardIcon        != null) cardIcon.sprite      = data.icon;
        if (cardName        != null) cardName.text        = data.buffName;
        if (cardDescription != null) cardDescription.text = data.GetDescription(level);
        if (cardLevel       != null) cardLevel.text       = $"LEVEL {level}";

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(OnCardSelected);
    }

    // ----------------------------------------------------------------
    private void OnCardSelected()
    {
        BuffManager.Instance.SelectBuff(_buffData);
    }
}