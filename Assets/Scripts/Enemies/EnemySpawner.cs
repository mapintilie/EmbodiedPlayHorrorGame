using System.Collections;
using UnityEngine;

/// <summary>
/// Spawns enemies at configured points and respawns them when they die.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Tooltip("Enemy prefab that contains the Enemy component.")]
    public GameObject enemyPrefab;

    [Tooltip("Where enemies can spawn.")]
    public Transform[] spawnPoints;

    [Tooltip("How many enemies to spawn at start.")]
    public int initialEnemyCount = 3;

    [Tooltip("Minimum time before respawning an enemy.")]
    public float respawnDelayMin = 1f;

    [Tooltip("Maximum time before respawning an enemy.")]
    public float respawnDelayMax = 5f;

    private void Start()
    {
        if (enemyPrefab == null || spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("EnemySpawner is not configured properly.", this);
            return;
        }

        for (int i = 0; i < initialEnemyCount; i++)
        {
            SpawnEnemy();
        }
    }

    public void NotifyEnemyDied(Enemy enemy)
    {
        // Spawn another after a short random delay.
        StartCoroutine(RespawnCoroutine());
    }

    private IEnumerator RespawnCoroutine()
    {
        float delay = Random.Range(respawnDelayMin, respawnDelayMax);
        yield return new WaitForSeconds(delay);

        SpawnEnemy();
    }

    private void SpawnEnemy()
    {
        if (enemyPrefab == null || spawnPoints == null || spawnPoints.Length == 0)
            return;

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject go = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);

        Enemy enemy = go.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.Spawner = this;
        }
        else
        {
            Debug.LogWarning("Spawned object does not have an Enemy component.", go);
        }
    }
}
