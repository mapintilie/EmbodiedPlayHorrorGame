// ============================================================================
// TobiiManager.cs 
// ============================================================================

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Tobii.GameIntegration.Net;

#if UNITY_EDITOR
using UnityEditor;
#endif

using TobiiGazePoint = Tobii.GameIntegration.Net.GazePoint;
using TobiiHeadPose = Tobii.GameIntegration.Net.HeadPose;

public class TobiiManager : MonoBehaviour
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    // ========================================================================
    // ÖFFENTLICHE DATEN
    // ========================================================================

    public static TobiiManager Instance { get; private set; }

    public Vector2 GazePointNormalized { get; private set; }
    public Vector2 GazePointViewport { get; private set; }
    public Vector3 HeadPosition { get; private set; }
    public Vector3 HeadRotation { get; private set; }
    public bool IsTrackerConnected { get; private set; }
    public bool IsUserPresent { get; private set; }
    public bool IsApiReady { get; private set; }
    public bool HasValidGazeData { get; private set; }
    public GameObject GazedObject { get; private set; }
    public RaycastHit LastGazeHit { get; private set; }

    // ========================================================================
    // KONFIGURATION
    // ========================================================================

    [Header("Tobii Konfiguration")]
    [SerializeField] private string gameName = "MeinUnitySpiel";

    [Header("Gaze Stabilisierung")]
    [SerializeField] private float gazeGracePeriod = 0.15f;

    [Range(0f, 0.95f)]
    [SerializeField] private float gazeSmoothing = 0.3f;

    [SerializeField] private float maxRaycastDistance = 100f;

    // ========================================================================
    // PRIVATE FELDER
    // ========================================================================

    private static bool isDllLoaded = false;  // static! überlebt Play/Stop
    private float retryTimer = 0f;
    private float timeSinceLastGaze = 999f;
    private Vector2 smoothedGazeViewport;
    private bool everConnected = false;

    // ========================================================================
    // EDITOR CALLBACK
    // ========================================================================

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    private static void RegisterEditorCallback()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // Wird aufgerufen BEVOR Play-Mode endet
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            DoShutdown();
            Debug.Log("[Tobii] Shutdown via Editor-Callback (ExitingPlayMode)");
        }
    }
