using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI-Overlay mit schwarzem Bildschirm und einem durchsichtigen Kreis an der Mausposition.
/// Nutzt ausschließlich die Mausposition; ohne Material wird die Overlay-Farbe direkt gesetzt.
/// </summary>
public class ViewRestriction : GazeInteractable
{
    [Tooltip("Material using the HoleMask shader (optional). If null the shader will be found by name.")]
    public Material maskMaterial;

    [Tooltip("Radius des sichtbaren Kreises in Pixeln.")]
    public float holeRadiusPixels = 300;

    [Tooltip("Farbe des Overlays (Alpha steuert die Abdunkelung).")]
    public Color overlayColor = new Color(0f, 0f, 0f, 0.8f);

    [Tooltip("Aktiviere Debug-Logs.")]
    public bool debugGaze = false;

    private Material runtimeMaterial;
    private Canvas canvas;
    private RawImage rawImage;

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
        Vector2 mouse = Input.mousePosition;
        UpdateMaterialWithMouse(mouse);
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
            else Debug.LogWarning("[ViewRestriction] Shader 'UI/HoleMask' nicht gefunden. Benutze overlayColor-Fallback.");
        }

        runtimeMaterial = mat;

        if (runtimeMaterial != null)
        {
            rawImage.material = runtimeMaterial;
            rawImage.color = Color.white; // Material steuert Overlay-Farbe/Alpha
        }
        else
        {
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

    private void UpdateMaterialWithMouse(Vector2 mousePosition)
    {
        if (runtimeMaterial == null)
            return;

        // aktualisiere Radius in jedem Frame (falls Fenstergröße sich ändert)
        float normalizedRadius = holeRadiusPixels / Mathf.Max(1f, Screen.height);
        runtimeMaterial.SetFloat("_HoleRadius", normalizedRadius);
        runtimeMaterial.SetColor("_OverlayColor", overlayColor);
        runtimeMaterial.SetFloat("_Aspect", (float)Screen.width / Mathf.Max(1f, Screen.height));

        Vector2 center = new Vector2(mousePosition.x / Screen.width, mousePosition.y / Screen.height);
        center.x = Mathf.Clamp01(center.x);
        center.y = Mathf.Clamp01(center.y);

        runtimeMaterial.SetVector("_HoleCenter", new Vector4(center.x, center.y, 0f, 0f));

        if (debugGaze)
            Debug.Log($"[ViewRestriction] Mouse -> screen={mousePosition} normalized={center}");
    }

    // Optional: leere Gaze-Callbacks, um Basisklasse nicht zu stören
    protected override void OnGazeEnterCallback() { }
    protected override void OnGazeStayCallback() { }
    protected override void OnGazeFocusedCallback() { }
    protected override void OnGazeExitCallback() { }
}