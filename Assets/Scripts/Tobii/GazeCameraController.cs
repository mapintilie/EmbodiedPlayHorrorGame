using System;
using UnityEngine;

public class GazeCameraController : MonoBehaviour
{
    [Header("Gaze Settings")]
    [SerializeField] private float transitionTime = 1f;
    [SerializeField] private float roomChangeGazeSpeedMultiplier = 5f; // Makes gaze reset 5x faster ONLY when changing rooms

    [Header("Turn Settings")]
    [SerializeField] private float turnSpeed = 150f; 

    public static event Action OnRoomChanged;

    private Quaternion absoluteStartRotation;
    private float currentBaseYaw = 0f; 

    private Quaternion currentBaseRotation;
    private Quaternion targetBaseRotation;

    private float targetPitch = 0f;
    private float targetYaw = 0f;
    
    private float currentPitch = 0f;
    private float currentYaw = 0f;

    private void Start()
    {
        absoluteStartRotation = transform.rotation;
        
        currentBaseRotation = absoluteStartRotation;
        targetBaseRotation = absoluteStartRotation;
    }

    private void Update()
    {
        // 1. Check input for 90-degree snap turns
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            TurnBase(90f);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            TurnBase(-90f);
        }

        // 2. Smoothly animate base rotation
        currentBaseRotation = Quaternion.RotateTowards(currentBaseRotation, targetBaseRotation, turnSpeed * Time.deltaTime);

        // 3. Check if we are currently in the middle of a room change
        bool isChangingRooms = Quaternion.Angle(currentBaseRotation, targetBaseRotation) > 0.1f;

        // 4. Calculate Gaze speed
        float baseSpeed = (1f / Mathf.Max(0.0001f, transitionTime)) * 30f;
        
        // Apply the fast multiplier ONLY if the room is actively turning
        float activeSpeed = isChangingRooms ? (baseSpeed * roomChangeGazeSpeedMultiplier) : baseSpeed;

        currentPitch = Mathf.MoveTowards(currentPitch, targetPitch, activeSpeed * Time.deltaTime);
        currentYaw = Mathf.MoveTowards(currentYaw, targetYaw, activeSpeed * Time.deltaTime);

        // 5. Combine both
        transform.rotation = currentBaseRotation * Quaternion.Euler(currentPitch, currentYaw, 0f);
    }

    private void TurnBase(float angle)
    {
        currentBaseYaw += angle;
        targetBaseRotation = absoluteStartRotation * Quaternion.Euler(0f, currentBaseYaw, 0f);

        // Discard the gaze offset so we look straight ahead into the new room
        targetPitch = 0f;
        targetYaw = 0f;

        // Notify listeners (z. B. Enemies), damit sie wieder gaze-aktiviert werden
        OnRoomChanged?.Invoke();
    }

    public void SetTargetAngles(float pitch, float yaw)
    {
        targetPitch = pitch;
        targetYaw = yaw;
    }

    public void StopMovement()
    {
        targetPitch = currentPitch;
        targetYaw = currentYaw;
    }
}