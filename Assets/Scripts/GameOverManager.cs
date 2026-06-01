using UnityEngine;
using UnityEngine.UI;
using System.Globalization;
using System.Collections; // WICHTIG für den Jumpscare-Timer
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance { get; private set; }

    [Tooltip("Wenn true wird Time.timeScale auf 0 gesetzt bei GameOver.")]
    public bool pauseTimeOnGameOver = true;

    [Tooltip("Optional: Referenz auf das UI-Text Element named 'Hinweistext'. Falls leer, wird GameObject.Find versucht.")]
    public Text hintText;
    public Font myFont;
    
    [Tooltip("Wie lange der Spieler in das Gesicht des Engels starren muss, bevor der 'You Died'-Text kommt.")]
    public float jumpscareDuration = 1.5f;

    [Tooltip("Wie lange der 'You Died'-Text alleine stehen bleibt, bevor die Statistiken eingeblendet werden.")]
    public float youDiedScreenDuration = 2.0f;

    private bool gameOverTriggered = false;
    private int destroyedCount = 0;
    private float startRealtime = 0f;
    
    public AudioSource gameoverSound;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this.gameObject);
        else Instance = this;
        startRealtime = Time.realtimeSinceStartup;

        var col = GetComponent<Collider>();
        if (col == null)
            Debug.LogWarning("GameOverManager: kein Collider vorhanden.");
        else
            col.isTrigger = true;

        if (hintText == null)
        {
            var go = GameObject.Find("Hinweistext");
            if (go != null)
                hintText = go.GetComponent<Text>();
        }

        if (hintText != null)
            hintText.gameObject.SetActive(false);
    }

    public void OnEnemyDestroyed()
    {
        destroyedCount++;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryTriggerGameOver(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryTriggerGameOver(collision.collider);
    }

    public static void ReportCollision(Collider other)
    {
        if (Instance == null) return;
        Instance.TryTriggerGameOver(other);
    }

    private void TryTriggerGameOver(Collider other)
    {
        if (gameOverTriggered) return;
        if (other == null) return;

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy == null) enemy = other.GetComponentInParent<Enemy>();
        if (enemy == null) enemy = other.GetComponentInChildren<Enemy>();

        if (enemy == null && other.attachedRigidbody != null)
            enemy = other.attachedRigidbody.gameObject.GetComponentInParent<Enemy>();

        if (enemy == null && other.transform != null)
            enemy = other.transform.root.GetComponent<Enemy>();

        if (enemy == null) return;

        TriggerGameOver(enemy);
    }

    private void TriggerGameOver(Enemy killerEnemy)
    {
        gameOverTriggered = true;
        gameoverSound.Play();
        StartCoroutine(GameOverSequence(killerEnemy));
    }

    // =========================================================================
    // DIE NEUE ZWEISTUFIGE GAME OVER SEQUENZ
    // =========================================================================
    private IEnumerator GameOverSequence(Enemy killerEnemy)
    {
        // STUFE 1: Jumpscare (Kamera reißt zum Engel)
        GazeCameraController camController = FindObjectOfType<GazeCameraController>();
        if (camController != null && killerEnemy != null)
        {
            camController.TriggerGameOverSnap(killerEnemy.transform);
        }

        // Warte während des Jumpscares
        yield return new WaitForSeconds(jumpscareDuration);

        float elapsed = Time.realtimeSinceStartup - startRealtime;

        GameStats.AngelsDestroyed = destroyedCount;
        GameStats.SurvivalTime = elapsed;

        SceneManager.LoadScene("GameOverScreen");
        
    }

    private bool TryShowHintText(string message)
    {
        if (hintText == null) return false;

        hintText.text = message;
        var go = hintText.gameObject;
        go.SetActive(true);

        var canvas = hintText.GetComponentInParent<Canvas>();
        if (canvas != null) canvas.sortingOrder = 1000;

        var rt = hintText.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

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
        txt.font = myFont;
        txt.material = myFont.material;
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