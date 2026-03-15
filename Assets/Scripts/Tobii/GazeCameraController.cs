using UnityEngine;

public class GazeCameraController : MonoBehaviour
{
    [SerializeField] private float transitionTime = 1f;
    
    private Quaternion startRotation;
    private float targetPitch = 0f;
    private float targetYaw = 0f;
    
    private float currentPitch = 0f;
    private float currentYaw = 0f;

    private void Start()
    {
        // ursprungsrotation wird 1x beim start gespeichert
        startRotation = transform.rotation;
    }

    private void Update()
    {
        // geschwindigkeit basierend auf transitiontime berechnen
        float speed = 1f / Mathf.Max(0.0001f, transitionTime);

        //  Lerp/MoveTowards für weiche transitions
        currentPitch = Mathf.MoveTowards(currentPitch, targetPitch, speed * 30f * Time.deltaTime);
        currentYaw = Mathf.MoveTowards(currentYaw, targetYaw, speed * 30f * Time.deltaTime);

        // rotation anwenden
        transform.rotation = startRotation * Quaternion.Euler(currentPitch, currentYaw, 0f);
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