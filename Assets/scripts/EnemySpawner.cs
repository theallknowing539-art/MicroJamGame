using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    [Header("Setup")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform[] spawnPoints;   // place these on the hull edges in the scene

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public IEnumerator SpawnWave(int count, float interval)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(interval);
        }
    }

    private void SpawnEnemy()
    {
        if (spawnPoints.Length == 0) return;

        // pick a random spawn point from the array
        Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Instantiate(enemyPrefab, point.position, point.rotation);
    }
}