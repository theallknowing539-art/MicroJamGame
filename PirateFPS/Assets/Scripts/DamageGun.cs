using UnityEngine;

public class DamageGun : MonoBehaviour
{
    //how much damage the gun does 
    public float Damage;

    //how far the bullet can go 
    public float BulletRange;

    //reference to the player camera to know where we are shooting from
    private Transform PlayerCamera;

    void Start()
    {
        PlayerCamera = Camera.main.transform; //get the main camera transform
    }

    public void Shoot()
    {
        //this create a ray from the camera position in the forward direction
        Ray gunRay = new Ray(PlayerCamera.position, PlayerCamera.forward);
        
        //we check if the ray hits something within the bullet range
        if (Physics.Raycast(gunRay, out RaycastHit hitInfo, BulletRange))
        {
            if(hitInfo.collider.gameObject.TryGetComponent(out Entity entity))
            {
               entity.health -= Damage; //reduce the health of the target by the damage amount
            }
        }
    }   
}
