using UnityEngine;
using UnityEngine.UI;
using System.Globalization;

[RequireComponent(typeof(Collider))]
public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance { get; private set; }

    [Tooltip("Wenn true wird Time.timeScale auf 0 gesetzt bei GameOver.")]
    public bool pauseTimeOnGameOver = true;

    private bool gameOverTriggered = false;
    private int destroyedCount = 0;
    private float startRealtime = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this.gameObject);
        else Instance = this;
        startRealtime = Time.realtimeSinceStartup;
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

    private void TryTriggerGameOver(Collider other)
    {
        if (gameOverTriggered) return;
        if (other == null) return;

        // Versuche mehrere Wege, ein Enemy zu finden
        Enemy enemy = null;

        // direktes oder parent/child durchsuchen
        enemy = other.GetComponent<Enemy>();
        if (enemy == null) enemy = other.GetComponentInParent<Enemy>();
        if (enemy == null) enemy = other.GetComponentInChildren<Enemy>();

        // falls Rigidbody vorhanden, prüfe das Root-GameObject
        if (enemy == null && other.attachedRigidbody != null)
            enemy = other.attachedRigidbody.gameObject.GetComponentInParent<Enemy>();

        // fallback auf root
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

        CreateBlackOverlayWithText($"Game Over\n\nAngels destroyed: {destroyedCount}\nTime: {timeStr}");

        if (pauseTimeOnGameOver)
            Time.timeScale = 0f;
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