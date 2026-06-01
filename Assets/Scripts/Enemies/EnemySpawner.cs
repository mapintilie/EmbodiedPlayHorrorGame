using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Audio")]
    [Tooltip("Optional audio clip played when an enemy spawns. Played at spawn position.")]
    public AudioSource spawnSound;

    
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
    
    private float gameStartTimer;
    private bool gameHasStarted = false;
    private float spawnTimer = 0f;

    private int totalKills = 0;
    private int enemiesUntilNextBonus = 5; 
    
    // NEU: Speichert das aktuell ausgewürfelte Limit für normale Runden
    private int currentNormalCap = 2; 

    private void Start()
    {
        gameStartTimer = initialStartDelay;
    }

    private void Update()
    {
        if (!gameHasStarted)
        {
            gameStartTimer -= Time.deltaTime;
            if (gameStartTimer <= 0f)
            {
                gameHasStarted = true;
                SpawnEnemy(); 
                SetNextSpawnDelay();
            }
            return;
        }

        activeEnemies.RemoveAll(item => item == null || item.gameObject == null || !item.gameObject.activeInHierarchy);

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

        // NEU: Nach jedem Kill würfeln wir neu aus, ob die nächste "Phase" entspannt (1) oder schwer (2) wird.
        // 40% Chance auf nur 1 Gegner. 60% Chance auf 2 Gegner.
        if (Random.value < 0.40f)
        {
            currentNormalCap = 1;
        }
        else
        {
            currentNormalCap = 2;
        }
    }

    private int GetCurrentAllowedMax()
    {
        if (totalKills < 2) return 1;
        
        // "unless there is already 3" -> Bonuswelle (3) wird NIEMALS von der 40%-Chance überschrieben!
        if (enemiesUntilNextBonus == 0) return 3; 

        // Ansonsten geben wir das Limit zurück, das beim letzten Kill ausgewürfelt wurde
        return Mathf.Clamp(currentNormalCap, 1, maxConcurrentEnemies);
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

        if (validSpawners.Count == 0 && spawners != null && spawners.Length > 0) 
        {
            foreach(var sp in spawners) validSpawners.Add(sp.transform);
        }

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
                spawned.spawnSound = spawnSound;  // Assign the audio source to the enemy
                
                activeEnemies.Add(spawned);
            }

            // Play spawn sound at the chosen spawn point (if provided)
            if (spawnSound != null)
            {
                spawnSound.Play();
            }
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