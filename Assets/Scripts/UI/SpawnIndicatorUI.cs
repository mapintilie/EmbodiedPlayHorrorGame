using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SpawnIndicatorUI : MonoBehaviour
{
    [Tooltip("Optionaler Canvas. Wenn null, wird ein neuer Canvas erstellt.")]
    public Canvas parentCanvas;

    [Tooltip("Größe des Indikators in Pixeln.")]
    public Vector2 sizePixels = new Vector2(48f, 48f);

    [Tooltip("Farbe des Indikators.")]
    public Color indicatorColor = Color.red;

    [Tooltip("Soll der Indikator der Weltposition folgen (wenn ein Transform übergeben wurde)?")]
    public bool followTransform = true;

    [Tooltip("Standarddauer (Sekunden), wie lange der Indikator angezeigt wird.")]
    public float defaultDuration = 2f;

    [Tooltip("Abstand vom Bildschirmrand in Pixeln.")]
    public float screenMargin = 20f;

    private RectTransform canvasRect;
    private RectTransform indicatorRect;
    private Image indicatorImage;
    private Coroutine hideCoroutine;
    private Transform trackedTransform;
    private Vector3 trackedWorldPos;
    private Camera mainCam;

    private void Awake()
    {
        mainCam = Camera.main;
        EnsureCanvasAndIndicator();
    }

    private void Update()
    {
        if (indicatorImage == null || !indicatorImage.enabled) return;

        if (trackedTransform != null && followTransform)
            UpdatePosition(trackedTransform.position);
        else if (trackedTransform == null)
            UpdatePosition(trackedWorldPos);
    }

    private void EnsureCanvasAndIndicator()
    {
        if (parentCanvas == null)
        {
            GameObject cgo = new GameObject("SpawnIndicator_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            parentCanvas = cgo.GetComponent<Canvas>();
            parentCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            parentCanvas.sortingOrder = 1000;
        }

        canvasRect = parentCanvas.transform as RectTransform;

        if (indicatorImage == null)
        {
            GameObject igo = new GameObject("SpawnIndicator_Image", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            igo.transform.SetParent(parentCanvas.transform, false);
            indicatorRect = igo.GetComponent<RectTransform>();
            indicatorImage = igo.GetComponent<Image>();

            indicatorRect.sizeDelta = sizePixels;
            indicatorRect.anchorMin = new Vector2(0.5f, 0.5f);
            indicatorRect.anchorMax = new Vector2(0.5f, 0.5f);
            indicatorRect.pivot = new Vector2(0.5f, 0.5f);

            indicatorImage.color = indicatorColor;
            indicatorImage.raycastTarget = false;
            indicatorImage.enabled = false;
        }
    }

    private void UpdatePosition(Vector3 worldPos)
    {
        if (mainCam == null) mainCam = Camera.main;
        Vector3 screenPoint = (mainCam != null) ? mainCam.WorldToScreenPoint(worldPos) : new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);

        // Wenn hinter der Kamera, clamp auf Bildschirmrand (optional)
        if (screenPoint.z < 0f)
        {
            screenPoint.x = Screen.width * 0.5f;
            screenPoint.y = Screen.height * 0.5f;
        }

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera, out localPoint);

        indicatorRect.anchoredPosition = localPoint;
    }

    // Public API

    // Original: Zeigt den Indikator an der Weltposition des Spawn-Transforms für duration Sekunden.
    public void ShowAt(Transform spawnPoint, float duration = -1f)
    {
        if (spawnPoint == null) return;
        if (duration <= 0f) duration = defaultDuration;

        EnsureCanvasAndIndicator();
        trackedTransform = spawnPoint;
        trackedWorldPos = spawnPoint.position;
        indicatorRect.sizeDelta = sizePixels;
        indicatorImage.color = indicatorColor;
        indicatorImage.enabled = true;

        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(HideAfter(duration));
        UpdatePosition(trackedWorldPos);
    }

    // Neue Methode: Positioniert den UI-Indikator basierend auf dem Namen des SpawnPoints.
    // "left" -> links, "right" -> rechts, "back" -> unten, "front" -> oben
    public void ShowAtSpawnByName(Transform spawnPoint, float duration = -1f)
    {
        if (spawnPoint == null) return;
        if (duration <= 0f) duration = defaultDuration;

        EnsureCanvasAndIndicator();

        // Wir wollen eine feste Bildschirmposition, also nicht der Weltposition folgen.
        trackedTransform = null;
        trackedWorldPos = spawnPoint.position;

        indicatorRect.sizeDelta = sizePixels;
        indicatorImage.color = indicatorColor;
        indicatorImage.enabled = true;

        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(HideAfter(duration));

        Vector2 anchored = ComputeAnchoredPositionForSpawnName(spawnPoint.name, spawnPoint.position);
        indicatorRect.anchoredPosition = anchored;
    }

    // Zeigt den Indikator an einer Weltposition (wenn kein Transform verfügbar).
    public void ShowAtWorldPosition(Vector3 worldPos, float duration = -1f)
    {
        if (duration <= 0f) duration = defaultDuration;

        EnsureCanvasAndIndicator();
        trackedTransform = null;
        trackedWorldPos = worldPos;
        indicatorRect.sizeDelta = sizePixels;
        indicatorImage.color = indicatorColor;
        indicatorImage.enabled = true;

        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(HideAfter(duration));
        UpdatePosition(worldPos);
    }

    public void Hide()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        if (indicatorImage != null)
            indicatorImage.enabled = false;

        trackedTransform = null;
    }

    private IEnumerator HideAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        Hide();
    }

    // Hilfsmethode: berechnet die anchoredPosition (Canvas‑Local) für einen SpawnPoint‑Namen.
    private Vector2 ComputeAnchoredPositionForSpawnName(string spawnName, Vector3 fallbackWorldPos)
    {
        if (canvasRect == null)
            EnsureCanvasAndIndicator();

        Rect rect = canvasRect.rect;
        float halfW = rect.width * 0.5f;
        float halfH = rect.height * 0.5f;
        float margin = screenMargin + Mathf.Max(sizePixels.x, sizePixels.y) * 0.5f;

        string n = (spawnName ?? string.Empty).ToLowerInvariant();

        if (n.Contains("left"))
            return new Vector2(-halfW + margin, 0f);
        if (n.Contains("right"))
            return new Vector2(halfW - margin, 0f);
        if (n.Contains("back"))
            return new Vector2(0f, -halfH + margin);
        if (n.Contains("front"))
            return new Vector2(0f, halfH - margin);

        // Fallback: mappe anhand der Weltposition auf Bildschirmposition
        if (mainCam == null) mainCam = Camera.main;
        Vector3 screenPoint = (mainCam != null) ? mainCam.WorldToScreenPoint(fallbackWorldPos) : new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        if (screenPoint.z < 0f)
        {
            screenPoint.x = Screen.width * 0.5f;
            screenPoint.y = Screen.height * 0.5f;
        }

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera, out localPoint);
        return localPoint;
    }
}