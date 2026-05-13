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
    [Tooltip("Absolute maximum concurrent enemies allowed (hard cap).")]
    public int maxConcurrentEnemies = 3;
    [Tooltip("Min respawn delay.")]
    public float respawnDelayMin = 1f;
    [Tooltip("Max respawn delay.")]
    public float respawnDelayMax = 5f;

    [Header("Spawn tuning")]
    [Tooltip("Während der Anfangsphase (vor erstem Kill) ist das Limit so niedrig.")]
    public int initialPhaseMax = 1;
    [Tooltip("Standard-Limit nach dem ersten Kill.")]
    public int normalMaxAfterFirstKill = 2;
    [Tooltip("Chance (0..1), dass nach dem ersten Kill auch 3 erlaubt werden.")]
    public float rareThirdChance = 0.12f;

    private int currentActiveEnemies = 0;

    // intern: wurde bereits mindestens ein Enemy getötet?
    private bool firstKillOccurred = false;
    // intern: dynamisch ermittelte erlaubte Max-Anzahl nach erstem Kill (geclamp't)
    private int dynamicMaxConcurrent = -1;

    private void Start()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("EnemySpawner: enemyPrefab not set.", this);
            return;
        }

        int spawnCount = Mathf.Clamp(initialEnemyCount, 0, GetCurrentAllowedMax());
        for (int i = 0; i < spawnCount; i++)
            SpawnEnemy();
    }

    public void NotifyEnemyDied(Enemy enemy)
    {
        // Zähle den Kill für Highscore
        if (GameOverManager.Instance != null)
            GameOverManager.Instance.OnEnemyDestroyed();

        // Markiere erstes Mal Kill und bestimme danach das neue Limit
        if (!firstKillOccurred)
        {
            firstKillOccurred = true;
            int chosen = normalMaxAfterFirstKill;
            if (Random.value < rareThirdChance)
                chosen = Mathf.Max(chosen, 3);
            // clamp gegen harten Max-Wert
            dynamicMaxConcurrent = Mathf.Clamp(chosen, 1, Mathf.Max(1, maxConcurrentEnemies));
        }

        // Ein Enemy ist tot, Platzzähler verringern
        currentActiveEnemies = Mathf.Max(0, currentActiveEnemies - 1);

        // Wenn jetzt keine Enemies mehr vorhanden sind, sofort einen neuen spawnen
        if (currentActiveEnemies == 0)
        {
            if (GetCurrentAllowedMax() > 0)
                SpawnEnemy();
            return;
        }

        // Starte Respawn nur wenn noch Platz ist (und es nicht der letzte war)
        if (currentActiveEnemies < GetCurrentAllowedMax())
            StartCoroutine(RespawnCoroutine());
    }

    private IEnumerator RespawnCoroutine()
    {
        float delay = Random.Range(respawnDelayMin, respawnDelayMax);
        yield return new WaitForSeconds(delay);

        if (currentActiveEnemies < GetCurrentAllowedMax())
            SpawnEnemy();
    }

    private void SpawnEnemy()
    {
        if (enemyPrefab == null) return;

        // respektiere dynamisches Limit
        if (currentActiveEnemies >= GetCurrentAllowedMax()) return;

        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;

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
        Enemy spawned = go.GetComponent<Enemy>();
        if (spawned != null)
            spawned.Spawner = this;
        else
            Debug.LogWarning("Spawned object does not have an Enemy component.", go);

        currentActiveEnemies++;

        if (spawnAudiosource != null)
            spawnAudiosource.Play();
    }

    private int GetCurrentAllowedMax()
    {
        if (!firstKillOccurred)
            return Mathf.Clamp(initialPhaseMax, 1, Mathf.Max(1, maxConcurrentEnemies));

        if (dynamicMaxConcurrent > 0)
            return Mathf.Clamp(dynamicMaxConcurrent, 1, Mathf.Max(1, maxConcurrentEnemies));

        // fallback
        return Mathf.Clamp(normalMaxAfterFirstKill, 1, Mathf.Max(1, maxConcurrentEnemies));
    }
}