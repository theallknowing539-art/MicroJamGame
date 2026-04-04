using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private PlayerBuffs buffStats;

    void Start()
    {
        buffStats = GetComponent<PlayerBuffs>();
    }

    void Update()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 move = transform.right * moveX + transform.forward * moveZ;

        // Use the buffed speed from our PlayerBuffs script
        if (buffStats != null)
        {
            transform.Translate(move * buffStats.moveSpeed * Time.deltaTime);
        }
    }
}