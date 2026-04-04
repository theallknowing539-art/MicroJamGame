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
    [SerializeField] private LayerMask enemyLayer;

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

    // --- BUFF REFERENCE ---
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

    // This finds the PlayerBuffs script anywhere on the Player object
    buffStats = GetComponentInParent<PlayerBuffs>();
    
    if (buffStats == null)
    {
        // If it's not in a parent, look for the object tagged "Player"
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) buffStats = player.GetComponent<PlayerBuffs>();
    }

    if (buffStats == null) Debug.LogError("Gun cannot find PlayerBuffs! Make sure your Player is tagged 'Player'.");
}

    private void Update()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        if (IsReloading) return;
        

        // 1. Block if Hangover is active
        if (DrunkManager.Instance != null && DrunkManager.Instance.IsHangover) return;

        // 2. Block if Player is Dead
        if (PlayerHealth.Instance != null && PlayerHealth.Instance.CurrentHealth <= 0) return;

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

    private void TryShoot()
    {
        if (CurrentAmmo <= 0)
        {
            if (_audioSource != null && _emptySound != null)
                _audioSource.PlayOneShot(_emptySound);

            if (ReserveAmmo > 0) StartCoroutine(ReloadRoutine());
            return;
        }

        // --- SHOOTING LOGIC (Raycast) ---
        ExecuteRaycastHit();

        WeaponSway sway = GetComponentInParent<WeaponSway>();
        if (sway != null) sway.ApplyRecoil();

        OnGunShoot?.Invoke();
        CurrentAmmo--;
        _currentCooldown = FireCooldown;

        if (_audioSource != null && _shootSound != null)
            _audioSource.PlayOneShot(_shootSound);

        OnAmmoChanged?.Invoke();
    }

   private void ExecuteRaycastHit()
{
    Ray ray = _mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
    RaycastHit hit;

    if (Physics.Raycast(ray, out hit, range))
    {
        Enemy enemy = hit.collider.GetComponent<Enemy>();
        if (enemy != null)
        {
            // MATH CHECK
            float baseDmg = baseGunDamage;
            float buffDmg = (buffStats != null) ? buffStats.attackDamage : 0f;
            float total = baseDmg + buffDmg;

            // PRINT TO CONSOLE: Look at this while playing!
            Debug.Log($"[GUN MATH] Base: {baseDmg} + Buff: {buffDmg} = TOTAL: {total}");

            enemy.TakeDamage(total);
        }
    }
}

    public void AddReserveAmmo(int amount)
    {
        ReserveAmmo += amount;
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