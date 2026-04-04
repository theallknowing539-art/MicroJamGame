// ================================================================
// DamageGun.cs
// ================================================================
using UnityEngine;

public class DamageGun : MonoBehaviour
{
    [Header("Stats")]
    public float Damage;
    public float BulletRange;
    private float _baseDamage;

    [Header("Weapon Sway")]
    [SerializeField] private WeaponSway weaponSway;

    private Transform _playerCamera;

    // ----------------------------------------------------------------
    private void Start()
    {
        _playerCamera = Camera.main.transform;
        _baseDamage = Damage;

    }
    public void SetDamageMultiplier(float multiplier)
{
    Damage = _baseDamage * multiplier;
}

    // ----------------------------------------------------------------
    public void Shoot()
    {
        Ray gunRay = new Ray(_playerCamera.position, _playerCamera.forward);

        if (Physics.Raycast(gunRay, out RaycastHit hitInfo, BulletRange))
        {
            Enemy enemy = hitInfo.collider.GetComponent<Enemy>();
            if (enemy != null)
                enemy.TakeDamage(Damage);
        }

        if (weaponSway != null)
            weaponSway.ApplyRecoil();

        if (CameraShake.Instance != null)
        {
        // Inside your Fire() function
            CameraShake.Instance.Shake(0.15f, 0.05f);
        }
    }

    void Update()
{
    // If the PlayerHealth script says we are dead, don't shoot!
    if (PlayerHealth.Instance != null && PlayerHealth.Instance.IsDead) return;

    if (Input.GetButtonDown("Fire1"))
    {
        Shoot();
    }
}
}