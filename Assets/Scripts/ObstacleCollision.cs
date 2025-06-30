using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleCollision : MonoBehaviour
{
    [Header("Explosion Animation")]
    public GameObject explosionPrefab; // Assign your explosion prefab with animation
    private bool hasExploded = false;

    [Header("Movement")]
    public float baseSpeed; // Base speed set from 2nd gear
    public bool useCustomSpeed = false; // Set to true if you want to override the default
    public float customMoveSpeed = 10f; // Custom speed value if needed
    private float currentSpeed; // Current speed that updates with player gear

    private GameController gameController;
    private Vector3 moveDirection = Vector3.left; // Moving left (toward player)

    private void Awake()
    {
        Debug.Log($"ObstacleCollision initialized on {gameObject.name}");
        Debug.Log($"ExplosionPrefab assigned: {explosionPrefab != null}");

        // Find the GameController to get the gear speeds
        gameController = FindObjectOfType<GameController>();

        if (gameController != null && !useCustomSpeed)
        {
            // Get the 2nd gear speed (index 1) as the base speed
            if (gameController.gearSpeeds.Length > 1)
            {
                baseSpeed = gameController.gearSpeeds[1]; // Use 2nd gear speed as base
                Debug.Log($"Obstacle using 2nd gear speed as base: {baseSpeed}");
            }
            else
            {
                baseSpeed = 10f; // Fallback speed if gearSpeeds array is too small
                Debug.LogWarning("GearSpeeds array not long enough, using default speed");
            }
        }
        else if (useCustomSpeed)
        {
            baseSpeed = customMoveSpeed;
            Debug.Log($"Obstacle using custom speed: {baseSpeed}");
        }
        else
        {
            baseSpeed = 10f; // Default speed if GameController not found
            Debug.LogWarning("GameController not found, using default speed");
        }

        // Initialize current speed
        currentSpeed = baseSpeed;
    }

    private void Update()
    {
        // Check if game is over or won
        if (gameController != null && (gameController.IsGameOver() || gameController.HasWon()))
            return;

        // Update obstacle speed based on player's current gear/speed
        UpdateObstacleSpeed();

        // Move the obstacle at the adjusted speed
        transform.position += moveDirection * currentSpeed * Time.deltaTime;
    }

    // New method to update obstacle speed based on player's current gear
    private void UpdateObstacleSpeed()
    {
        if (gameController == null || useCustomSpeed)
            return;

        // Get a scaling factor based on player's current gear vs. gear 2
        float baseGearSpeed = gameController.gearSpeeds[1]; // 2nd gear speed (index 1)
        float currentWorldSpeed = gameController.worldSpeed; // Current player speed

        // Calculate scaling factor - how much faster player is going compared to base gear
        float speedRatio = currentWorldSpeed / baseGearSpeed;

        // Scale obstacle speed, but not 1:1 with player (about 70% of player's speed increase)
        // This makes higher gears feel more effective while still maintaining challenge
        currentSpeed = baseSpeed * (.5f + (speedRatio * .5f));

        // Debug
        //Debug.Log($"Player gear speed: {currentWorldSpeed}, Obstacle speed: {currentSpeed}");
    }

    // Rest of your existing code...
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
