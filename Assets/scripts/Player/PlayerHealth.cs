using UnityEngine;
using System;
public class PlayerHealth : MonoBehaviour
{

    public float MaxHealth = 100f;// The maximum health of the player

    private float currentHealth; // The current health of the player

    public event Action<float> OnHealthChanged; // Event to notify when health changes

    public event Action OnPlayerDied;// Event to notify when the player dies


    void Start()
    {
        currentHealth = MaxHealth; // Initialize current health to maximum health at the start
    }


    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount; // Decrease current health by the specified amount
        currentHealth = Mathf.Clamp(currentHealth, 0f, MaxHealth); //ensure health never goes below 0
        OnHealthChanged?.Invoke(currentHealth); // Notify listeners about the health change
        Debug.Log("Player health: " + currentHealth);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        currentHealth += amount; // Increase current health by the specified amount
        currentHealth = Mathf.Clamp(currentHealth, 0f, MaxHealth); // Ensure health does not exceed maximum
        OnHealthChanged?.Invoke(currentHealth); // Notify listeners about the health change
        Debug.Log("Player health: " + currentHealth);
    }

    private void Die()
    {
        OnPlayerDied?.Invoke();// player died
        Debug.Log("Player died!");
        // For now just disable the player
        gameObject.SetActive(false);
    }
}
