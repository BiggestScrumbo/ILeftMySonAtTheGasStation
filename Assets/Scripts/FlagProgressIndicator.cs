using UnityEngine;
using UnityEngine.UI;

public class FlagProgressIndicator : MonoBehaviour
{
    [Header("Progress Track")]
    public Image trackBackground;       // The defined area/track background image
    public Image flagIcon;             // The flag icon that slides across

    [Header("Animation Settings")]
    public bool smoothMovement = true;  // Enable smooth movement animation
    public float smoothSpeed = 5f;      // Speed of smooth movement

    [Header("Visual Effects")]
    public bool enableColorChange = true;
    public Color startColor = Color.red;
    public Color midColor = Color.yellow;
    public Color endColor = Color.green;

    [Header("Flag Animation")]
    public bool enableFlagWave = true;  // Enable flag waving animation
    public float waveSpeed = 2f;        // Speed of the wave animation
    public float waveAmplitude = 5f;    // How much the flag waves (in degrees)

    private float totalDistance;
    private float currentProgress = 0f;
    private float targetProgress = 0f;
    private Vector2 startPosition;
    private Vector2 endPosition;
    private float initialRotation;

    void Start()
    {
        if (flagIcon != null)
        {
            initialRotation = flagIcon.transform.rotation.eulerAngles.z;
        }

        CalculatePositions();
    }

    void Update()
    {
        if (smoothMovement && flagIcon != null)
        {
            // Smoothly move toward target progress
            currentProgress = Mathf.MoveTowards(currentProgress, targetProgress, smoothSpeed * Time.deltaTime);
            UpdateFlagPosition(currentProgress);
        }

        // Apply flag waving animation
        if (enableFlagWave && flagIcon != null)
        {
            float wave = Mathf.Sin(Time.time * waveSpeed) * waveAmplitude;
            flagIcon.transform.rotation = Quaternion.Euler(0, 0, initialRotation + wave);
        }
    }

    public void Initialize(float totalDistance)
    {
        this.totalDistance = totalDistance;
        currentProgress = 0f;
        targetProgress = 0f;
        CalculatePositions();
        UpdateProgress(0);
    }

    public void UpdateProgress(float distanceTraveled)
    {
        // Calculate progress ratio (0 to 1)
        float progress = Mathf.Clamp01(distanceTraveled / totalDistance);

        if (smoothMovement)
        {
            targetProgress = progress;
        }
        else
        {
            currentProgress = progress;
            UpdateFlagPosition(progress);
        }
    }

    private void CalculatePositions()
    {
        if (flagIcon == null || trackBackground == null) return;

        RectTransform flagRt = flagIcon.rectTransform;
        RectTransform trackRt = trackBackground.rectTransform;

        // Calculate the available width for movement (track width minus flag width)
        float availableWidth = trackRt.rect.width - flagRt.rect.width;

        // Start position (left side of track)
        startPosition = new Vector2(-availableWidth / 2, flagRt.anchoredPosition.y);

        // End position (right side of track)
        endPosition = new Vector2(availableWidth / 2, flagRt.anchoredPosition.y);

        // Set initial position
        flagRt.anchoredPosition = startPosition;
    }

    private void UpdateFlagPosition(float progress)
    {
        if (flagIcon == null) return;

        RectTransform flagRt = flagIcon.rectTransform;

        // Interpolate between start and end positions
        Vector2 newPosition = Vector2.Lerp(startPosition, endPosition, progress);
        flagRt.anchoredPosition = newPosition;

        // Update flag color based on progress if enabled
        if (enableColorChange)
        {
            Color newColor;
            if (progress < 0.5f)
            {
                // Lerp from start to mid color
                newColor = Color.Lerp(startColor, midColor, progress * 2f);
            }
            else
            {
                // Lerp from mid to end color
                newColor = Color.Lerp(midColor, endColor, (progress - 0.5f) * 2f);
            }

            flagIcon.color = newColor;
        }
    }

    // Method to set flag position directly (useful for testing)
    public void SetProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);
        currentProgress = progress;
        targetProgress = progress;
        UpdateFlagPosition(progress);
    }

    // Method to get current progress
    public float GetProgress()
    {
        return currentProgress;
    }

    void OnValidate()
    {
        // Recalculate positions when values change in the inspector
        if (Application.isPlaying)
        {
            CalculatePositions();
        }
    }
}