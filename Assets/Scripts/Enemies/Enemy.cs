using System.Collections;
using UnityEngine;

/// <summary>
/// Enemy that moves towards the player and can be stunned by gaze.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Enemy : GazeInteractable
{
    [Header("Movement")]
    [Tooltip("How fast the enemy moves towards the player.")]
    public float moveSpeed = 2f;

    [Tooltip("Optional target for movement. If null, uses Camera.main.")]
    public Transform target;

    [Tooltip("How long after spawn the enemy waits before starting to move.")]
    public Vector2 startDelayRange = new Vector2(0f, 2f);

    [Header("Respawn")]
    [Tooltip("Spawner that will respawn this enemy when it dies.")]
    public EnemySpawner Spawner;

    [Header("Gaze")]
    [Tooltip("Delay after gaze exit until movement resumes.")]
    [SerializeField] private float resumeDelay = 10f;

    [Header("Spawn Indicator")]
    [Tooltip("Show a SpawnIndicatorUI at this enemy's spawn position on Start.")]
    public bool showSpawnIndicatorOnSpawn = true;

    [Tooltip("How long the spawn indicator is visible (seconds).")]
    public float spawnIndicatorDuration = 2f;

    // State
    private bool canMove;
    private bool isStopped;
    private bool canBeLookedAt = true; // erlaubt ein Gaze pro Raum
    private int gazeHits = 0;
    private bool stoppedByGaze = false;
    private bool isDying = false;

    private Renderer cachedRenderer;

    private Coroutine resumeCoroutine;

    private void Start()
    {
        StartCoroutine(EnableMovementAfterDelay());

        cachedRenderer = GetComponentInChildren<Renderer>();
        if (cachedRenderer == null)
        {
            Debug.LogWarning($"[Enemy] No Renderer found on {name}");
        }

        // Zeige optional Spawn-Indicator an der Position, an der dieser Enemy erzeugt wurde.
        if (showSpawnIndicatorOnSpawn)
        {
            var indicator = FindObjectOfType<SpawnIndicatorUI>();
            if (indicator != null)
            {
                indicator.ShowAtWorldPosition(transform.position, spawnIndicatorDuration);
            }
        }

        // subscribe room-change event so gaze can be used again after turning
        GazeCameraController.OnRoomChanged += OnRoomChanged;
    }

    private void OnDestroy()
    {
        GazeCameraController.OnRoomChanged -= OnRoomChanged;
    }

    private void OnRoomChanged()
    {
        // Nach Raumwechsel kann der Enemy wieder einmal angeguckt werden
        canBeLookedAt = true;
        // Keine Farbänderung / kein Zurücksetzen — Farbe bleibt dauerhaft
    }

    protected override void Update()
    {
        // Keep base gaze logic running.
        base.Update();

        if (!canMove || isStopped) return;

        Transform moveTarget = GetMoveTarget();
        if (moveTarget == null) return;

        Vector3 direction = (moveTarget.position - transform.position);
        direction.y = 0f; // keep enemies on ground plane (optional)

        if (direction.sqrMagnitude < 0.01f) return;

        Vector3 move = direction.normalized * moveSpeed * Time.deltaTime;
        transform.position += move;
    }

    // Wird aufgerufen, wenn der Gaze-Fokus abgeschlossen ist (vollständig angesehen)
    protected override void OnGazeFocusedCallback()
    {
        if (!canBeLookedAt) return;

        gazeHits++;
        canBeLookedAt = false; // nur ein Blick pro Raum

        // Stoppe Bewegung solange der Spieler hinschaut
        stoppedByGaze = true;
        isStopped = true;
        canMove = false;

        // Stoppe eventuell laufenden Resume-Timer
        if (resumeCoroutine != null)
        {
            StopCoroutine(resumeCoroutine);
            resumeCoroutine = null;
        }

        // Setze Farbe je nach Trefferanzahl (nur einmal pro Blick)
        if (cachedRenderer != null)
        {
            if (gazeHits == 1)
                cachedRenderer.material.color = Color.yellow;
            else if (gazeHits == 2)
                cachedRenderer.material.color = Color.red;
            // WICHTIG: Es gibt keine Stelle, die die Farbe wieder auf die vorherige zurücksetzt.
        }

        // Zerstöre sofort beim 3. Treffer
        if (gazeHits >= 3)
        {
            StartCoroutine(FlashBlackAndDie());
        }
    }

    // Wird aufgerufen, wenn der Gaze verlassen wird
    protected override void OnGazeExitCallback()
    {
        if (stoppedByGaze)
        {
            stoppedByGaze = false;

            // starte verzögertes Wiederanfahren (wird abgebrochen, falls wieder hingeschaut wird)
            if (resumeCoroutine != null)
                StopCoroutine(resumeCoroutine);

            resumeCoroutine = StartCoroutine(ResumeAfterDelay());
        }
    }

    private IEnumerator ResumeAfterDelay()
    {
        yield return new WaitForSeconds(resumeDelay);
        resumeCoroutine = null;

        // nur weitermachen, wenn aktuell nicht erneut angesehen und nicht bereits gestorben
        if (!stoppedByGaze && gazeHits < 3)
        {
            isStopped = false;
            canMove = true;
        }
    }

    private IEnumerator EnableMovementAfterDelay()
    {
        float delay = Random.Range(startDelayRange.x, startDelayRange.y);
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        canMove = true;
    }

    // Setzt Material kurz auf schwarz und zerstört dann nach 1s
    private IEnumerator FlashBlackAndDie()
    {
        if (isDying) yield break;
        isDying = true;

        if (cachedRenderer != null)
        {
            // access .material to ensure instance so we don't modify shared material unexpectedly
            cachedRenderer.material.color = Color.black;
        }

        yield return new WaitForSeconds(0.2f);

        Spawner?.NotifyEnemyDied(this);
        Destroy(gameObject);
    }

    private void Die()
    {
        // fallback falls andere Stellen noch Die() aufrufen: benutze die Coroutine
        StartCoroutine(FlashBlackAndDie());
    }

    private Transform GetMoveTarget()
    {
        if (target != null)
            return target;

        if (Camera.main != null)
            return Camera.main.transform;

        return null;
    }
}

