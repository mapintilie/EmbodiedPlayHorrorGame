using System;
using UnityEngine;

public class GazeCameraController : MonoBehaviour
{
    [Header("Gaze Edge Panning (Joystick Style)")]
    [Tooltip("How far the camera can pan left/right (in degrees).")]
    [SerializeField] private float maxYaw = 25f;
    [Tooltip("How far the camera can pan up/down (in degrees).")]
    [SerializeField] private float maxPitch = 15f;
    [Tooltip("When to start panning (0.25 = outer 25% of the screen).")]
    [SerializeField] private float edgeThreshold = 0.25f;
    [Tooltip("How fast the camera pushes when looking at the edge.")]
    [SerializeField] private float edgePanSpeed = 45f;

    [Header("Gaze Settings")]
    [SerializeField] private float transitionTime = 0.8f;
    [SerializeField] private float roomChangeGazeSpeedMultiplier = 5f;

    [Header("Turn Settings")]
    [SerializeField] private float turnSnappiness = 15f; 

    public static event Action OnRoomChanged;

    private Quaternion absoluteStartRotation;
    private float currentBaseYaw = 0f; 

    private Quaternion currentBaseRotation;
    private Quaternion targetBaseRotation;

    private float targetPitch = 0f;
    private float targetYaw = 0f;
    
    private float currentPitch = 0f;
    private float currentYaw = 0f;

    private bool isChangingRooms = false;

    private void Start()
    {
        absoluteStartRotation = transform.rotation;
        currentBaseRotation = absoluteStartRotation;
        targetBaseRotation = absoluteStartRotation;
    }

    private void Update()
    {
        // 1. Room turning (Snap Turns)
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            TurnBase(90f);
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            TurnBase(-90f);

        // 2. Smooth room rotation (Slerp)
        currentBaseRotation = Quaternion.Slerp(currentBaseRotation, targetBaseRotation, turnSnappiness * Time.deltaTime);

        // Are we currently spinning?
        isChangingRooms = Quaternion.Angle(currentBaseRotation, targetBaseRotation) > 0.1f;

        // 3. Calculate Edge Panning (Joystick Style)
        CalculateEdgePanning();

        // 4. Calculate Gaze Speed (Smoothing the virtual joystick)
        float baseSpeed = (1f / Mathf.Max(0.0001f, transitionTime)) * 42f;
        float activeSpeed = isChangingRooms ? (baseSpeed * roomChangeGazeSpeedMultiplier) : baseSpeed;

        currentPitch = Mathf.MoveTowards(currentPitch, targetPitch, activeSpeed * Time.deltaTime);
        currentYaw = Mathf.MoveTowards(currentYaw, targetYaw, activeSpeed * Time.deltaTime);

        // 5. Combine everything
        transform.rotation = currentBaseRotation * Quaternion.Euler(currentPitch, currentYaw, 0f);
    }

    private void CalculateEdgePanning()
    {
        // SAFETY: If we are changing rooms, reset everything so we snap to the dead center.
        if (isChangingRooms) 
        {
            targetPitch = 0f;
            targetYaw = 0f;
            return;
        }

        // Read viewport coordinates (0.0 to 1.0) directly from your TobiiManager
        if (TobiiManager.Instance != null && TobiiManager.Instance.HasValidGazeData)
        {
            Vector2 viewport = TobiiManager.Instance.GazePointViewport;

            // --- HORIZONTAL (Left/Right) ---
            if (viewport.x < edgeThreshold) 
                targetYaw -= edgePanSpeed * Time.deltaTime; // Push Left
            else if (viewport.x > 1f - edgeThreshold) 
                targetYaw += edgePanSpeed * Time.deltaTime; // Push Right
            
            // Clamp so we don't spin 360 degrees
            targetYaw = Mathf.Clamp(targetYaw, -maxYaw, maxYaw);

            // --- VERTICAL (Up/Down) ---
            if (viewport.y < edgeThreshold) 
                targetPitch += edgePanSpeed * Time.deltaTime; // Push Down
            else if (viewport.y > 1f - edgeThreshold) 
                targetPitch -= edgePanSpeed * Time.deltaTime; // Push Up
            
            // Clamp so we don't stare at the floor/ceiling
            targetPitch = Mathf.Clamp(targetPitch, -maxPitch, maxPitch);
        }
    }

    private void TurnBase(float angle)
    {
        currentBaseYaw += angle;
        targetBaseRotation = absoluteStartRotation * Quaternion.Euler(0f, currentBaseYaw, 0f);

        // HARD RESET: Overwrite any eye offsets so you look straight ahead during a turn.
        targetPitch = 0f;
        targetYaw = 0f;
        currentPitch = 0f; 
        currentYaw = 0f;   

        OnRoomChanged?.Invoke();
    }

    // =========================================================================
    // LEGACY METHODS (To prevent compiler errors from GazeScreenmove.cs)
    // =========================================================================

    public void SetTargetAngles(float pitch, float yaw)
    {
        // Intentionally left blank! 
    }

    public void StopMovement()
    {
        // Intentionally left blank!
    }
}