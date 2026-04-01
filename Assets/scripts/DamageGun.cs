// ================================================================
// DamageGun.cs
// ================================================================
using UnityEngine;

public class DamageGun : MonoBehaviour
{
    [Header("Stats")]
    public float Damage;
    public float BulletRange;

    [Header("Weapon Sway")]
    [SerializeField] private WeaponSway weaponSway;

    private Transform _playerCamera;

    // ----------------------------------------------------------------
    private void Start()
    {
        _playerCamera = Camera.main.transform;
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
    }
}