using UnityEngine;

public class Entity : MonoBehaviour
{
    [SerializeField]
    private float StartingHealth;//the initial health of the entity
    
    private float health;//current health of the entity

    public float Health
    {
        get { return health; }
        set
        {
            health = value;
            Debug.Log($"health: {health}");
            if (health <= 0)
            {
                Destroy(gameObject); //destroy the entity when health is 0 or less
            }
        }
    }

    void Start()
    {
        Health = StartingHealth; //initialize health to the starting health at the beginning
    }
}
