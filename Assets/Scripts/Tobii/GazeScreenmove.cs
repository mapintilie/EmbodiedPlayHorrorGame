using UnityEngine;
public class GazeScreenmove : GazeInteractable
{
    public enum ViewDirection { Top, Bottom, Left, Right }

    [SerializeField] private ViewDirection viewDirection = ViewDirection.Top;
    [SerializeField] private float maxAngle = 30f;

    private GazeCameraController cameraController;

    private void Start()
    {
        // sucht controller in der szene
        cameraController = FindObjectOfType<GazeCameraController>();
        
        if (cameraController == null)
        {
            Debug.LogError("GazeCameraController nicht in der Szene gefunden!");
        }
    }

    protected override void OnGazeEnterCallback()
    {
        if (cameraController == null) return;

        float pitch = 0f;
        float yaw = 0f;

        // richtung bestimmen
        switch (viewDirection)
        {
            case ViewDirection.Top:    pitch = -maxAngle; break;
            case ViewDirection.Bottom: pitch = maxAngle;  break;
            case ViewDirection.Left:   yaw = -maxAngle;   break;
            case ViewDirection.Right:  yaw = maxAngle;    break;
        }

        cameraController.SetTargetAngles(pitch, yaw);
        Debug.Log($"Gaze Enter: Bewege Kamera zu Pitch {pitch}, Yaw {yaw}");
    }

    protected override void OnGazeExitCallback()
    {
        if (cameraController == null) return;

        // kamera auf die position einfrieren
        cameraController.StopMovement();
        
        Debug.Log($"Gaze Exit auf {gameObject.name}: Bewegung gestoppt.");
    }
}