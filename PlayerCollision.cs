using UnityEngine;

public class PlayerCollision : MonoBehaviour
{
    private GameController gameController;

    void Start()
    {
        gameController = FindObjectOfType<GameController>();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            gameController.OnPlayerHitObstacle();
        }
    }
}
