using UnityEngine;
using UnityEngine.Events;
using System.Collections;

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

    [Header("Sound")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _shootSound;
    [SerializeField] private AudioClip _reloadSound;
    [SerializeField] private AudioClip _emptySound; 

    [Header("Animator")]
    [SerializeField] private Animator gunAnimator;

    private static readonly int AnimReload = Animator.StringToHash("Reload");

    public int CurrentAmmo  { get; private set; }
    public int ReserveAmmo  { get; private set; }
    public bool IsReloading { get; private set; } = false;

    public event System.Action OnAmmoChanged;

    private float _currentCooldown;

    private void Start()
    {
        CurrentAmmo = magazineSize;
        ReserveAmmo = reserveAmmo;
    }

    private void Update()
    {
        if (IsReloading) return;

        // 1. Block if Hangover is active
        if (DrunkManager.Instance != null && DrunkManager.Instance.IsHangover) return;

        // 2. NEW: Block if Player is Dead
        if (PlayerHealth.Instance != null && PlayerHealth.Instance.CurrentHealth <= 0) 
        {
            return; 
        }

        HandleShooting();
        HandleReloadInput();
    }

    private void HandleShooting()
    {
        if (_currentCooldown > 0)
        {
            _currentCooldown -= Time.deltaTime;
            return;
        }

        if (Automatic)
        {
            if (Input.GetMouseButton(0)) TryShoot();
        }
        else
        {
            if (Input.GetMouseButtonDown(0)) TryShoot();
        }
    }

    public void AddReserveAmmo(int amount)
    {
        ReserveAmmo += amount;
        OnAmmoChanged?.Invoke();
    }

    private void TryShoot()
    {
        if (CurrentAmmo <= 0)
        {
            if (_audioSource != null && _emptySound != null)
                _audioSource.PlayOneShot(_emptySound);

            StartCoroutine(ReloadRoutine());
            return;
        }

        WeaponSway sway = GetComponentInParent<WeaponSway>();
        if (sway != null) sway.ApplyRecoil();

        OnGunShoot?.Invoke();
        CurrentAmmo--;
        _currentCooldown = FireCooldown;

        if (_audioSource != null && _shootSound != null)
            _audioSource.PlayOneShot(_shootSound);

        OnAmmoChanged?.Invoke();
    }

    private void HandleReloadInput()
    {
        if (Input.GetKeyDown(KeyCode.R) && CurrentAmmo < magazineSize && ReserveAmmo > 0)
            StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        if (IsReloading || ReserveAmmo <= 0) yield break;

        IsReloading = true;

        if (gunAnimator != null)
            gunAnimator.SetTrigger(AnimReload);

        if (_audioSource != null && _reloadSound != null)
            _audioSource.PlayOneShot(_reloadSound);

        yield return new WaitForSeconds(reloadDuration);

        int bulletsNeeded = magazineSize - CurrentAmmo;
        int bulletsToLoad = Mathf.Min(bulletsNeeded, ReserveAmmo);

        CurrentAmmo += bulletsToLoad;
        ReserveAmmo -= bulletsToLoad;

        IsReloading = false;
        OnAmmoChanged?.Invoke();
    }
}