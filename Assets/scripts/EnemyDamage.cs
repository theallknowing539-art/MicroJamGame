using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDamage : MonoBehaviour
{

    public float DamageAmount = 10f; //demage done by enemy to player

    public float DamageCooldown = 1f; //how often the enemy can damage the player 
    
    private float currentCooldown; //timer
    

    void Update()
    {
        if (currentCooldown > 0f)
        {
            currentCooldown -= Time.deltaTime; //reduce cooldown timer
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (currentCooldown <= 0f)
            {
                PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(DamageAmount);
                    currentCooldown = DamageCooldown;
                }
            }
        }
    }
}
