using UnityEngine;

public class ObstacleMovement : MonoBehaviour
{
    // Speed factor between 0 and 1.0
    // - 0.0 means obstacle moves at world speed (appears stationary relative to world)
    // - 0.5 means obstacle moves at half world speed (appears to move forward slowly)
    // - 1.0 means obstacle doesn't move at all (appears to move at player speed)
    public float speedFactor = 0.7f;
}
