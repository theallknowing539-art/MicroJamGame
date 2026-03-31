// ================================================================
// Gun.cs — updated with reloading
// ================================================================
using UnityEngine;
using UnityEngine.Events;

public class Gun : MonoBehaviour
{
    [Header("Firing")]
    public UnityEvent OnGunShoot;
    public float FireCooldown;
    public bool Automatic;

    [Header("Ammo")]
    public int magazineSize = 6;
    public int reserveAmmo = 30;
    public float reloadDuration = 1.5f;

    [Header("Animator")]
    [SerializeField] private Animator gunAnimator;

    // animator parameter — must match your Animator exactly
    private static readonly int AnimReload = Animator.StringToHash("Reload");

    // ----------------------------------------------------------------
    // public so UI can read them
    public int CurrentAmmo   { get; private set; }
    public int ReserveAmmo   { get; private set; }
    public bool IsReloading  { get; private set; } = false;

    // events for UI
    public event System.Action OnAmmoChanged;

    private float _currentCooldown;

    // ----------------------------------------------------------------
    private void Start()
    {
        CurrentAmmo = magazineSize;
        ReserveAmmo = reserveAmmo;
    }

    // ----------------------------------------------------------------
    private void Update()
    {
        // block all gun input during hangover or reload
        if (IsReloading) return;
        if (DrunkManager.Instance != null && DrunkManager.Instance.IsHangover) return;

        HandleShooting();
        HandleReloadInput();
    }

    // ----------------------------------------------------------------
    private void HandleShooting()
    {
        if (_currentCooldown > 0)
        {
            _currentCooldown -= Time.deltaTime;
            return;
        }

        if (Automatic)
        {
            if (Input.GetMouseButton(0))
                TryShoot();
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
                TryShoot();
        }
    }

    // ----------------------------------------------------------------
    private void TryShoot()
    {
        if (CurrentAmmo <= 0)
        {
            // auto trigger reload if magazine is empty
            StartCoroutine(ReloadRoutine());
            return;
        }

        OnGunShoot?.Invoke();
        CurrentAmmo--;
        _currentCooldown = FireCooldown;
        OnAmmoChanged?.Invoke();
    }

    // ----------------------------------------------------------------
    private void HandleReloadInput()
    {
        // don't reload if magazine is already full or no reserve ammo
        if (Input.GetKeyDown(KeyCode.R) && CurrentAmmo < magazineSize && ReserveAmmo > 0)
            StartCoroutine(ReloadRoutine());
    }

    // ----------------------------------------------------------------
    private System.Collections.IEnumerator ReloadRoutine()
    {
        if (IsReloading || ReserveAmmo <= 0) yield break;

        IsReloading = true;

        // play reload animation
        if (gunAnimator != null)
            gunAnimator.SetTrigger(AnimReload);

        yield return new UnityEngine.WaitForSeconds(reloadDuration);

        // calculate how many bullets we need and how many reserve has
        int bulletsNeeded = magazineSize - CurrentAmmo;
        int bulletsToLoad = Mathf.Min(bulletsNeeded, ReserveAmmo);

        CurrentAmmo += bulletsToLoad;
        ReserveAmmo -= bulletsToLoad;

        IsReloading = false;
        OnAmmoChanged?.Invoke();
    }
}