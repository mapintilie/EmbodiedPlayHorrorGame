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
    private GameObject currentVisual;

    private Coroutine resumeCoroutine;

    private void Start()
    {
        StartCoroutine(EnableMovementAfterDelay());

        // Setup initial visual (Angel1) or replace child visuals if configured
        SetupInitialVisual();

        if (showSpawnIndicatorOnSpawn)
        {
            var indicator = FindObjectOfType<SpawnIndicatorUI>();
            if (indicator != null)
                indicator.ShowAtWorldPosition(transform.position, spawnIndicatorDuration);
        }

        // Event: falls das Event eine andere Signatur hat, muss das extern angepasst werden
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

        if (gazeHits == 1)
            SwapVisualToPrefab(angelPrefab2);
        else if (gazeHits == 2)
            SwapVisualToPrefab(angelPrefab3);

        if (gazeHits >= 3)
            StartCoroutine(FlashBlackAndDie());
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
    private void SetupInitialVisual()
    {
        // Wenn angelPrefab1 gesetzt ist, entferne vorhandene Child-Visuals (z.B. Zylinder) und instanziere Angel1.
        if (angelPrefab1 != null)
        {
            var toDestroy = new List<GameObject>();
            foreach (Transform child in transform)
            {
                if (child.GetComponentInChildren<Renderer>() != null)
                    toDestroy.Add(child.gameObject);
            }

            foreach (var go in toDestroy)
                Destroy(go);

            SwapVisualToPrefab(angelPrefab1);
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

        currentVisual = Instantiate(prefab, transform);
        currentVisual.transform.localPosition = Vector3.zero;
        currentVisual.transform.localRotation = Quaternion.identity;
        currentVisual.transform.localScale = Vector3.one;

        UpdateCachedRenderers();
    }

    private void UpdateCachedRenderers()
    {
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
    }
}