///SARAHS CODE!! KEEP!!
///
///
/*
using System.Collections;
using UnityEngine;

/// <summary>
/// Enemy that moves towards the player and can be stunned by gaze.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Enemy : GazeInteractable
{
    [Header("Movement")]
    [Tooltip("How fast the enemy moves towards the player.")]
    public float moveSpeed = 2f;

    [Tooltip("Optional target for movement. If null, uses Camera.main.")]
    public Transform target;

    [Tooltip("How long after spawn the enemy waits before starting to move.")]
    public Vector2 startDelayRange = new Vector2(0f, 2f);

    [Header("Respawn")]
    [Tooltip("Spawner that will respawn this enemy when it dies.")]
    public EnemySpawner Spawner;

    [Header("Gaze")]
    [Tooltip("Delay after gaze exit until movement resumes.")]
    [SerializeField] private float resumeDelay = 10f;

    // State
    private bool canMove;
    private bool isStopped;
    private bool canBeLookedAt = true; // erlaubt ein Gaze pro Raum
    private int gazeHits = 0;
    private bool stoppedByGaze = false;
    private bool isDying = false;

    private Renderer cachedRenderer;

    private Coroutine resumeCoroutine;

    private void Start()
    {
        StartCoroutine(EnableMovementAfterDelay());

        cachedRenderer = GetComponentInChildren<Renderer>();
        if (cachedRenderer == null)
        {
            Debug.LogWarning($"[Enemy] No Renderer found on {name}");
        }

        // subscribe room-change event so gaze can be used again after turning
        GazeCameraController.OnRoomChanged += OnRoomChanged;
    }

    private void OnDestroy()
    {
        GazeCameraController.OnRoomChanged -= OnRoomChanged;
    }

    private void OnRoomChanged()
    {
        // Nach Raumwechsel kann der Enemy wieder einmal angeguckt werden
        canBeLookedAt = true;
        // Keine Farbänderung / kein Zurücksetzen — Farbe bleibt dauerhaft
    }

    protected override void Update()
    {
        // Keep base gaze logic running.
        base.Update();

        if (!canMove || isStopped) return;

        Transform moveTarget = GetMoveTarget();
        if (moveTarget == null) return;

        Vector3 direction = (moveTarget.position - transform.position);
        direction.y = 0f; // keep enemies on ground plane (optional)

        if (direction.sqrMagnitude < 0.01f) return;

        Vector3 move = direction.normalized * moveSpeed * Time.deltaTime;
        transform.position += move;
    }

    // Wird aufgerufen, wenn der Gaze-Fokus abgeschlossen ist (vollständig angesehen)
    protected override void OnGazeFocusedCallback()
    {
        if (!canBeLookedAt) return;

        gazeHits++;
        canBeLookedAt = false; // nur ein Blick pro Raum

        // Stoppe Bewegung solange der Spieler hinschaut
        stoppedByGaze = true;
        isStopped = true;
        canMove = false;

        // Stoppe eventuell laufenden Resume-Timer
        if (resumeCoroutine != null)
        {
            StopCoroutine(resumeCoroutine);
            resumeCoroutine = null;
        }

        // Setze Farbe je nach Trefferanzahl (nur einmal pro Blick)
        if (cachedRenderer != null)
        {
            if (gazeHits == 1)
                cachedRenderer.material.color = Color.yellow;
            else if (gazeHits == 2)
                cachedRenderer.material.color = Color.red;
            // WICHTIG: Es gibt keine Stelle, die die Farbe wieder auf die vorherige zurücksetzt.
        }

        // Zerstöre sofort beim 3. Treffer
        if (gazeHits >= 3)
        {
            StartCoroutine(FlashBlackAndDie());
        }
    }

    // Wird aufgerufen, wenn der Gaze verlassen wird
    protected override void OnGazeExitCallback()
    {
        if (stoppedByGaze)
        {
            stoppedByGaze = false;

            // starte verzögertes Wiederanfahren (wird abgebrochen, falls wieder hingeschaut wird)
            if (resumeCoroutine != null)
                StopCoroutine(resumeCoroutine);

            resumeCoroutine = StartCoroutine(ResumeAfterDelay());
        }
    }

    private IEnumerator ResumeAfterDelay()
    {
        yield return new WaitForSeconds(resumeDelay);
        resumeCoroutine = null;

        // nur weitermachen, wenn aktuell nicht erneut angesehen und nicht bereits gestorben
        if (!stoppedByGaze && gazeHits < 3)
        {
            isStopped = false;
            canMove = true;
        }
    }

    private IEnumerator EnableMovementAfterDelay()
    {
        float delay = Random.Range(startDelayRange.x, startDelayRange.y);
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        canMove = true;
    }

    // Setzt Material kurz auf schwarz und zerstört dann nach 1s
    private IEnumerator FlashBlackAndDie()
    {
        if (isDying) yield break;
        isDying = true;

        if (cachedRenderer != null)
        {
            // access .material to ensure instance so we don't modify shared material unexpectedly
            cachedRenderer.material.color = Color.black;
        }

        yield return new WaitForSeconds(0.2f);

        Spawner?.NotifyEnemyDied(this);
        Destroy(gameObject);
    }

    private void Die()
    {
        // fallback falls andere Stellen noch Die() aufrufen: benutze die Coroutine
        StartCoroutine(FlashBlackAndDie());
    }

    private Transform GetMoveTarget()
    {
        if (target != null)
            return target;

        if (Camera.main != null)
            return Camera.main.transform;

        return null;
    }
}

*/
//SARAHS CODE OVER