// csharp
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI-Overlay mit schwarzem Bildschirm und einem durchsichtigen Kreis an der Blickposition.
/// Vererbt GazeInteractable (nutzt dessen Callbacks, zeigt Overlay unabhängig davon).
/// </summary>
public class ViewRestriction : GazeInteractable
{
    [Tooltip("Material using the HoleMask shader (optional). If null the shader will be found by name.")]
    public Material maskMaterial;

    [Tooltip("Radius des sichtbaren Kreises in Pixeln (ca. 3-4 cm = ~100-150px je nach DPI).")]
    public float holeRadiusPixels = 120f;

    [Tooltip("Farbe des Overlays (Alpha steuert die Abdunkelung).")]
    public Color overlayColor = Color.black;

    [Tooltip("Nutze Mausposition im Editor zur einfachen Tests.")]
    public bool useMouseInEditor = false;

    private Material runtimeMaterial;
    private Canvas canvas;
    private RawImage rawImage;

    // externe Steuerung der Blickposition (z.B. EyeTracker)
    private Vector2 externalGazePos;
    private bool hasExternalGaze = false;

    void Start()
    {
        CreateOverlay();
        ApplyMaterialProperties();
    }

    void OnDestroy()
    {
        if (runtimeMaterial != null)
            Destroy(runtimeMaterial);
    }

    void Update()
    {
        Vector2 gaze = DetermineGazeScreenPosition();
        UpdateMaterialWithGaze(gaze);
    }

    // Extern aufrufbar: setzt Blickposition in Bildschirmpixeln (0..Screen.width,height)
    public void SetGazeScreenPosition(Vector2 screenPos)
    {
        externalGazePos = screenPos;
        hasExternalGaze = true;
    }

    public void ClearExternalGaze()
    {
        hasExternalGaze = false;
    }

    private Vector2 DetermineGazeScreenPosition()
    {
        if (hasExternalGaze)
        {
            // Wenn ExternalGaze als 0..1 geliefert wird, in Pixel umwandeln.
            if (externalGazePos.x >= 0f && externalGazePos.x <= 1f &&
                externalGazePos.y >= 0f && externalGazePos.y <= 1f)
            {
                return new Vector2(externalGazePos.x * Screen.width, externalGazePos.y * Screen.height);
            }
            // ansonsten bereits in Pixeln
            return externalGazePos;
        }

#if UNITY_EDITOR
        if (useMouseInEditor)
            return Input.mousePosition;
#endif

        return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
    }

    private void CreateOverlay()
{
    // Canvas
    GameObject canvasGO = new GameObject("ViewRestriction_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
    canvas = canvasGO.GetComponent<Canvas>();
    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
    canvas.sortingOrder = 1000;

    // RawImage full screen
    GameObject imgGO = new GameObject("ViewRestriction_Overlay", typeof(RawImage));
    imgGO.transform.SetParent(canvas.transform, false);
    rawImage = imgGO.GetComponent<RawImage>();
    RectTransform rt = rawImage.rectTransform;
    rt.anchorMin = Vector2.zero;
    rt.anchorMax = Vector2.one;
    rt.sizeDelta = Vector2.zero;
    rt.anchoredPosition = Vector2.zero;

    // Material: instanziere Runtime-Kopie
    Material mat = null;
    if (maskMaterial != null)
        mat = Instantiate(maskMaterial);
    else
    {
        Shader s = Shader.Find("UI/HoleMask");
        if (s != null) mat = new Material(s);
        else Debug.LogError("[ViewRestriction] Shader 'UI/HoleMask' nicht gefunden. Bitte Asset hinzufügen oder maskMaterial im Inspector setzen.");
    }

    runtimeMaterial = mat;

    if (runtimeMaterial != null)
    {
        rawImage.material = runtimeMaterial;
        rawImage.color = Color.white; // Material steuert Farbe/Alpha
    }
    else
    {
        // Fallback: kein Material -> benutze overlayColor direkt (verhindert weiße Fläche)
        rawImage.material = null;
        rawImage.color = overlayColor;
    }
}

    private void ApplyMaterialProperties()
    {
        if (runtimeMaterial == null)
        {
            if (rawImage != null)
                rawImage.color = overlayColor;
            return;
        }

        runtimeMaterial.SetColor("_OverlayColor", overlayColor);
        float normalizedRadius = holeRadiusPixels / Mathf.Max(1f, Screen.height);
        runtimeMaterial.SetFloat("_HoleRadius", normalizedRadius);
        runtimeMaterial.SetVector("_HoleCenter", new Vector4(0.5f, 0.5f, 0f, 0f));
        runtimeMaterial.SetFloat("_Aspect", (float)Screen.width / Mathf.Max(1f, Screen.height));
    }

    private void UpdateMaterialWithGaze(Vector2 screenPos)
    {
        if (runtimeMaterial == null)
            return;

        float normalizedRadius = holeRadiusPixels / Mathf.Max(1f, Screen.height);
        runtimeMaterial.SetFloat("_HoleRadius", normalizedRadius);
        runtimeMaterial.SetColor("_OverlayColor", overlayColor);
        runtimeMaterial.SetFloat("_Aspect", (float)Screen.width / Mathf.Max(1f, Screen.height));

        Vector2 center = new Vector2(screenPos.x / Screen.width, screenPos.y / Screen.height);
        runtimeMaterial.SetVector("_HoleCenter", new Vector4(center.x, center.y, 0f, 0f));
    }

    // Optional: Gaze callbacks (nicht zwingend notwendig; hier nur als Beispiel)
    protected override void OnGazeEnterCallback() { /* optional: visibility toggle */ }
    protected override void OnGazeStayCallback() { }
    protected override void OnGazeFocusedCallback() { }
    protected override void OnGazeExitCallback() { }
}