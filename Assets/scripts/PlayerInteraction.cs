// ================================================================
// PlayerInteraction.cs
// Attach to: Player GameObject
// ================================================================
using UnityEngine;
using UnityEngine.UI;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private Transform cameraTransform;

    [Header("Crosshair")]
    [SerializeField] private Image crosshairImage;
    [SerializeField] private Sprite crosshairDefault;
    [SerializeField] private Sprite crosshairInteractable;
    [SerializeField] private Color crosshairDefaultColor  = Color.white;
    [SerializeField] private Color crosshairInteractColor = Color.yellow;

    [Header("World Space UI")]
    [SerializeField] private Canvas iconCanvas;
    [SerializeField] private Image keyImage;
    [SerializeField] private float uiRange = 2f;

    private IInteractable _currentTarget;

    // ----------------------------------------------------------------
    private void Start()
    {
        if (crosshairImage != null)
            crosshairImage.sprite = crosshairDefault;

        if (keyImage != null)
            keyImage.enabled = false;
    }

    // ----------------------------------------------------------------
    private void Update()
    {
        ScanForInteractable();

        if (_currentTarget != null && Input.GetKeyDown(KeyCode.E))
            _currentTarget.Interact();
    }

    // ----------------------------------------------------------------
    private void ScanForInteractable()
    {
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        // cast against everything, check for interface on hit
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                if (interactable != _currentTarget)
                {
                    _currentTarget = interactable;
                    SetCrosshair(true);
                }

                // update world space UI position and rotation
                if (iconCanvas != null && hit.distance <= uiRange)
                {
                    iconCanvas.transform.position = hit.transform.position + Vector3.up * 0.3f;
                    iconCanvas.transform.LookAt(cameraTransform);
                    iconCanvas.transform.Rotate(0, 180, 0);
                    if (keyImage != null) keyImage.enabled = true;
                }
                return;
            }
        }

        // nothing hit or hit something non-interactable
        if (_currentTarget != null)
        {
            _currentTarget = null;
            SetCrosshair(false);
            if (keyImage != null) keyImage.enabled = false;
        }
    }

    // ----------------------------------------------------------------
    private void SetCrosshair(bool isLookingAtInteractable)
    {
        if (crosshairImage == null) return;

        crosshairImage.sprite = isLookingAtInteractable
            ? crosshairInteractable
            : crosshairDefault;

        crosshairImage.color = isLookingAtInteractable
            ? crosshairInteractColor
            : crosshairDefaultColor;
    }
}