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
    
    private float gameStartTimer;
    private bool gameHasStarted = false;
    private float spawnTimer = 0f;

    private int totalKills = 0;
    private int enemiesUntilNextBonus = 5; 

    // --- DIAGNOSTIC VARIABLE ---
    private string lastError = "Waiting to start...";

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

        // THE GHOST KILLER: 
        // This now strictly removes enemies if they are destroyed OR if another script disabled them!
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
            // Keep timer frozen so it doesn't instantly spawn when an enemy dies
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
        if (enemyPrefab == null) { lastError = "ERROR: No Prefab assigned!"; return; }
        if (activeEnemies.Count >= GetCurrentAllowedMax()) { lastError = "Limit Reached"; return; }

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
                activeEnemies.Add(spawned);
                lastError = "Spawn Successful";
            }
            else
            {
                lastError = "ERROR: Prefab is missing the Enemy script!";
            }

            if (spawnAudiosource != null) spawnAudiosource.Play();
        }
        else
        {
            lastError = "ERROR: Absolutely no spawn points found!";
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

    // ==========================================
    // THE X-RAY OVERLAY
    // ==========================================
    private void OnGUI()
    {
        GUI.Box(new Rect(10, 10, 450, 250), "");
        GUILayout.BeginArea(new Rect(20, 20, 430, 230));
        
        GUILayout.Label($"<color=yellow><size=16><b>SPAWNER DIAGNOSTICS</b></size></color>");
        GUILayout.Label($"Game Started: {gameHasStarted}");
        GUILayout.Label($"Total Kills: {totalKills}");
        GUILayout.Label($"Active Limit: {GetCurrentAllowedMax()} (Next 3-Enemy wave in {enemiesUntilNextBonus} kills)");
        GUILayout.Label($"Timer until next spawn: {Mathf.Max(0, spawnTimer):F1}s");
        GUILayout.Label($"Last Spawner Status: <color=cyan>{lastError}</color>");
        GUILayout.Space(10);
        
        GUILayout.Label($"<b>Active Enemies in memory: {activeEnemies.Count}</b>");
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            if (activeEnemies[i] != null)
            {
                Vector3 pos = activeEnemies[i].transform.position;
                GUILayout.Label($"- Enemy {i}: Pos({pos.x:F1}, {pos.y:F1}, {pos.z:F1}) | Active: {activeEnemies[i].gameObject.activeInHierarchy}");
            }
        }
        
        GUILayout.EndArea();
    }
}