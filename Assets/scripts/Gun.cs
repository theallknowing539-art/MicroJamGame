using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using UnityEngine.EventSystems;

public class Gun : MonoBehaviour
{
    [Header("Firing")]
    public UnityEvent OnGunShoot;
    public float FireCooldown = 0.2f;
    public bool Automatic;
    [SerializeField] private float baseGunDamage = 10f;
    [SerializeField] private float range = 100f;

    [Header("Ammo")]
    public int magazineSize = 6;
    public int reserveAmmo = 30;
    public float reloadDuration = 1.5f;

    [Header("References")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _shootSound;
    [SerializeField] private AudioClip _emptySound; 
    [SerializeField] private Animator gunAnimator;

    private PlayerBuffs buffStats;
    private static readonly int AnimReload = Animator.StringToHash("Reload");

    public int CurrentAmmo  { get; private set; }
    public int ReserveAmmo  { get; private set; }
    public bool IsReloading { get; private set; } = false;

    public event System.Action OnAmmoChanged;
    private float _currentCooldown;
    private Camera _mainCam;

    private void Start()
    {
        CurrentAmmo = magazineSize;
        ReserveAmmo = reserveAmmo;
        _mainCam = Camera.main;
        buffStats = GetComponentInParent<PlayerBuffs>();
        if (buffStats == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) buffStats = player.GetComponent<PlayerBuffs>();
        }
    }

    private void Update()
    {
        if (EventSystem.current.IsPointerOverGameObject() || IsReloading) return;
        if (DrunkManager.Instance != null && DrunkManager.Instance.IsHangover) return;
        if (PlayerHealth.Instance != null && PlayerHealth.Instance.CurrentHealth <= 0) return;

        HandleShooting();
        if (Input.GetKeyDown(KeyCode.R) && CurrentAmmo < magazineSize && ReserveAmmo > 0)
            StartCoroutine(ReloadRoutine());
    }

    private void HandleShooting()
    {
        if (_currentCooldown > 0) { _currentCooldown -= Time.deltaTime; return; }

        if (Automatic ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0)) TryShoot();
    }

    private void TryShoot()
    {
        if (CurrentAmmo <= 0)
        {
            if (_audioSource != null && _emptySound != null) _audioSource.PlayOneShot(_emptySound);
            if (ReserveAmmo > 0) StartCoroutine(ReloadRoutine());
            return;
        }

        ExecuteRaycastHit();
        CurrentAmmo--;
        _currentCooldown = FireCooldown;
        OnGunShoot?.Invoke();
        OnAmmoChanged?.Invoke();
        if (_audioSource != null && _shootSound != null) _audioSource.PlayOneShot(_shootSound);
    }

    private void ExecuteRaycastHit()
    {
        Ray ray = _mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            Enemy enemy = hit.collider.GetComponent<Enemy>();
            if (enemy != null)
            {
                float totalDmg = baseGunDamage + (buffStats != null ? buffStats.attackDamage : 0f);
                enemy.TakeDamage(totalDmg);

                // --- KRAKEN'S BELCH LOGIC ---
                if (buffStats != null && buffStats.knockbackForce > 0)
                {
                    Vector3 shotDir = (hit.point - transform.position).normalized;
                    shotDir.y = 0; // Keep push horizontal
                    enemy.ApplyKnockback(shotDir * buffStats.knockbackForce);
                }
            }
        }
    }

    private IEnumerator ReloadRoutine()
    {
        IsReloading = true;
        if (gunAnimator != null) gunAnimator.SetTrigger(AnimReload);
        yield return new WaitForSeconds(reloadDuration);
        int toLoad = Mathf.Min(magazineSize - CurrentAmmo, ReserveAmmo);
        CurrentAmmo += toLoad;
        ReserveAmmo -= toLoad;
        IsReloading = false;
        OnAmmoChanged?.Invoke();
    }

    public void AddReserveAmmo(int amount) { ReserveAmmo += amount; OnAmmoChanged?.Invoke(); }
}