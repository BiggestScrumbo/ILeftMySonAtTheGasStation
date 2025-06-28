using UnityEngine;
using UnityEngine.UI;

public class GoalProgressBar : MonoBehaviour
{
    public Image carIndicator;      // Car icon that moves along the track
    public Image progressTrack;     // The track/road background (not a fill image)
    public Color startColor = Color.red;
    public Color midColor = Color.yellow;
    public Color endColor = Color.green;
    public bool useColorChange = true;

    private float totalDistance;

    public void Initialize(float totalDistance)
    {
        this.totalDistance = totalDistance;
        UpdateProgress(0);
    }

    public void UpdateProgress(float distanceTraveled)
    {
        // Calculate progress ratio (0 to 1)
        float progress = Mathf.Clamp01(distanceTraveled / totalDistance);

        // Update car indicator position
        if (carIndicator != null)
        {
            RectTransform rt = carIndicator.rectTransform;
            RectTransform parentRt = transform as RectTransform; // Use the parent container

            if (parentRt != null)
            {
                float width = parentRt.rect.width - rt.rect.width;
                Vector2 position = rt.anchoredPosition;
                position.x = -width / 2 + (width * progress);
                rt.anchoredPosition = position;

                // If color change is enabled, update the car color based on progress
                if (useColorChange)
                {
                    if (progress < 0.5f)
                    {
                        // Lerp from start to mid color
                        carIndicator.color = Color.Lerp(startColor, midColor, progress * 2f);
                    }
                    else
                    {
                        // Lerp from mid to end color
                        carIndicator.color = Color.Lerp(midColor, endColor, (progress - 0.5f) * 2f);
                    }
                }
            }
        }
    }
}
