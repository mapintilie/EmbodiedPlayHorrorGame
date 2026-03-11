// ============================================================================
// GazeInteractable.cs  
// ============================================================================

using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class GazeInteractable : MonoBehaviour
{
    // ========================================================================
    // KONFIGURATION
    // ========================================================================

    [Header("Gaze Einstellungen")]
    [Tooltip("Wie lange (Sekunden) muss man das Objekt ansehen, " +
             "bevor OnGazeFocused ausgelöst wird?")]
    [SerializeField] protected float focusTime = 0.3f;

    [Header("Events")]
    public UnityEvent OnGazeEnter;
    public UnityEvent OnGazeExit;
    public UnityEvent OnGazeFocused;

    // ========================================================================
    // ÖFFENTLICHE PROPERTIES
    // ========================================================================

    public bool IsGazedAt { get; private set; }
    public float GazeDuration { get; private set; }
    public bool IsFocused { get; private set; }

    // ========================================================================
    // PRIVATE FELDER
    // ========================================================================

    private bool wasGazedAtLastFrame = false;
    private bool wasFocused = false;

    // ========================================================================
    // UNITY LIFECYCLE
    // ========================================================================

    protected virtual void Update()
    {
        if (TobiiManager.Instance == null || !TobiiManager.Instance.HasValidGazeData)
        {
            // Kein Gaze — aber nicht sofort resetten.
            // TobiiManager hat schon seine Grace Period.
            // Nur resetten wenn wirklich keine Daten.
            if (!wasGazedAtLastFrame) return;
            HandleGazeLost();
            return;
        }

        // Zentraler Raycast aus TobiiManager — kein eigener Raycast nötig!
        bool currentlyGazed = (TobiiManager.Instance.GazedObject == gameObject);

        if (currentlyGazed)
        {
            HandleGazeHit();
        }
        else
        {
            HandleGazeLost();
        }
    }

    // ========================================================================
    // GAZE-LOGIK
    // ========================================================================

    private void HandleGazeHit()
    {
        IsGazedAt = true;
        GazeDuration += Time.deltaTime;

        if (!wasGazedAtLastFrame)
        {
            wasGazedAtLastFrame = true;
            OnGazeEnter?.Invoke();
            OnGazeEnterCallback();
        }

        if (!wasFocused && GazeDuration >= focusTime)
        {
            wasFocused = true;
            IsFocused = true;
            OnGazeFocused?.Invoke();
            OnGazeFocusedCallback();
        }

        OnGazeStayCallback();
    }

    private void HandleGazeLost()
    {
        if (wasGazedAtLastFrame)
        {
            wasGazedAtLastFrame = false;
            wasFocused = false;
            IsFocused = false;
            GazeDuration = 0f;
            IsGazedAt = false;

            OnGazeExit?.Invoke();
            OnGazeExitCallback();
        }
    }

    // ========================================================================
    // ÜBERSCHREIBBARE CALLBACKS
    // ========================================================================

    protected virtual void OnGazeEnterCallback() { }
    protected virtual void OnGazeStayCallback() { }
    protected virtual void OnGazeFocusedCallback() { }
    protected virtual void OnGazeExitCallback() { }
}
