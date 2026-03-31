// ================================================================
// DamageGun.cs — updated
// ================================================================
using UnityEngine;

public class DamageGun : MonoBehaviour
{
    public float Damage;
    public float BulletRange;

    [Header("Weapon Sway")]
    [SerializeField] private WeaponSway weaponSway;  // drag WeaponHolder here

    private Transform PlayerCamera;

    void Start()
    {
        PlayerCamera = Camera.main.transform;
    }

    public void Shoot()
    {
        Ray gunRay = new Ray(PlayerCamera.position, PlayerCamera.forward);

        if (Physics.Raycast(gunRay, out RaycastHit hitInfo, BulletRange))
        {
            // works with your Enemy.cs TakeDamage method
            Enemy enemy = hitInfo.collider.GetComponent<Enemy>();
            if (enemy != null)
                enemy.TakeDamage(Damage);
        }

        // trigger recoil on the weapon sway system
        if (weaponSway != null)
            weaponSway.ApplyRecoil();
    }
}
