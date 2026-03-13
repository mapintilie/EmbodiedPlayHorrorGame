using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class GazeScreenmove : GazeInteractable
{
    private enum ViewDirection { Top, Bottom, Left, Right }

    [SerializeField] private Camera targetCamera;
    [SerializeField] private ViewDirection viewDirection = ViewDirection.Top;
    [SerializeField] private float maxAngle = 30f;
    [SerializeField] private float transitionTime = 1f;

    private Quaternion baselineWorldRotation;
    private float gazeProgress = 0f; // 0..1

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
        {
            Debug.LogWarning($"GazeScreenmove: Keine Kamera gefunden auf {gameObject.name}, deaktiviert.");
            enabled = false;
            return;
        }
    }

    protected override void Update()
    {
        base.Update();

        if (!enabled || targetCamera == null) return;
        
        float delta = Time.deltaTime / Mathf.Max(0.0001f, transitionTime);
        if (IsGazedAt)
            gazeProgress += delta;

        gazeProgress = Mathf.Clamp01(gazeProgress);

        float pitch = 0f;
        float yaw = 0f;

        switch (viewDirection)
        {
            case ViewDirection.Top:
                pitch = -maxAngle * gazeProgress; // nach oben
                break;
            case ViewDirection.Bottom:
                pitch = maxAngle * gazeProgress;  // nach unten
                break;
            case ViewDirection.Left:
                yaw = -maxAngle * gazeProgress;   // links
                break;
            case ViewDirection.Right:
                yaw = maxAngle * gazeProgress;    // rechts
                break;
        }

        Debug.Log($"GazeScreenmove: {gameObject.name} IsGazedAt={IsGazedAt} progress={gazeProgress:F2} pitch={pitch:F2} yaw={yaw:F2}");

        Quaternion targetRotation = baselineWorldRotation * Quaternion.Euler(pitch, yaw, 0f);
        targetCamera.transform.rotation = targetRotation;
    }

    protected override void OnGazeEnterCallback()
    {
        if (targetCamera != null)
        {
            baselineWorldRotation = targetCamera.transform.rotation;
            gazeProgress = 0f;

            Debug.Log($"Baseline reset for {gameObject.name}");
        }
    }

    protected override void OnGazeExitCallback()
    {
        Debug.Log($"Gaze exited panel: {gameObject.name}");
    }
}