// csharp
using UnityEngine;
using UnityEngine.UI;
using System.Globalization;

[RequireComponent(typeof(Collider))]
public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance { get; private set; }

    [Tooltip("Wenn true wird Time.timeScale auf 0 gesetzt bei GameOver.")]
    public bool pauseTimeOnGameOver = true;

    [Tooltip("Optional: Referenz auf das UI-Text Element named 'Hinweistext'. Falls leer, wird GameObject.Find versucht.")]
    public Text hintText;

    private bool gameOverTriggered = false;
    private int destroyedCount = 0;
    private float startRealtime = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this.gameObject);
        else Instance = this;
        startRealtime = Time.realtimeSinceStartup;

        var col = GetComponent<Collider>();
        if (col == null)
            Debug.LogWarning("GameOverManager: kein Collider vorhanden. Attach an Player Collider or call GameOverManager.ReportCollision manually.");
        else
            col.isTrigger = true;

        // Wenn im Inspector nichts gesetzt wurde, versuche das Element per Name zu finden.
        if (hintText == null)
        {
            var go = GameObject.Find("Hinweistext");
            if (go != null)
                hintText = go.GetComponent<Text>();
        }

        // Stelle sicher, dass das Hinweistext-Element standardmäßig verborgen ist.
        if (hintText != null)
            hintText.gameObject.SetActive(false);
    }

    // Aufruf durch EnemySpawner wenn ein Enemy gestorben ist
    public void OnEnemyDestroyed()
    {
        destroyedCount++;
    }

    // Attach an das Player-Collider-GameObject.
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"GameOverManager: OnTriggerEnter with {other.name}", this);
        TryTriggerGameOver(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"GameOverManager: OnCollisionEnter with {collision.collider.name}", this);
        TryTriggerGameOver(collision.collider);
    }

    // Statische Hilfsmethode: andere Scripts können Kollisionen melden, falls GameOverManager nicht am Player hängt.
    public static void ReportCollision(Collider other)
    {
        if (Instance == null)
        {
            Debug.LogWarning("GameOverManager.ReportCollision called but no Instance present.");
            return;
        }
        Instance.TryTriggerGameOver(other);
    }

    private void TryTriggerGameOver(Collider other)
    {
        if (gameOverTriggered) return;
        if (other == null) return;

        Enemy enemy = null;

        enemy = other.GetComponent<Enemy>();
        if (enemy == null) enemy = other.GetComponentInParent<Enemy>();
        if (enemy == null) enemy = other.GetComponentInChildren<Enemy>();

        if (enemy == null && other.attachedRigidbody != null)
            enemy = other.attachedRigidbody.gameObject.GetComponentInParent<Enemy>();

        if (enemy == null && other.transform != null)
            enemy = other.transform.root.GetComponent<Enemy>();

        if (enemy == null)
        {
            Debug.Log($"GameOverManager: Kollidierendes Objekt '{other.name}' ist kein Enemy (keine Enemy-Komponente gefunden).", this);
            return;
        }

        Debug.Log($"GameOverManager: Enemy '{enemy.name}' getroffen -> Game Over auslösen.", this);
        TriggerGameOver();
    }

    private void TriggerGameOver()
    {
        gameOverTriggered = true;

        float elapsed = Time.realtimeSinceStartup - startRealtime;
        string timeStr = FormatTime(elapsed);

        string message = $"Game Over\n\nAngels destroyed: {destroyedCount}\nTime: {timeStr}";

        // Versuche erstes das vorhandene Hinweistext-Element zu benutzen, ansonsten Fallback auf das Overlay.
        if (!TryShowHintText(message))
            CreateBlackOverlayWithText(message);

        if (pauseTimeOnGameOver)
            Time.timeScale = 0f;
    }

    private bool TryShowHintText(string message)
    {
        if (hintText == null) return false;

        hintText.text = message;
        var go = hintText.gameObject;
        go.SetActive(true);

        // Bring das UI-Element in den Vordergrund (Canvas-SortingOrder erhöhen falls vorhanden)
        var canvas = hintText.GetComponentInParent<Canvas>();
        if (canvas != null)
            canvas.sortingOrder = 1000;

        // Optional: das RectTransform auf Fullscreen stretchen, damit der Text zentral über dem ganzen Bildschirm liegt.
        var rt = hintText.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // Stelle sicher, dass der Text zentriert und gut lesbar ist
        hintText.alignment = TextAnchor.MiddleCenter;
        hintText.color = Color.white;

        return true;
    }

    private string FormatTime(float seconds)
    {
        int mins = (int)(seconds / 60f);
        float sec = seconds - mins * 60;
        return string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00.00}", mins, sec);
    }

    private void CreateBlackOverlayWithText(string message)
    {
        var canvasGO = new GameObject("GameOverCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var bgGO = new GameObject("BlackBG");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var img = bgGO.AddComponent<Image>();
        img.color = Color.black;
        var rect = img.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var textGO = new GameObject("GameOverText");
        textGO.transform.SetParent(canvasGO.transform, false);
        var txt = textGO.AddComponent<Text>();
        txt.alignment = TextAnchor.MiddleCenter;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Truncate;
        txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.fontSize = 36;
        txt.color = Color.white;
        txt.text = message;
        var tr = txt.rectTransform;
        tr.anchorMin = new Vector2(0.1f, 0.1f);
        tr.anchorMax = new Vector2(0.9f, 0.9f);
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;
    }
}