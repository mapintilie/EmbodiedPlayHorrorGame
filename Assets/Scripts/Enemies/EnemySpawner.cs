// csharp
using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public AudioSource spawnAudiosource;
    [Tooltip("Enemy prefab that contains the Enemy component.")]
    public GameObject enemyPrefab;
    [Tooltip("Fallback spawn points if no tagged spawners found.")]
    public Transform[] spawnPoints;
    [Tooltip("How many enemies to spawn at start.")]
    public int initialEnemyCount = 3;
    [Tooltip("Min respawn delay.")]
    public float respawnDelayMin = 1f;
    [Tooltip("Max respawn delay.")]
    public float respawnDelayMax = 5f;

    private void Start()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("EnemySpawner: enemyPrefab not set.", this);
            return;
        }

        for (int i = 0; i < initialEnemyCount; i++)
            SpawnEnemy();
    }

    public void NotifyEnemyDied(Enemy enemy)
    {
        // Zähle den Kill für Highscore
        if (GameOverManager.Instance != null)
            GameOverManager.Instance.OnEnemyDestroyed();

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
        if (enemyPrefab == null) return;

        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;

        // strikt: nur Punkte mit Tag "Spawners1" verwenden, fallback auf configured spawnPoints
        var spawners = GameObject.FindGameObjectsWithTag("Spawners1");
        if (spawners != null && spawners.Length > 0)
        {
            var pick = spawners[Random.Range(0, spawners.Length)];
            spawnPos = pick.transform.position;
            spawnRot = pick.transform.rotation;
        }
        else if (spawnPoints != null && spawnPoints.Length > 0)
        {
            var pick = spawnPoints[Random.Range(0, spawnPoints.Length)];
            spawnPos = pick.position;
            spawnRot = pick.rotation;
        }
        else
        {
            Debug.LogWarning("EnemySpawner: No spawn locations available.", this);
            return;
        }

        GameObject go = Instantiate(enemyPrefab, spawnPos, spawnRot);
        Enemy enemy = go.GetComponent<Enemy>();
        if (enemy != null)
            enemy.Spawner = this;
        else
            Debug.LogWarning("Spawned object does not have an Enemy component.", go);

        if (spawnAudiosource != null)
            spawnAudiosource.Play();
    }
}