#endif

    // ========================================================================
    // AWAKE
    // ========================================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        try
        {
            TobiiGameIntegrationApi.PrelinkAll();

            // Sicherheitshalber alten Zustand bereinigen
            try { TobiiGameIntegrationApi.Shutdown(); }
            catch (Exception) { }

            // Kurz warten ist nicht möglich, aber ein paar Frames
            // Update vor SetApplicationName kann helfen
            TobiiGameIntegrationApi.SetApplicationName(gameName);
            isDllLoaded = true;

            Debug.Log($"[Tobii] DLL geladen: {TobiiGameIntegrationApi.LoadedDll}");
            Debug.Log($"[Tobii] App Name: '{gameName}'");

            IsApiReady = TobiiGameIntegrationApi.IsApiInitialized();
            Debug.Log($"[Tobii] API bereit: {IsApiReady}");
        }
        catch (DllNotFoundException e)
        {
            Debug.LogError("[Tobii] DLL nicht gefunden!\n" + e.Message);
        }
        catch (Exception e)
        {
            Debug.LogError("[Tobii] Ladefehler: " + e.Message);
        }
    }

    // ========================================================================
    // UPDATE
    // ========================================================================

    private void Update()
    {
        if (!isDllLoaded) return;

        // 1) TGI updaten
        TobiiGameIntegrationApi.Update();

        // 2) API prüfen
        IsApiReady = TobiiGameIntegrationApi.IsApiInitialized();
        if (!IsApiReady) return;

        // 3) Verbindungsstatus
        IsTrackerConnected = TobiiGameIntegrationApi.IsTrackerConnected();
        IsUserPresent = TobiiGameIntegrationApi.IsPresent();

        // 4) Wenn nicht verbunden: TrackWindow alle 2 Sekunden
        if (!IsTrackerConnected)
        {
            retryTimer += Time.deltaTime;
            if (retryTimer >= 2f)
            {
                retryTimer = 0f;
                IntPtr hwnd = GetActiveWindow();
                TobiiGameIntegrationApi.TrackWindow(hwnd);
            }
        }
        else if (!everConnected)
        {
            everConnected = true;
            Debug.Log("[Tobii] Tracker verbunden!");

            var info = TobiiGameIntegrationApi.GetTrackerInfo();
            if (info != null)
                Debug.Log($"[Tobii] Gerät: {info.FriendlyName} ({info.ModelName})");
        }

        // 5) Gaze-Daten lesen
        TobiiGazePoint gazePoint;
        bool freshData = TobiiGameIntegrationApi.TryGetLatestGazePoint(out gazePoint);

        if (freshData)
        {
            timeSinceLastGaze = 0f;
            GazePointNormalized = new Vector2(gazePoint.X, gazePoint.Y);

            Vector2 rawViewport = new Vector2(
                (gazePoint.X + 1f) * 0.5f,
                (gazePoint.Y + 1f) * 0.5f
            );

            if (gazeSmoothing > 0f && HasValidGazeData)
                smoothedGazeViewport = Vector2.Lerp(rawViewport, smoothedGazeViewport, gazeSmoothing);
            else
                smoothedGazeViewport = rawViewport;

            GazePointViewport = smoothedGazeViewport;
        }
        else
        {
            timeSinceLastGaze += Time.deltaTime;
        }

        HasValidGazeData = (timeSinceLastGaze <= gazeGracePeriod);

        // 6) Zentraler Raycast
        GazedObject = null;
        if (HasValidGazeData && Camera.main != null)
        {
            Ray gazeRay = Camera.main.ViewportPointToRay(
                new Vector3(GazePointViewport.x, GazePointViewport.y, 0f));

            RaycastHit hit;
            if (Physics.Raycast(gazeRay, out hit, maxRaycastDistance))
            {
                GazedObject = hit.collider.gameObject;
                LastGazeHit = hit;
            }
        }

        // 7) Head-Tracking
        TobiiHeadPose headPose;
        if (TobiiGameIntegrationApi.TryGetLatestHeadPose(out headPose))
        {
            HeadPosition = new Vector3(
                headPose.Position.X, headPose.Position.Y, headPose.Position.Z);
            HeadRotation = new Vector3(
                headPose.Rotation.YawDegrees,
                headPose.Rotation.PitchDegrees,
                headPose.Rotation.RollDegrees);
        }
    }

    // ========================================================================
    // SHUTDOWN
    // ========================================================================

    private void OnDisable()
    {
        DoShutdown();
    }

    private void OnApplicationQuit()
    {
        DoShutdown();
    }

    private static void DoShutdown()
    {
        if (isDllLoaded)
        {
            try
            {
                TobiiGameIntegrationApi.StopTracking();
                TobiiGameIntegrationApi.Shutdown();
            }
            catch (Exception) { }

            isDllLoaded = false;
            Debug.Log("[Tobii] TGI heruntergefahren.");
        }
    }

    // ========================================================================
    // HILFSMETHODEN
    // ========================================================================

    public bool GetGazeRay(out Ray gazeRay)
    {
        if (HasValidGazeData && Camera.main != null)
        {
            gazeRay = Camera.main.ViewportPointToRay(
                new Vector3(GazePointViewport.x, GazePointViewport.y, 0f));
            return true;
        }
        gazeRay = default;
        return false;
    }

    public bool GazeRaycast(out RaycastHit hit, float maxDistance = 100f)
    {
        Ray ray;
        if (GetGazeRay(out ray))
            return Physics.Raycast(ray, out hit, maxDistance);
        hit = default;
        return false;
    }
}
