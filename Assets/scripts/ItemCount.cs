// ================================================================
// ItemCount.cs
// Attach to: any GameObject in the scene (e.g. your Canvas or UI manager)
// ================================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemCount : MonoBehaviour
{
    [Header("Rum Bottle UI")]
    [SerializeField] private Image _bottleIcon;
    [SerializeField] private TextMeshProUGUI _bottleCountText;
    [SerializeField] private ItemData _rumBottleData;

    [Header("Ammo UI")]
    [SerializeField] private Image _ammoIcon;
    [SerializeField] private TextMeshProUGUI _ammoCountText;
    [SerializeField] private TextMeshProUGUI _ammoReserveText;   // shows currentAmmo / reserveAmmo

    [Header("Gun Reference")]
    [SerializeField] private Gun _gun;

    // ----------------------------------------------------------------
    private void OnEnable()
    {
        Inventory.Instance.OnInventoryChanged += UpdateBottleCount;
        _gun.OnAmmoChanged                    += UpdateAmmoCount;
    }

    private void OnDisable()
    {
        Inventory.Instance.OnInventoryChanged -= UpdateBottleCount;
        _gun.OnAmmoChanged                    -= UpdateAmmoCount;
    }

    // ----------------------------------------------------------------
    private void Start()
    {
        // set icons if assigned
        if (_bottleIcon != null && _rumBottleData != null && _rumBottleData.icon != null)
            _bottleIcon.sprite = _rumBottleData.icon;

        if (_ammoIcon != null && _rumBottleData != null)
            _ammoIcon.sprite = _rumBottleData.icon;

        // initialise both displays
        UpdateBottleCount();
        UpdateAmmoCount();
    }

    // ----------------------------------------------------------------
    private void UpdateBottleCount()
    {
        if (_rumBottleData == null) return;

        int count = Inventory.Instance.GetCount(_rumBottleData);

        if (_bottleCountText != null)
            _bottleCountText.text = $"x{count}";

        // hide icon and text if player has none
        if (_bottleIcon != null)
            _bottleIcon.enabled = count > 0;

        if (_bottleCountText != null)
            _bottleCountText.enabled = count > 0;
    }

    // ----------------------------------------------------------------
    private void UpdateAmmoCount()
    {
        if (_gun == null) return;

        if (_ammoCountText != null)
            _ammoCountText.text = _gun.CurrentAmmo.ToString();

        if (_ammoReserveText != null)
            _ammoReserveText.text = $"/ {_gun.ReserveAmmo}";

        // show reloading text when reloading
        if (_gun.IsReloading)
        {
            if (_ammoCountText != null)
                _ammoCountText.text = "...";
        }
    }
}
