using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class GazeScreenmove : GazeInteractable
{
    [SerializeField] private Camera targetCamera;
    //rotation in grad (nach oben)
    [SerializeField] private float maxPitchAngle = 30f;

    //Sekunden für Bewegung
    [SerializeField] private float transitionTime = 1f;

    private Quaternion originalLocalRotation;
    private float gazeProgress = 0f; // 0..1

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        originalLocalRotation = targetCamera.transform.localRotation;
    }

    protected override void Update()
    {
        base.Update();

        if (!enabled || targetCamera == null) return;

        // nach oben schauen nur wenn man auf die box starrt
        float delta = Time.deltaTime / Mathf.Max(0.0001f, transitionTime);
        if (IsGazedAt)
            gazeProgress += delta;

        gazeProgress = Mathf.Clamp01(gazeProgress);

        // nach oben
        float pitch = -maxPitchAngle * gazeProgress;

        // relativ zur ursprünglichen rotation rotieren
        Quaternion targetRotation = originalLocalRotation * Quaternion.Euler(pitch, 0f, 0f);
        targetCamera.transform.localRotation = targetRotation;
    }

    protected override void OnGazeEnterCallback()
    {
        Debug.Log($"Gaze entered panel: {gameObject.name}");
    }

    protected override void OnGazeExitCallback()
    {
        Debug.Log($"Gaze exited panel: {gameObject.name}");
    }
}