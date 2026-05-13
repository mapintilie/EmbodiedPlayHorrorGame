using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Enemy : GazeInteractable
{
    [Header("Movement")]
    public float moveSpeed = 2.5f;
    [Tooltip("Globaler Multiplikator für die eigentliche Movement-Geschwindigkeit (z.B. 0.25 = Viertel der Basisgeschwindigkeit)")]
    public float movementSpeedMultiplier = 0.5f;

    public Transform target;
    public Vector2 startDelayRange = new Vector2(0f, 2f);

    [Header("Respawn")]
    public EnemySpawner Spawner;

    [Header("Gaze")]
    [SerializeField] private float resumeDelay = 10f;

    [Header("Spawn tags")]
    public string spawnTagStage1 = "Spawners1";
    public string spawnTagStage2 = "Spawners2";
    public string spawnTagStage3 = "Spawners3";

    [Header("Cylinder (optional)")]
    public Renderer cylinderRenderer;

    [Header("Obstacle avoidance")]
    public LayerMask obstacleLayer;
    public float obstacleCheckRadius = 0.3f;

    [Header("Spawn Indicator")]
    public bool showSpawnIndicatorOnSpawn = true;
    public float spawnIndicatorDuration = 2f;

    // State
    private bool canMove;
    private bool isStopped;
    private bool canBeLookedAt = true;
    private int gazeHits = 0;
    private bool stoppedByGaze = false;
    private bool isDying = false;
    private bool spawnedAtStage3 = false;

    private Renderer[] cachedRenderers;
    private GameObject currentVisual = null;

    private Coroutine resumeCoroutine;
    // Name des aktuell gesetzten Spawn-Punkts (z.B. "1", "2", ...)
    private string currentSpawnName = null;

    private void Start()
    {
        if (obstacleLayer.value == 0)
        {
            int lay = LayerMask.NameToLayer("Layout (1)");
            if (lay >= 0) obstacleLayer = 1 << lay;
        }

        StartCoroutine(EnableMovementAfterDelay());

        TryFindCylinderRenderer();
        SetupInitialVisual();

        // Sicherstellen: beim Spawn immer auf einen random Punkt aus Spawners1 teleportieren
        var initial = TeleportToRandomWithTagsReturn(new string[] { spawnTagStage1 }, excludeSameName: false);
        if (initial != null)
            currentSpawnName = initial.name;

        if (showSpawnIndicatorOnSpawn)
        {
            var indicator = FindObjectOfType<SpawnIndicatorUI>();
            if (indicator != null)
                indicator.ShowAtWorldPosition(transform.position, spawnIndicatorDuration);
        }

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

        if (!canMove || isStopped || isDying) return;

        Transform moveTarget = GetMoveTarget();
        if (moveTarget == null) return;

        Vector3 direction = (moveTarget.position - transform.position);
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f) return;

        Vector3 desiredDir = direction.normalized;
        float dt = Time.deltaTime;

        float currentMoveSpeed = moveSpeed * movementSpeedMultiplier;
        Vector3 desiredMove = desiredDir * currentMoveSpeed * dt;

        bool blocked = Physics.SphereCast(transform.position, obstacleCheckRadius, desiredDir, out RaycastHit hitInfo, desiredMove.magnitude, obstacleLayer, QueryTriggerInteraction.Ignore);

        if (!blocked)
        {
            transform.position += desiredMove;
        }
        else
        {
            Vector3 perp = Vector3.Cross(desiredDir, Vector3.up).normalized;
            Vector3 tryA = perp;
            Vector3 tryB = -perp;
            bool blockedA = Physics.SphereCast(transform.position, obstacleCheckRadius, tryA, out _, currentMoveSpeed * dt, obstacleLayer, QueryTriggerInteraction.Ignore);
            bool blockedB = Physics.SphereCast(transform.position, obstacleCheckRadius, tryB, out _, currentMoveSpeed * dt, obstacleLayer, QueryTriggerInteraction.Ignore);

            if (!blockedA)
                transform.position += tryA * (currentMoveSpeed * dt);
            else if (!blockedB)
                transform.position += tryB * (currentMoveSpeed * dt);
        }

        if (currentVisual != null)
        {
            Vector3 visualTarget = moveTarget.position;
            Vector3 vdir = (visualTarget - currentVisual.transform.position);
            vdir.y = 0f;
            if (vdir.sqrMagnitude > 0.0001f)
            {
                currentVisual.transform.position += vdir.normalized * (currentMoveSpeed * 0.2f) * dt;
            }
            else
            {
                Vector3 toEnemy = (transform.position - currentVisual.transform.position);
                toEnemy.y = 0f;
                if (toEnemy.sqrMagnitude > 0.0001f)
                    currentVisual.transform.position += toEnemy.normalized * (currentMoveSpeed * 0.2f) * dt;
            }
        }
    }

    protected override void OnGazeFocusedCallback()
    {
        if (!canBeLookedAt || isDying) return;

        gazeHits++;
        canBeLookedAt = false;

        stoppedByGaze = true;
        isStopped = true;
        canMove = false;

        if (resumeCoroutine != null)
        {
            StopCoroutine(resumeCoroutine);
            resumeCoroutine = null;
        }

        ApplyCylinderColorForHit(gazeHits);

        if (gazeHits >= 3)
        {
            ApplyCylinderColorForHit(3);
            StartCoroutine(DieAfterDelay(0.5f));
        }
    }

    // Beim Wegschauen: sofort teleportieren (kein Delay)
    protected override void OnGazeExitCallback()
    {
        if (stoppedByGaze)
        {
            stoppedByGaze = false;

            // Sofort teleportieren entsprechend Trefferanzahl
            DoImmediateTeleportForGazeState();

            // nach Teleport startet der Resume-Timer
            if (resumeCoroutine != null)
            {
                StopCoroutine(resumeCoroutine);
                resumeCoroutine = null;
            }
            resumeCoroutine = StartCoroutine(ResumeAfterDelay());
        }
    }

    private void DoImmediateTeleportForGazeState()
    {
        if (gazeHits == 1)
        {
            GameObject picked = TeleportToRandomWithTagsReturn(new string[] { spawnTagStage2 }, excludeSameName: true);
            if (picked != null) currentSpawnName = picked.name;
            spawnedAtStage3 = false;
        }
        else if (gazeHits == 2)
        {
            // jetzt ausschließlich Spawners3
            GameObject picked = TeleportToRandomWithTagsReturn(new string[] { spawnTagStage3 }, excludeSameName: true);
            if (picked != null)
            {
                currentSpawnName = picked.name;
                spawnedAtStage3 = picked.CompareTag(spawnTagStage3);
                if (spawnedAtStage3)
                    target = Camera.main != null ? Camera.main.transform : target;
            }
        }
    }

    private IEnumerator ResumeAfterDelay()
    {
        yield return new WaitForSeconds(resumeDelay);
        resumeCoroutine = null;

        if (!stoppedByGaze && gazeHits < 3 && !isDying)
        {
            isStopped = false;
            canMove = true;
        }
    }

    private IEnumerator EnableMovementAfterDelay()
    {
        float delay = Random.Range(startDelayRange.x, startDelayRange.y);
        if (delay > 0f) yield return new WaitForSeconds(delay);
        canMove = true;
    }

    private IEnumerator DieAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        StartCoroutine(FlashBlackAndDie());
    }

    private IEnumerator FlashBlackAndDie()
    {
        if (isDying) yield break;
        isDying = true;

        UpdateCachedRenderers();
        if (cachedRenderers != null)
        {
            foreach (var r in cachedRenderers)
            {
                if (r != null)
                    r.material.color = Color.black;
            }
        }

        yield return new WaitForSeconds(0.2f);

        Spawner?.NotifyEnemyDied(this);

        Destroy(gameObject);
    }

    private Transform GetMoveTarget()
    {
        if (target != null)
            return target;
        if (Camera.main != null)
            return Camera.main.transform;
        return null;
    }

    private void TryFindCylinderRenderer()
    {
        if (cylinderRenderer != null) return;

        var child = transform.Find("Cylinder");
        if (child != null)
        {
            cylinderRenderer = child.GetComponentInChildren<Renderer>(true);
            if (cylinderRenderer != null) return;
        }

        var rends = GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends)
        {
            if (r == null) continue;
            var n = r.gameObject.name.ToLowerInvariant();
            if (n.Contains("cyl") || n.Contains("cylinder"))
            {
                cylinderRenderer = r;
                return;
            }
        }

        var selfR = GetComponent<Renderer>();
        if (selfR != null)
        {
            cylinderRenderer = selfR;
        }
    }

    private void SetupInitialVisual()
    {
        currentVisual = null;
        UpdateCachedRenderers();
    }

    /// <summary>
    /// Teleportiert zu einem zufälligen Punkt aus den angegebenen Tags.
    /// Wenn excludeSameName == true werden Kandidaten gefiltert, deren Name gleich currentSpawnName ist.
    /// Gibt das ausgewählte GameObject zurück (oder null).
    /// </summary>
    private GameObject TeleportToRandomWithTagsReturn(string[] tags, bool excludeSameName = true)
{
    if (tags == null || tags.Length == 0) return null;

    var allCandidates = new List<GameObject>();
    foreach (var t in tags)
    {
        if (string.IsNullOrEmpty(t)) continue;
        var gos = GameObject.FindGameObjectsWithTag(t);
        if (gos != null && gos.Length > 0)
            allCandidates.AddRange(gos);
    }

    if (allCandidates.Count == 0) return null;

    // Filter nach gleichem Namen (wie bisher)
    List<GameObject> filteredByName = allCandidates;
    if (excludeSameName && !string.IsNullOrEmpty(currentSpawnName))
    {
        filteredByName = new List<GameObject>();
        foreach (var g in allCandidates)
        {
            if (g == null) continue;
            if (g.name == currentSpawnName) continue;
            filteredByName.Add(g);
        }
    }

    if (filteredByName.Count == 0)
    {
        Debug.LogWarning($"Teleport: keine Kandidaten nach Ausschluss des gleichen Spawn-Namens ('{currentSpawnName}'). Kein Teleport.", this);
        return null;
    }

    // Weiter filtern: keine Kandidaten auswählen, die aktuell von einem anderen Enemy belegt sind
    var allEnemies = GameObject.FindObjectsOfType<Enemy>();
    float occupancyRadius = 0.5f;
    List<GameObject> available = new List<GameObject>();
    foreach (var g in filteredByName)
    {
        if (g == null) continue;
        bool occupied = false;
        foreach (var e in allEnemies)
        {
            if (e == null) continue;
            if (e == this) continue; // sich selbst ignorieren
            if (Vector3.Distance(e.transform.position, g.transform.position) <= occupancyRadius)
            {
                occupied = true;
                break;
            }
        }
        if (!occupied) available.Add(g);
    }

    if (available.Count == 0)
    {
        Debug.LogWarning($"Teleport: keine freien Kandidaten (nach Occupancy-Filter). Kein Teleport.", this);
        return null;
    }

    var pick = available[Random.Range(0, available.Count)];
    transform.position = pick.transform.position;
    transform.rotation = pick.transform.rotation;

    UpdateCachedRenderers();

    return pick;
}

    private void TeleportToRandomWithTags(string[] tags)
    {
        TeleportToRandomWithTagsReturn(tags);
    }

    private void TeleportToRandomWithTags(string tagSingle)
    {
        TeleportToRandomWithTags(new string[] { tagSingle });
    }

    private void UpdateCachedRenderers()
    {
        var list = new List<Renderer>();
        list.AddRange(GetComponentsInChildren<Renderer>(true));
        cachedRenderers = list.ToArray();
    }

    private void ApplyCylinderColorForHit(int hitCount)
    {
        if (cylinderRenderer == null) return;

        var mat = cylinderRenderer.material;
        if (mat == null) return;

        if (hitCount == 1) mat.color = Color.yellow;
        else if (hitCount == 2) mat.color = Color.red;
        else mat.color = Color.black;
    }
}