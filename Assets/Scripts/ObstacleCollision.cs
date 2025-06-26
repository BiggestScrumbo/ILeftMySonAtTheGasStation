using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleCollision : MonoBehaviour
{
    [Header("Explosion Animation")]
    public GameObject explosionPrefab; // Assign your explosion prefab with animation
    private bool hasExploded = false;

    [Header("Movement")]
    public float moveSpeed; // This will be set automatically based on 2nd gear
    public bool useCustomSpeed = false; // Set to true if you want to override the default speed
    public float customMoveSpeed = 10f; // Custom speed value if needed

    private GameController gameController;
    private Vector3 moveDirection = Vector3.left; // Moving left (toward player)

    private void Awake()
    {
        Debug.Log($"ObstacleCollision initialized on {gameObject.name}");
        Debug.Log($"ExplosionPrefab assigned: {explosionPrefab != null}");

        // Find the GameController to get the 2nd gear speed
        gameController = FindObjectOfType<GameController>();

        if (gameController != null && !useCustomSpeed)
        {
            // Get the 2nd gear speed (index 1 in the gearSpeeds array)
            if (gameController.gearSpeeds.Length > 1)
            {
                moveSpeed = gameController.gearSpeeds[1]; // Use 2nd gear speed
                Debug.Log($"Obstacle using 2nd gear speed: {moveSpeed}");
            }
            else
            {
                moveSpeed = 10f; // Fallback speed if gearSpeeds array is too small
                Debug.LogWarning("GearSpeeds array not long enough, using default speed");
            }
        }
        else if (useCustomSpeed)
        {
            moveSpeed = customMoveSpeed;
            Debug.Log($"Obstacle using custom speed: {moveSpeed}");
        }
        else
        {
            moveSpeed = 10f; // Default speed if GameController not found
            Debug.LogWarning("GameController not found, using default speed");
        }
    }

    private void Start()
    {
        // Uncomment this line to test explosion at startup
        // TestExplosion();
    }

    private void Update()
    {
        // Move the obstacle forward at constant speed regardless of game state
        // (except when the game is over or won, which will be handled by GameController)
        if (gameController != null && (gameController.IsGameOver() || gameController.HasWon()))
            return;

        transform.position += moveDirection * moveSpeed * Time.deltaTime;
    }

    // Test function to verify explosion works
    private void TestExplosion()
    {
        Debug.Log("Testing explosion animation");
        PlayExplosionAnimation(transform.position);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"Obstacle {gameObject.name} collided with: {collision.gameObject.name}, tag: {collision.gameObject.tag}");

        if (collision.gameObject.CompareTag("Player") && !hasExploded)
        {
            Debug.Log("Player collision detected - attempting to play explosion");
            hasExploded = true;
            PlayExplosionAnimation(collision.GetContact(0).point);

            if (gameController != null)
            {
                gameController.OnPlayerHitObstacle(gameObject);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"Obstacle {gameObject.name} triggered with: {other.gameObject.name}, tag: {other.gameObject.tag}");

        if (other.CompareTag("Player") && !hasExploded)
        {
            Debug.Log("Player trigger detected - attempting to play explosion");
            hasExploded = true;
            PlayExplosionAnimation(transform.position);

            if (gameController != null)
            {
                gameController.OnPlayerHitObstacle(gameObject);
            }
        }
    }

    private void PlayExplosionAnimation(Vector2 position)
    {
        Debug.Log("PlayExplosionAnimation called at: " + position);

        if (explosionPrefab == null)
        {
            Debug.LogError("Explosion prefab is not assigned in the inspector!");
            return;
        }

        // Create explosion at the collision point
        GameObject explosion = Instantiate(explosionPrefab, position, Quaternion.identity);
        Debug.Log("Explosion instantiated: " + (explosion != null));

        // Make explosion render on top of everything
        SpriteRenderer explosionRenderer = explosion.GetComponent<SpriteRenderer>();
        if (explosionRenderer != null)
        {
            // Option 1: Set sorting layer to a high-priority layer (preferred method)
            explosionRenderer.sortingLayerName = "UI"; // Or create a "FX" or "Foreground" layer in Unity

            // Option 2: Use a high sorting order value
            explosionRenderer.sortingOrder = 100; // This should be higher than your background
        }

        // Rest of your animation code remains the same...
        Animator animator = explosion.GetComponent<Animator>();
        if (animator != null)
        {
            Debug.Log("Animator found on explosion");
            float animationLength = GetAnimationLength(animator);
            Destroy(explosion, animationLength);
        }
        else
        {
            SpriteRenderer spriteRenderer = explosion.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                Debug.Log("SpriteRenderer found, will use manual sprite animation");
                StartCoroutine(AnimateExplosionSprites(explosion, spriteRenderer));
            }
            else
            {
                Debug.LogWarning("No animation components found on explosion prefab");
                Destroy(explosion, 2f);
            }
        }
    }

    private IEnumerator AnimateExplosionSprites(GameObject explosion, SpriteRenderer renderer)
    {
        // Check if we have sprites assigned in the explosion object
        ExplosionAnimation explosionAnim = explosion.GetComponent<ExplosionAnimation>();

        if (explosionAnim == null)
        {
            Debug.LogError("ExplosionAnimation component missing from prefab!");
            Destroy(explosion, 1f);
            yield break;
        }

        if (explosionAnim.explosionSprites == null || explosionAnim.explosionSprites.Length == 0)
        {
            Debug.LogError("No explosion sprites assigned in ExplosionAnimation component!");
            Destroy(explosion, 1f);
            yield break;
        }

        Debug.Log("Starting manual sprite animation with " + explosionAnim.explosionSprites.Length + " frames");

        // Play through each sprite in sequence
        for (int i = 0; i < explosionAnim.explosionSprites.Length; i++)
        {
            if (renderer == null || explosion == null)
            {
                Debug.LogWarning("Animation object was destroyed mid-animation");
                yield break;
            }

            renderer.sprite = explosionAnim.explosionSprites[i];
            yield return new WaitForSeconds(explosionAnim.frameDuration);
        }

        // Animation complete, destroy the object
        if (explosion != null)
        {
            Destroy(explosion);
        }
    }

    private float GetAnimationLength(Animator animator)
    {
        // Get length of current animation
        AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
        if (clipInfo.Length > 0)
        {
            return clipInfo[0].clip.length;
        }
        return 1f; // Default to 1 second if no animation found
    }
}
