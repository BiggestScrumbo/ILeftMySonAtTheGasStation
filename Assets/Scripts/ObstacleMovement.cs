using UnityEngine;

public class ObstacleMovement : MonoBehaviour
{
    [Header("Speed Settings")]
    [Tooltip("Speed factors per gear (0-4). Lower values = obstacle appears to move faster")]
    public float[] gearSpeedFactors = new float[5] { 0.9f, 0.7f, 0.5f, 0.3f, 0.1f };

    [Tooltip("Use a single speed factor instead of gear-based factors")]
    public bool useSingleSpeedFactor = false;

    [Tooltip("Single speed factor if not using gear-based speeds")]
    [Range(0.0f, 1.0f)]
    public float singleSpeedFactor = 0.7f;

    [Header("Runtime Info")]
    [SerializeField] private int currentPlayerGear = 1; // For inspector debugging
    [SerializeField] private float effectiveSpeedFactor = 0.7f; // For inspector debugging

    private GameController gameController;

    private void Start()
    {
        // Find the GameController
        gameController = FindObjectOfType<GameController>();

        // Ensure we have enough speed factors for all gears
        if (gameController != null && gearSpeedFactors.Length < gameController.gearSpeeds.Length)
        {
            // Resize the array if needed
            System.Array.Resize(ref gearSpeedFactors, gameController.gearSpeeds.Length);

            // Fill any new elements with default values
            for (int i = 0; i < gearSpeedFactors.Length; i++)
            {
                if (i >= gearSpeedFactors.Length)
                {
                    gearSpeedFactors[i] = 0.5f;
                }
            }
        }
    }

    private void Update()
    {
        // Update the current gear and effective speed factor
        if (gameController != null)
        {
            currentPlayerGear = gameController.GetCurrentGear();
            effectiveSpeedFactor = GetCurrentSpeedFactor();
        }
    }

    /// <summary>
    /// Gets the current speed factor based on gear
    /// </summary>
    public float GetCurrentSpeedFactor()
    {
        if (useSingleSpeedFactor)
        {
            return singleSpeedFactor;
        }

        // If we have gear-specific factors and a valid game controller
        if (gameController != null && gearSpeedFactors.Length > 0)
        {
            int gear = gameController.GetCurrentGear();

            // Make sure we don't go out of bounds
            if (gear >= 0 && gear < gearSpeedFactors.Length)
            {
                return gearSpeedFactors[gear];
            }
        }

        // Default fallback
        return 0.7f;
    }
}
