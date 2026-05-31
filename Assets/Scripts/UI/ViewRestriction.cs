using UnityEngine;

public class ViewRestriction : MonoBehaviour
{
    [Header("UI Reference")]
    public RectTransform targetImage;

    [Header("Settings")]
    public float smoothingSpeed = 15f;
    
    [Tooltip("If the hole isn't perfectly centered, use X and Y to manually nudge it into place.")]
    public Vector2 manualOffset = Vector2.zero;

    private Vector2 currentPos;

    void Start()
    {
        currentPos = new Vector2(Screen.width / 2f, Screen.height / 2f);

        if (targetImage != null)
        {
            targetImage.anchorMin = Vector2.zero; 
            targetImage.anchorMax = Vector2.zero;
            targetImage.pivot = new Vector2(0.5f, 0.5f);
        }
    }

    void Update()
    {
        if (targetImage == null || TobiiManager.Instance == null) return; 

        Vector2 targetPos = currentPos;
        
        if (TobiiManager.Instance.HasValidGazeData)
        {
            Vector2 viewport = TobiiManager.Instance.GazePointViewport;
            targetPos = new Vector2(viewport.x * Screen.width, viewport.y * Screen.height);
        }

        currentPos = Vector2.Lerp(currentPos, targetPos, Time.deltaTime * smoothingSpeed);

        // Apply the position PLUS your manual offset nudge
        targetImage.anchoredPosition = currentPos + manualOffset;
    }
}