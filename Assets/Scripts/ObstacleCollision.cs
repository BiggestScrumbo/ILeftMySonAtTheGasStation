using UnityEngine;

public class ObstacleCollision : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("Obstacle collided with: " + collision.gameObject.name);

        if (collision.gameObject.CompareTag("Player"))
        {
            GameController gameController = FindObjectOfType<GameController>();
            if (gameController != null)
            {
                gameController.OnPlayerHitObstacle(gameObject);
            }
        }
    }

    // Add trigger detection as well
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Obstacle triggered with: " + other.gameObject.name);

        if (other.CompareTag("Player"))
        {
            GameController gameController = FindObjectOfType<GameController>();
            if (gameController != null)
            {
                gameController.OnPlayerHitObstacle(gameObject);
            }
        }
    }
}
