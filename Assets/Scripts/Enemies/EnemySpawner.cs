using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public AudioSource spawnAudiosource;
    public GameObject enemyPrefab;
    [Tooltip("Falls keine Tags gefunden werden, nutzt er diese Punkte")]
    public Transform[] fallbackSpawnPoints; 
    
    [Header("Pacing & Limits")]
    public float initialStartDelay = 4f; 
    public int maxConcurrentEnemies = 3;
    
    public float easyRespawnDelayMin = 3f;
    public float easyRespawnDelayMax = 6f;

    public float hardRespawnDelayMin = 1.5f;
    public float hardRespawnDelayMax = 3.5f;

    private List<Enemy> activeEnemies = new List<Enemy>();
    
    // Kugelsichere Timer (statt Coroutines)
    private float gameStartTimer;
    private bool gameHasStarted = false;
    private float spawnTimer = 0f;

    private int totalKills = 0;
    private int enemiesUntilNextBonus = 5; 

    private void Start()
    {
        gameStartTimer = initialStartDelay;
    }

    private void Update()
    {
        // 1. Warte die Startphase ab
        if (!gameHasStarted)
        {
            gameStartTimer -= Time.deltaTime;
            if (gameStartTimer <= 0f)
            {
                gameHasStarted = true;
                SpawnEnemy(); // Der allererste Gegner
                SetNextSpawnDelay();
            }
            return;
        }

        // 2. Brutales Bereinigen der Liste (falls Gegner gelöscht wurden)
        activeEnemies.RemoveAll(item => item == null || item.gameObject == null);

        // 3. Spawnen (nur über Timer, kann nicht "stecken bleiben")
        if (activeEnemies.Count < GetCurrentAllowedMax())
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                SpawnEnemy();
                SetNextSpawnDelay();
            }
        }
        else
        {
            // Halte den Timer oben, falls die Karte voll ist. 
            // So spawnt nicht instant einer, wenn ein Gegner stirbt.
            SetNextSpawnDelay();
        }
    }

    public void NotifyEnemyDied(Enemy enemy)
    {
        if (GameOverManager.Instance != null)
            GameOverManager.Instance.OnEnemyDestroyed();

        totalKills++;
        enemiesUntilNextBonus--;

        if (enemiesUntilNextBonus < 0)
        {
            enemiesUntilNextBonus = Random.Range(5, 11); 
        }
    }

    private int GetCurrentAllowedMax()
    {
        if (totalKills < 2) return 1;
        
        int allowedMax = 2;
        if (enemiesUntilNextBonus == 0) allowedMax = 3;

        return Mathf.Clamp(allowedMax, 1, maxConcurrentEnemies);
    }

    private void SetNextSpawnDelay()
    {
        spawnTimer = (totalKills < 3) 
            ? Random.Range(easyRespawnDelayMin, easyRespawnDelayMax) 
            : Random.Range(hardRespawnDelayMin, hardRespawnDelayMax);
    }

    private void SpawnEnemy()
    {
        if (enemyPrefab == null) return;
        if (activeEnemies.Count >= GetCurrentAllowedMax()) return;

        var spawners = GameObject.FindGameObjectsWithTag("Spawners1");
        List<Transform> validSpawners = new List<Transform>();

        if (spawners != null)
        {
            foreach (var sp in spawners)
            {
                if (IsRoomFree(sp.transform.position))
                    validSpawners.Add(sp.transform);
            }
        }

        // Fallback 1: Wenn alle Räume als "voll" gelten, nimm einfach alle möglichen Spawner
        if (validSpawners.Count == 0 && spawners != null && spawners.Length > 0) 
        {
            foreach(var sp in spawners) validSpawners.Add(sp.transform);
        }

        // Fallback 2: Wenn es die Tags gar nicht gibt, nutze die manuellen Punkte
        if (validSpawners.Count == 0 && fallbackSpawnPoints != null && fallbackSpawnPoints.Length > 0)
        {
            validSpawners.AddRange(fallbackSpawnPoints);
        }

        if (validSpawners.Count > 0)
        {
            var pick = validSpawners[Random.Range(0, validSpawners.Count)];
            GameObject go = Instantiate(enemyPrefab, pick.position, pick.rotation);
            Enemy spawned = go.GetComponent<Enemy>();
            
            if (spawned != null)
            {
                spawned.Spawner = this;
                activeEnemies.Add(spawned);
            }

            if (spawnAudiosource != null) spawnAudiosource.Play();
        }
    }

    private bool IsRoomFree(Vector3 candidatePos)
    {
        Transform playerTr = Camera.main != null ? Camera.main.transform : null;
        if (playerTr == null) return true;

        Vector3 toCandidate = (candidatePos - playerTr.position);
        toCandidate.y = 0f;

        foreach (var e in activeEnemies)
        {
            if (e == null) continue;
            Vector3 toExisting = (e.transform.position - playerTr.position);
            toExisting.y = 0f;

            if (Vector3.Angle(toCandidate, toExisting) < 75f) 
                return false; 
        }
        return true;
    }
}