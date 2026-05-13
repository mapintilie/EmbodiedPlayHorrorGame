using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Enemy : GazeInteractable
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public Transform target;
    public Vector2 startDelayRange = new Vector2(0f, 2f);

    [Header("Respawn")]
    public EnemySpawner Spawner;

    [Header("Gaze")]
    [SerializeField] private float resumeDelay = 10f;

    [Header("Angel Prefabs")]
    public GameObject angelPrefab1;
    public GameObject angelPrefab2;
    public GameObject angelPrefab3;

    [Header("Cylinder (optional)")]
    [Tooltip("Renderer of the cylinder that should change color. If null, the script will try to find a child named 'Cylinder' or a suitable renderer.")]
    public Renderer cylinderRenderer;

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

    private Renderer[] cachedRenderers;
    private GameObject currentVisual; // now instantiated as an independent object (not parented)

    private Coroutine resumeCoroutine;

    private void Start()
    {
        StartCoroutine(EnableMovementAfterDelay());

        // try to find cylinder renderer before adding angel visual
        TryFindCylinderRenderer();

        // Setup initial visual (Angel1) if configured (do not remove existing cylinder)
        SetupInitialVisual();

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

        if (!canMove || isStopped) return;

        Transform moveTarget = GetMoveTarget();
        if (moveTarget == null) return;

        Vector3 direction = moveTarget.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f) return;

        Vector3 move = direction.normalized * moveSpeed * Time.deltaTime;
        transform.position += move;

        // Move the angel visual independently at 0.2x of the enemy's current moveSpeed
        if (currentVisual != null)
        {
            // If there's a global move target, have the visual head toward that target at reduced speed
            Vector3 visualTarget = moveTarget.position;
            Vector3 vdir = visualTarget - currentVisual.transform.position;
            vdir.y = 0f;
            if (vdir.sqrMagnitude > 0.0001f)
            {
                currentVisual.transform.position += vdir.normalized * (moveSpeed * 0.2f) * Time.deltaTime;
            }
            else
            {
                // If already at the target, gently approach the enemy position so it doesn't drift away
                Vector3 toEnemy = transform.position - currentVisual.transform.position;
                toEnemy.y = 0f;
                if (toEnemy.sqrMagnitude > 0.0001f)
                    currentVisual.transform.position += toEnemy.normalized * (moveSpeed * 0.2f) * Time.deltaTime;
            }
        }
    }

    protected override void OnGazeFocusedCallback()
    {
        if (!canBeLookedAt) return;

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

        // Change cylinder color according to hit count
        ApplyCylinderColorForHit(gazeHits);

        // Swap angels
        if (gazeHits == 1)
            SwapVisualToPrefab(angelPrefab2);
        else if (gazeHits == 2)
            SwapVisualToPrefab(angelPrefab3);

        // On third hit: black and die after 0.5s
        if (gazeHits >= 3)
        {
            // ensure cylinder is black immediately
            ApplyCylinderColorForHit(3);
            StartCoroutine(DieAfterDelay(0.5f));
        }
    }

    protected override void OnGazeExitCallback()
    {
        if (stoppedByGaze)
        {
            stoppedByGaze = false;

            if (resumeCoroutine != null)
                StopCoroutine(resumeCoroutine);

            resumeCoroutine = StartCoroutine(ResumeAfterDelay());
        }
    }

    private IEnumerator ResumeAfterDelay()
    {
        yield return new WaitForSeconds(resumeDelay);
        resumeCoroutine = null;

        if (!stoppedByGaze && gazeHits < 3)
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

        if (currentVisual != null)
            Destroy(currentVisual);

        Destroy(gameObject);
    }

    private void Die()
    {
        StartCoroutine(FlashBlackAndDie());
    }

    private Transform GetMoveTarget()
    {
        if (target != null) return target;
        if (Camera.main != null) return Camera.main.transform;
        return null;
    }

    // ----- Visual Management -----
    private void TryFindCylinderRenderer()
    {
        if (cylinderRenderer != null) return;

        // Prefer child named "Cylinder"
        var child = transform.Find("Cylinder");
        if (child != null)
        {
            cylinderRenderer = child.GetComponentInChildren<Renderer>(true);
            if (cylinderRenderer != null) return;
        }

        // Fallback: search children for a renderer whose name contains "cyl" (case-insensitive)
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

        // Last resort: use the first renderer on this GameObject
        var selfR = GetComponent<Renderer>();
        if (selfR != null)
        {
            cylinderRenderer = selfR;
        }
    }

    private void SetupInitialVisual()
    {
        // Instantiate angelPrefab1 as an independent GameObject (not parented) so it can move at reduced speed.
        if (angelPrefab1 != null)
        {
            if (currentVisual == null)
            {
                currentVisual = Instantiate(angelPrefab1);
                currentVisual.transform.position = transform.position;
                currentVisual.transform.rotation = transform.rotation;
                currentVisual.transform.localScale = Vector3.one;
            }

            UpdateCachedRenderers();
            return;
        }

        // Falls kein Prefab konfiguriert ist, nutze vorhandene Renderer
        UpdateCachedRenderers();
    }

    private void SwapVisualToPrefab(GameObject prefab)
    {
        if (prefab == null) return;

        if (currentVisual != null)
        {
            Destroy(currentVisual);
            currentVisual = null;
        }

        currentVisual = Instantiate(prefab);
        currentVisual.transform.position = transform.position;
        currentVisual.transform.rotation = transform.rotation;
        currentVisual.transform.localScale = Vector3.one;

        UpdateCachedRenderers();
    }

    private void UpdateCachedRenderers()
    {
        var list = new List<Renderer>();
        // renderers that belong to the enemy and its children
        list.AddRange(GetComponentsInChildren<Renderer>(true));

        // include renderers from the independent currentVisual as well
        if (currentVisual != null)
        {
            var vr = currentVisual.GetComponentsInChildren<Renderer>(true);
            foreach (var r in vr)
            {
                if (r != null && !list.Contains(r))
                    list.Add(r);
            }
        }

        cachedRenderers = list.ToArray();
    }

    private void ApplyCylinderColorForHit(int hitCount)
    {
        if (cylinderRenderer == null) return;

        // ensure material instance
        var mat = cylinderRenderer.material;
        if (mat == null) return;

        if (hitCount <= 0)
        {
            return;
        }
        else if (hitCount == 1)
        {
            mat.color = Color.yellow;
        }
        else if (hitCount == 2)
        {
            mat.color = Color.red;
        }
        else // 3 or more
        {
            mat.color = Color.black;
        }
    }
}