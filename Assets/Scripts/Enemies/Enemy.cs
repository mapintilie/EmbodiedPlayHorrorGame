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
    public float moveSpeed = 3f;

    [Tooltip("Optional target for movement. If null, uses Camera.main.")]
    public Transform target;

    [Tooltip("How long after spawn the enemy waits before starting to move.")]
    public Vector2 startDelayRange = new Vector2(0f, 2f);

    [Header("Stun")]
    [Tooltip("How long the enemy stays stunned when the player focuses gaze on it.")]
    public float stunDuration = 3f;

    [Header("Respawn")]
    [Tooltip("Spawner that will respawn this enemy when it dies.")]
    public EnemySpawner Spawner;

    // State
    private bool canMove;
    private bool isStunned;

    private void Start()
    {
        StartCoroutine(EnableMovementAfterDelay());
    }

    protected override void Update()
    {
        // Keep base gaze logic running.
        base.Update();

        if (!canMove || isStunned) return;

        Transform moveTarget = GetMoveTarget();
        if (moveTarget == null) return;

        Vector3 direction = (moveTarget.position - transform.position);
        direction.y = 0f; // keep enemies on ground plane (optional)

        if (direction.sqrMagnitude < 0.01f) return;

        Vector3 move = direction.normalized * moveSpeed * Time.deltaTime;
        transform.position += move;
    }

    protected override void OnGazeFocusedCallback()
    {
        if (!isStunned)
        {
            StartCoroutine(StunThenDie());
        }
    }

    private IEnumerator EnableMovementAfterDelay()
    {
        float delay = Random.Range(startDelayRange.x, startDelayRange.y);
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        canMove = true;
    }

    private IEnumerator StunThenDie()
    {
        isStunned = true;
        canMove = false;

        // Optionally: add visual feedback here (e.g. change material color).

        yield return new WaitForSeconds(stunDuration);

        Die();
    }

    private void Die()
    {
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
}
