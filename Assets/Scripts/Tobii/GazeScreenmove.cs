using UnityEngine;

// [RequireComponent(typeof(Image))] // <- Entfernt, falls es 3D Cubes sind! 
public class GazeScreenmove : GazeInteractable
{
    public enum ViewDirection { Top, Bottom, Left, Right }

    [SerializeField] private ViewDirection viewDirection = ViewDirection.Top;
    [SerializeField] private float maxAngle = 30f;

    private GazeCameraController cameraController;

    private void Start()
    {
        // Sucht automatisch den Controller in der Szene
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

        // Bestimme das Ziel anhand der Richtung
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

        // Statt die Kamera auf (0,0) zurückzusetzen, frieren wir sie auf der aktuellen Position ein
        cameraController.StopMovement();
        
        Debug.Log($"Gaze Exit auf {gameObject.name}: Bewegung gestoppt.");
    }
}