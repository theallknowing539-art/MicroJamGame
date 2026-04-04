using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    private PlayerBuffs buffStats;

    void Start()
    {
        buffStats = GetComponent<PlayerBuffs>();
    }

    // If you don't have an EnemyHealth script yet, 
    // comment this section out with /* */ to stop the error
    public void DealDamage(GameObject enemy)
    {
        if (buffStats != null)
        {
            float damageToDeal = buffStats.attackDamage;
            Debug.Log("Dealt " + damageToDeal + " damage!");
            // enemy.GetComponent<EnemyHealth>().TakeDamage(damageToDeal);
        }
    }
}