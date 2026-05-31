using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI-Overlay mit schwarzem Bildschirm und einem durchsichtigen Kreis.
/// </summary>
public class ViewRestriction : GazeInteractable
{
    [Header("Visual Settings")]
    [Tooltip("Material using the HoleMask shader (optional). If null the shader will be found by name.")]
    public Material maskMaterial;

    [Tooltip("Radius des sichtbaren Kreises in Pixeln.")]
    public float holeRadiusPixels = 300;

    [Tooltip("Farbe des Overlays (Alpha steuert die Abdunkelung).")]
    public Color overlayColor = new Color(0f, 0f, 0f, 0.8f);

    [Header("Tracking Settings")]
    [Tooltip("How smoothly the hole follows the gaze (prevents eye-tracking jitter). Higher is faster.")]
    public float followSpeed = 15f;

    [Tooltip("Aktiviere Debug-Logs.")]
    public bool debugGaze = false;

    private Material runtimeMaterial;
    private Canvas canvas;
    private RawImage rawImage;
    private Vector2 currentScreenPosition;

    void Start()
    {
        CreateOverlay();
        ApplyMaterialProperties();
        currentScreenPosition = new Vector2(Screen.width / 2f, Screen.height / 2f); // Start in center
    }

    void OnDestroy()
    {
        if (runtimeMaterial != null)
            Destroy(runtimeMaterial);
    }

    void Update()
    {
        // 1. Hole die Zielposition (Maus oder Eye Tracker)
        Vector2 targetPosition = GetGazePosition();

        // 2. Interpoliere die Position, um das natürliche Zittern der Augen auszugleichen
        currentScreenPosition = Vector2.Lerp(currentScreenPosition, targetPosition, Time.deltaTime * followSpeed);

        // 3. Aktualisiere den Shader
        UpdateMaterialWithPosition(currentScreenPosition);
    }

    /// <summary>
    /// Hier wird die X/Y Bildschirmkoordinate abgerufen.
    /// </summary>
    private Vector2 GetGazePosition()
    {
        // 1. Get the 3D gaze data from the Tobii XR SDK
        var eyeData = Tobii.XR.TobiiXR.GetEyeTrackingData(Tobii.XR.TobiiXR_TrackingSpace.World);
        
        // 2. Check if the eye tracker can currently see your eyes
        if (eyeData.GazeRay.IsValid)
        {
            // 3. Take the 3D direction you are looking, project it 10 meters forward...
            Vector3 lookPointIn3D = eyeData.GazeRay.Origin + (eyeData.GazeRay.Direction * 10f);
            
            // 4. ...and translate that 3D point into a 2D pixel coordinate on your screen/camera
            Vector2 screenPos = Camera.main.WorldToScreenPoint(lookPointIn3D);
            
            return screenPos;
        }

        // Fallback: If you blink, look away, or the tracker disconnects, use the mouse
        return Input.mousePosition; 
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
            rawImage.color = Color.white;
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

    private void UpdateMaterialWithPosition(Vector2 screenPosition)
    {
        if (runtimeMaterial == null)
            return;

        float normalizedRadius = holeRadiusPixels / Mathf.Max(1f, Screen.height);
        runtimeMaterial.SetFloat("_HoleRadius", normalizedRadius);
        runtimeMaterial.SetColor("_OverlayColor", overlayColor);
        runtimeMaterial.SetFloat("_Aspect", (float)Screen.width / Mathf.Max(1f, Screen.height));

        Vector2 center = new Vector2(screenPosition.x / Screen.width, screenPosition.y / Screen.height);
        center.x = Mathf.Clamp01(center.x);
        center.y = Mathf.Clamp01(center.y);

        runtimeMaterial.SetVector("_HoleCenter", new Vector4(center.x, center.y, 0f, 0f));

        if (debugGaze)
            Debug.Log($"[ViewRestriction] Target -> screen={screenPosition} normalized={center}");
    }

    // Leere Callbacks, um die Basisklasse nicht zu stören (da wir die kontinuierliche Position brauchen, nicht die Trigger)
    protected override void OnGazeEnterCallback() { }
    protected override void OnGazeStayCallback() { }
    protected override void OnGazeFocusedCallback() { }
    protected override void OnGazeExitCallback() { }
}