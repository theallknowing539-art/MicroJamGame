using UnityEngine;
using UnityEngine.Events;
public class Gun : MonoBehaviour
{
    //this is to say when shoot , damageGun fires
    public UnityEvent OnGunShoot;

    //The time between each shoot
    public float FireCooldown;

    //true : the gun continues to fire , false : the gun fires only one time when we click
    public bool Automatic;

    //set speed between each shoot 
    private float CurrentCooldown;

    void Update()
    {
        if (Automatic)
        {
            //left mouse button is pressed?
            if (Input.GetMouseButton(0))
            {
                //to ensure that we can only shoot when the cooldown is finished
                if (CurrentCooldown <= 0)
                {
                    OnGunShoot?.Invoke(); //the unity event
                    CurrentCooldown = FireCooldown; // reset the cooldown
                }
            }
        }else
        {
            // press down in this frame ?
            if (Input.GetMouseButtonDown(0))
            {
                if (CurrentCooldown <= 0)
                {
                    OnGunShoot?.Invoke(); //the unity event
                    CurrentCooldown = FireCooldown; // reset the cooldown
                }
            }

        }
        //reduce the cooldown timer 
        if (CurrentCooldown > 0)
        {
            CurrentCooldown -= Time.deltaTime;
        

    }

}
}
