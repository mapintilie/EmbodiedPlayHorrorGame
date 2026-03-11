// ============================================================================
// GazeColorSphere.cs
// ============================================================================

using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class GazeColorSphere : GazeInteractable
{
    // ========================================================================
    // KONFIGURATION
    // ========================================================================

    [Header("Farben")]
    [SerializeField] private Color normalColor = new Color(0.3f, 0.5f, 0.8f);
    [SerializeField] private Color gazeColor = new Color(1.0f, 0.7f, 0.0f);
    [SerializeField] private Color focusedColor = new Color(1.0f, 0.2f, 0.2f);

    [Header("Übergangs-Einstellungen")]
    [SerializeField] private float colorTransitionSpeed = 5f;
    [SerializeField] private bool pulseWhenFocused = true;
    [SerializeField] private float pulseIntensity = 0.1f;
    [SerializeField] private float pulseSpeed = 3f;

    // ========================================================================
    // PRIVATE FELDER
    // ========================================================================

    private Material instanceMaterial;
    private Color currentColor;
    private Color targetColor;
    private Vector3 originalScale;

    // ========================================================================
    // UNITY LIFECYCLE
    // ========================================================================

    private void Awake()
    {
        // Eigene Material-Instanz pro Sphäre
        instanceMaterial = GetComponent<Renderer>().material;
        originalScale = transform.localScale;

        // Startfarbe setzen
        currentColor = normalColor;
        targetColor = normalColor;
        SetColor(normalColor);
    }

    protected override void Update()
    {
        base.Update();

        // Sanfter Farbübergang
        currentColor = Color.Lerp(currentColor, targetColor,
            Time.deltaTime * colorTransitionSpeed);
        SetColor(currentColor);

        // Puls-Effekt
        if (pulseWhenFocused && IsFocused)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseIntensity;
            transform.localScale = originalScale * pulse;
        }
        else
        {
            transform.localScale = Vector3.Lerp(transform.localScale,
                originalScale, Time.deltaTime * colorTransitionSpeed);
        }
    }

    private void OnDestroy()
    {
        if (instanceMaterial != null)
            Destroy(instanceMaterial);
    }

    // ========================================================================
    // GAZE CALLBACKS
    // ========================================================================

    protected override void OnGazeEnterCallback()
    {
        targetColor = gazeColor;
    }

    protected override void OnGazeFocusedCallback()
    {
        targetColor = focusedColor;
    }

    protected override void OnGazeExitCallback()
    {
        targetColor = normalColor;
    }

    // ========================================================================
    // FARBE SETZEN 
    // ========================================================================

    private void SetColor(Color color)
    {
        if (instanceMaterial == null) return;

        // URP / HDRP
        if (instanceMaterial.HasProperty("_BaseColor"))
            instanceMaterial.SetColor("_BaseColor", color);

        // Built-in Standard
        if (instanceMaterial.HasProperty("_Color"))
            instanceMaterial.SetColor("_Color", color);

        // material.color setzt intern auch die Hauptfarbe
        instanceMaterial.color = color;
    }
}
