// ============================================================================
// GazeDebugVisualizer.cs 
// ============================================================================

using UnityEngine;

public class GazeDebugVisualizer : MonoBehaviour
{
    [Header("Visualisierung")]
    [SerializeField] private float cursorSize = 30f;
    [SerializeField] private Color cursorColor = new Color(0f, 1f, 0f, 0.7f);
    [SerializeField] private bool onlyInEditor = false;

    [Header("Status-Anzeige")]
    [SerializeField] private bool showStatusText = true;

    private Texture2D cursorTexture;

    private void Awake()
    {
        int size = 64;
        cursorTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float radius = size * 0.5f;
        Color transparent = new Color(0, 0, 0, 0);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(
                    new Vector2(x, y), new Vector2(radius, radius));

                if (dist < radius * 0.8f)
                {
                    float alpha = 1f - (dist / (radius * 0.8f));
                    cursorTexture.SetPixel(x, y,
                        new Color(cursorColor.r, cursorColor.g,
                                  cursorColor.b, alpha * cursorColor.a));
                }
                else
                {
                    cursorTexture.SetPixel(x, y, transparent);
                }
            }
        }
        cursorTexture.Apply();
    }

    private void OnGUI()
    {
        #if !UNITY_EDITOR
        if (onlyInEditor) return;
        #endif

        if (TobiiManager.Instance == null) return;

        if (showStatusText)
        {
            GUILayout.BeginArea(new Rect(10, 10, 450, 250));
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 14;
            style.normal.textColor = Color.white;
            style.richText = true;

            string status = TobiiManager.Instance.IsTrackerConnected
                ? "<color=green>Verbunden</color>"
                : "<color=red>Nicht verbunden</color>";

            string apiReady = TobiiManager.Instance.IsApiReady
                ? "<color=green>Ja</color>"
                : "<color=yellow>Nein</color>";

            string presence = TobiiManager.Instance.IsUserPresent
                ? "<color=green>Ja</color>"
                : "<color=yellow>Nein</color>";

            string gazeValid = TobiiManager.Instance.HasValidGazeData
                ? "<color=green>Ja</color>"
                : "<color=red>Nein</color>";

            string gazedObj = TobiiManager.Instance.GazedObject != null
                ? $"<color=cyan>{TobiiManager.Instance.GazedObject.name}</color>"
                : "<color=gray>—</color>";

            GUILayout.Label($"Tobii Tracker: {status}", style);
            GUILayout.Label($"API bereit: {apiReady}", style);
            GUILayout.Label($"Benutzer erkannt: {presence}", style);
            GUILayout.Label($"Gaze gültig: {gazeValid}", style);
            GUILayout.Label($"Viewport: {TobiiManager.Instance.GazePointViewport:F3}", style);
            GUILayout.Label($"Angesehenes Objekt: {gazedObj}", style);
            GUILayout.EndArea();
        }

        // Gaze-Cursor zeichnen
        if (TobiiManager.Instance.HasValidGazeData)
        {
            Vector2 vp = TobiiManager.Instance.GazePointViewport;

            float screenX = vp.x * Screen.width;
            float screenY = (1f - vp.y) * Screen.height;

            // Cursor-Farbe ändert sich wenn ein Objekt getroffen wird
            Color drawColor = TobiiManager.Instance.GazedObject != null
                ? Color.cyan
                : cursorColor;

            GUI.color = drawColor;

            Rect cursorRect = new Rect(
                screenX - cursorSize * 0.5f,
                screenY - cursorSize * 0.5f,
                cursorSize,
                cursorSize);

            GUI.DrawTexture(cursorRect, cursorTexture);
            GUI.color = Color.white;
        }
    }
}
