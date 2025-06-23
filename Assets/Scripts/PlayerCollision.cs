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

        // Try using string comparison instead of CompareTag if tag isn't found
        if (collision.gameObject.CompareTag("Obstacle") ||
            collision.gameObject.tag == "Obstacle" ||
            collision.gameObject.name.Contains("obstacle") ||
            collision.gameObject.name.Contains("Obstacle"))
        {
            Debug.Log("Obstacle detected! Calling OnPlayerHitObstacle");
            // Pass the obstacle to the GameController
            gameController.OnPlayerHitObstacle(collision.gameObject);
        }
    }

    // Add trigger-based collision detection as a backup
    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Player triggered with: " + other.gameObject.name + " (Tag: " + other.gameObject.tag + ")");

        if (gameController == null)
            gameController = FindObjectOfType<GameController>();

        if (gameController != null &&
           (other.CompareTag("Obstacle") || other.tag == "Obstacle" ||
            other.name.Contains("obstacle") || other.name.Contains("Obstacle")))
        {
            Debug.Log("Obstacle trigger detected! Calling OnPlayerHitObstacle");
            gameController.OnPlayerHitObstacle(other.gameObject);
        }
    }
}
