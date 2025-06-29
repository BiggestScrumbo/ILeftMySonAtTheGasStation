using UnityEngine;

public class PlayerCollision : MonoBehaviour
{
    private GameController gameController;

    void Start()
    {
        // Find the GameController in the scene
        gameController = FindObjectOfType<GameController>();

        // Log that we found the controller (debugging)
        if (gameController != null)
            Debug.Log("GameController found by PlayerCollision");
        else
            Debug.LogError("GameController not found!");
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // First check if the collision object exists
        if (collision == null || collision.gameObject == null)
            return;

        // Debug log to see what we're colliding with
        Debug.Log("Player collided with: " + collision.gameObject.name + " (Tag: " + collision.gameObject.tag + ")");

        // Check if gameController exists
        if (gameController == null)
        {
            Debug.LogError("GameController reference is null!");
            return;
        }

        // Check for collectible first (cigarette pack)
        if (collision.gameObject.CompareTag("Collectible") ||
            collision.gameObject.tag == "Collectible" ||
            collision.gameObject.name.ToLower().Contains("pewnorts") ||
            collision.gameObject.name.ToLower().Contains("pack") ||
            collision.gameObject.name.ToLower().Contains("boost"))
        {
            Debug.Log("Collectible detected! Calling OnPlayerCollectBoost");
            gameController.OnPlayerCollectBoost(collision.gameObject);
            return; // Exit early to avoid obstacle collision processing
        }

        // Check for obstacles - but only process if not in boost mode
        if (collision.gameObject.CompareTag("Obstacle") ||
            collision.gameObject.tag == "Obstacle" ||
            collision.gameObject.name.ToLower().Contains("obstacle"))
        {
            Debug.Log("Obstacle detected!");

            // If boost is active, the GameController will handle destroying the obstacle
            // without penalty, so we still call the method
            gameController.OnPlayerHitObstacle(collision.gameObject);
        }
    }

    // Add trigger-based collision detection as a backup
    void OnTriggerEnter2D(Collider2D other)
    {
        // First check if the other object exists
        if (other == null || other.gameObject == null)
            return;

        Debug.Log("Player triggered with: " + other.gameObject.name + " (Tag: " + other.gameObject.tag + ")");

        if (gameController == null)
            gameController = FindObjectOfType<GameController>();

        if (gameController == null)
        {
            Debug.LogError("GameController reference is null in trigger!");
            return;
        }

        // Check for collectible first (cigarette pack)
        if (other.CompareTag("Collectible") ||
            other.tag == "Collectible" ||
            other.name.ToLower().Contains("cigarette") ||
            other.name.ToLower().Contains("pack") ||
            other.name.ToLower().Contains("boost"))
        {
            Debug.Log("Collectible trigger detected! Calling OnPlayerCollectBoost");
            gameController.OnPlayerCollectBoost(other.gameObject);
            return; // Exit early to avoid obstacle collision processing
        }

        // Check for obstacles
        if (other.CompareTag("Obstacle") ||
            other.tag == "Obstacle" ||
            other.name.ToLower().Contains("obstacle"))
        {
            Debug.Log("Obstacle trigger detected!");

            // Call the obstacle hit method - GameController will handle boost logic
            gameController.OnPlayerHitObstacle(other.gameObject);
        }
    }
}