using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : GazeInteractable
{
    [Header("Movement")]
    public float moveSpeed = 2.1f; 
    public float movementSpeedMultiplier = 0.5f;
    public Transform target;
    public Vector2 startDelayRange = new Vector2(1.5f, 3.0f);

    [Header("Respawn")]
    public EnemySpawner Spawner;

    [Header("Gaze")]
    public float freezeDuration = 0.5f; 

    [Header("Spawn tags")]
    public string spawnTagStage1 = "Spawners1";
    public string spawnTagStage2 = "Spawners2";
    public string spawnTagStage3 = "Spawners3";

    [Header("Visual Poses")]
    [Tooltip("Ziehe hier die 3 Engel-Kindobjekte in der richtigen Reihenfolge rein (0=Start, 1=1xHit, 2=2xHit)")]
    public GameObject[] angelPoses;

    private bool canMove;
    private bool isFrozen;
    private bool canBeLookedAt = true;
    private int gazeHits = 0;
    private bool isDying = false;

    private Renderer[] cachedRenderers;
    private string currentSpawnName = null;
    private NavMeshAgent agent;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = moveSpeed * movementSpeedMultiplier;
            agent.angularSpeed = 720f;
            agent.acceleration = 8f;
            if (Enum.TryParse("MediumQuality", true, out ObstacleAvoidanceType chosen)) agent.obstacleAvoidanceType = chosen;
            else agent.obstacleAvoidanceType = (ObstacleAvoidanceType)2;
            agent.updateRotation = true;
        }

        // Setze sofort die allererste Pose (0 Hits)
        UpdateVisualPose(0);

        StartCoroutine(EnableMovementAfterDelay());
        GazeCameraController.OnRoomChanged += OnRoomChanged;
    }

    private void OnDestroy()
    {
        GazeCameraController.OnRoomChanged -= OnRoomChanged;
    }

    private void OnRoomChanged()
    {
        canBeLookedAt = true;
    }

    protected override void Update()
    {
        base.Update();
        
        // Anti-Geister-Sicherung
        if (transform.position.y < -50f)
        {
            Destroy(gameObject);
            return;
        }

        if (agent != null && agent.isOnNavMesh)
        {
            agent.speed = moveSpeed * movementSpeedMultiplier;
            bool shouldMove = canMove && !isFrozen && !isDying;
            agent.isStopped = !shouldMove;

            if (shouldMove)
            {
                Transform moveTarget = GetMoveTarget();
                if (moveTarget != null && (!agent.hasPath || Vector3.Distance(agent.destination, moveTarget.position) > 0.5f))
                {
                    agent.SetDestination(moveTarget.position);
                }
            }
        }
    }

    protected override void OnGazeFocusedCallback()
    {
        if (!canBeLookedAt || isDying || isFrozen) return;

        canBeLookedAt = false;
        gazeHits++;
        
        // Aktualisiere sofort die Pose (das Modell)
        UpdateVisualPose(gazeHits);

        if (gazeHits >= 3) StartCoroutine(DieSequence());
        else StartCoroutine(FreezeAndTeleportSequence());
    }

    protected override void OnGazeExitCallback() { } 

    private IEnumerator FreezeAndTeleportSequence()
    {
        isFrozen = true;
        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;

        yield return new WaitForSeconds(freezeDuration);

        if (gazeHits == 1)
        {
            GameObject picked = TeleportToValidRoom(new string[] { spawnTagStage2 });
            if (picked != null) currentSpawnName = picked.name;
        }
        else if (gazeHits == 2)
        {
            GameObject picked = TeleportToValidRoom(new string[] { spawnTagStage3 });
            if (picked != null)
            {
                currentSpawnName = picked.name;
                if (picked.CompareTag(spawnTagStage3)) target = Camera.main != null ? Camera.main.transform : target;
            }
        }

        isFrozen = false;
        if (agent != null && agent.isOnNavMesh) agent.isStopped = false;

        yield return new WaitForSeconds(1f);
        canBeLookedAt = true;
    }

    private IEnumerator EnableMovementAfterDelay()
    {
        float delay = Random.Range(startDelayRange.x, startDelayRange.y);
        yield return new WaitForSeconds(delay);
        canMove = true;
    }

    private IEnumerator DieSequence()
    {
        isFrozen = true;
        isDying = true;
        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;

        yield return new WaitForSeconds(freezeDuration);

        // Die schwarze Todesschattierung funktioniert weiterhin auf dem aktuell aktiven Modell!
        if (cachedRenderers != null)
        {
            foreach (var r in cachedRenderers)
                if (r != null) r.material.color = Color.black;
        }

        yield return new WaitForSeconds(0.2f);

        Spawner?.NotifyEnemyDied(this);
        Destroy(gameObject);
    }

    private Transform GetMoveTarget()
    {
        if (target != null) return target;
        if (Camera.main != null) return Camera.main.transform;
        return null;
    }

    private GameObject TeleportToValidRoom(string[] tags)
    {
        if (tags == null || tags.Length == 0) return null;

        var allCandidates = new List<GameObject>();
        foreach (var t in tags)
        {
            var gos = GameObject.FindGameObjectsWithTag(t);
            if (gos != null) allCandidates.AddRange(gos);
        }

        Transform playerTr = Camera.main != null ? Camera.main.transform : null;
        var allEnemies = FindObjectsOfType<Enemy>();
        List<GameObject> available = new List<GameObject>();

        foreach (var g in allCandidates)
        {
            if (g == null || g.name == currentSpawnName) continue;

            if (playerTr != null)
            {
                Vector3 toCandidate = (g.transform.position - playerTr.position);
                toCandidate.y = 0f;

                Vector3 toCurrent = (transform.position - playerTr.position);
                toCurrent.y = 0f;
                if (Vector3.Angle(toCandidate, toCurrent) < 75f) continue;

                bool occupiedByOther = false;
                foreach (var e in allEnemies)
                {
                    if (e == null || e == this) continue;
                    Vector3 toOther = (e.transform.position - playerTr.position);
                    toOther.y = 0f;

                    if (Vector3.Angle(toCandidate, toOther) < 75f)
                    {
                        occupiedByOther = true;
                        break;
                    }
                }
                
                if (occupiedByOther) continue;
            }

            available.Add(g);
        }

        if (available.Count == 0)
        {
            foreach (var g in allCandidates)
            {
                if (g != null && g.name != currentSpawnName) available.Add(g);
            }
        }

        if (available.Count == 0) return null;

        var pick = available[Random.Range(0, available.Count)];

        if (agent != null && agent.isOnNavMesh)
        {
            agent.Warp(pick.transform.position);
            transform.rotation = pick.transform.rotation;
            agent.ResetPath();
        }
        else
        {
            transform.position = pick.transform.position;
            transform.rotation = pick.transform.rotation;
        }

        return pick;
    }

    // ==========================================
    // DAS NEUE MODEL-SWAP SYSTEM
    // ==========================================
    private void UpdateVisualPose(int hits)
    {
        if (angelPoses == null || angelPoses.Length == 0) return;

        // 1. Schalte ALLE Posen erstmal unsichtbar
        foreach (var pose in angelPoses)
        {
            if (pose != null) pose.SetActive(false);
        }

        // 2. Finde die richtige Pose heraus (0 Hits = Index 0, 1 Hit = Index 1, 2 Hits = Index 2)
        int index = Mathf.Clamp(hits, 0, angelPoses.Length - 1);
        
        // 3. Schalte nur die richtige Pose sichtbar
        if (angelPoses[index] != null)
        {
            angelPoses[index].SetActive(true);
        }

        // 4. Lese die Renderer neu ein, damit der Gegner beim Tod immer noch schwarz werden kann
        UpdateCachedRenderers();
    }
    
    private void UpdateCachedRenderers() 
    { 
        // Lese nur die Renderer von Objekten ein, die gerade "an" sind
        cachedRenderers = GetComponentsInChildren<Renderer>(false); 
    }
}