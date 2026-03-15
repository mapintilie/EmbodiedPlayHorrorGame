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
        // Wir speichern ECHTE Ursprungsrotation genau einmal beim Start
        startRotation = transform.rotation;
    }

    private void Update()
    {
        // Berechne die Geschwindigkeit basierend auf der TransitionTime
        // (z.B. wie viel Grad pro Sekunde bewegt werden sollen)
        float speed = 1f / Mathf.Max(0.0001f, transitionTime);

        // Wir nutzen Lerp/MoveTowards, um weich zwischen dem aktuellen und dem Zielwinkel zu wechseln
        currentPitch = Mathf.MoveTowards(currentPitch, targetPitch, speed * 30f * Time.deltaTime);
        currentYaw = Mathf.MoveTowards(currentYaw, targetYaw, speed * 30f * Time.deltaTime);

        // Wende die Rotation an (Ursprung + aktueller Offset)
        transform.rotation = startRotation * Quaternion.Euler(currentPitch, currentYaw, 0f);
    }

    // Von außen aufrufbar, um die Zielwinkel zu setzen
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