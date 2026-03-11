// ============================================================================
// DemoSceneSetup.cs
// Editor-Script: Erstellt automatisch eine Demo-Szene mit
// farbwechselnden Sphären.
// Lege diese Datei unter Assets/Scripts/Tobii/Editor/ ab.
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class DemoSceneSetup : EditorWindow
{
    [MenuItem("Tobii/Demo-Szene erstellen")]
    public static void CreateDemoScene()
    {
        // ====================================================================
        // 1) TOBII MANAGER
        // ====================================================================
        GameObject managerObj = new GameObject("TobiiManager");
        managerObj.AddComponent<TobiiManager>();
        managerObj.AddComponent<GazeDebugVisualizer>();

        Debug.Log("TobiiManager erstellt.");

        // ====================================================================
        // 2) KAMERA
        // ====================================================================
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.position = new Vector3(0, 1.5f, -5f);
            mainCam.transform.LookAt(Vector3.zero);
        }

        // ====================================================================
        // 3) BELEUCHTUNG
        // ====================================================================
        GameObject lightObj = new GameObject("Directional Light");
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
        lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // ====================================================================
        // 4) BODEN
        // ====================================================================
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Boden";
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(2, 1, 2);

        // ====================================================================
        // 5) GAZE-SPHÄREN
        // ====================================================================

        // Farb-Konfigurationen für verschiedene Sphären
        Color[][] colorSets = new Color[][]
        {
            // normalColor,              gazeColor,                focusedColor
            new[] { new Color(0.3f, 0.5f, 0.8f), new Color(1f, 0.7f, 0f),   new Color(1f, 0.2f, 0.2f) },  // Blau → Orange → Rot
            new[] { new Color(0.2f, 0.7f, 0.3f), new Color(1f, 1f, 0f),      new Color(0f, 1f, 1f)     },  // Grün → Gelb → Cyan
            new[] { new Color(0.7f, 0.3f, 0.7f), new Color(1f, 0.5f, 0.8f),  new Color(1f, 1f, 1f)     },  // Lila → Rosa → Weiß
            new[] { new Color(0.8f, 0.4f, 0.1f), new Color(0.2f, 0.8f, 1f),  new Color(0f, 0.4f, 1f)   },  // Orange → Hellblau → Blau
            new[] { new Color(0.5f, 0.5f, 0.5f), new Color(1f, 0.9f, 0.3f),  new Color(1f, 0.5f, 0f)   },  // Grau → Gold → Orange
        };

        string[] names = {
            "Sphäre_Blau", "Sphäre_Grün", "Sphäre_Lila",
            "Sphäre_Orange", "Sphäre_Grau"
        };

        // Sphären in einem Halbkreis anordnen
        float radius = 3f;
        for (int i = 0; i < 5; i++)
        {
            float angle = Mathf.Lerp(-60f, 60f, i / 4f) * Mathf.Deg2Rad;
            float x = Mathf.Sin(angle) * radius;
            float z = Mathf.Cos(angle) * radius - 1f;

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = names[i];
            sphere.transform.position = new Vector3(x, 1.5f, z);
            sphere.transform.localScale = Vector3.one * 0.8f;

            // Collider ist schon vorhanden (Sphere Primitive hat SphereCollider)

            // GazeColorSphere hinzufügen
            GazeColorSphere gazeColor = sphere.AddComponent<GazeColorSphere>();

            // Farben per SerializedObject setzen
            SerializedObject so = new SerializedObject(gazeColor);
            so.FindProperty("normalColor").colorValue = colorSets[i][0];
            so.FindProperty("gazeColor").colorValue = colorSets[i][1];
            so.FindProperty("focusedColor").colorValue = colorSets[i][2];
            so.ApplyModifiedProperties();

            Debug.Log($"Sphäre '{names[i]}' erstellt bei Position ({x:F1}, 1.5, {z:F1})");
        }

        // ====================================================================
        // 6) HINWEISTEXT
        // ====================================================================
        GameObject canvasObj = new GameObject("UI_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();

        GameObject textObj = new GameObject("Hinweistext");
        textObj.transform.SetParent(canvasObj.transform);
        UnityEngine.UI.Text text = textObj.AddComponent<UnityEngine.UI.Text>();
        text.text = "Schau auf eine Sphäre um ihre Farbe zu ändern!";
        text.fontSize = 24;
        text.color = Color.white;
        text.alignment = TextAnchor.UpperCenter;

        RectTransform rt = textObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.2f, 0.9f);
        rt.anchorMax = new Vector2(0.8f, 0.98f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Debug.Log("=== Tobii Demo-Szene erfolgreich erstellt! ===");
        Debug.Log("Starte das Spiel und schau auf die Sphären.");
    }
}
#endif
