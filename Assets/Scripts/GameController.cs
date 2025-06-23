using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // For UI elements
using UnityEngine.SceneManagement; // For scene management
using TMPro; // For TextMeshPro 

public class GameController : MonoBehaviour
{
    [Header("Player Settings")]
    public GameObject playerCar;
    public float verticalMoveSpeed = 5f; // Vertical movement speed
    public float laneWidth = 8f; // Total width of the road
    public float[] gearSpeeds = { 5f, 10f, 15f, 20f, 25f }; // Speed for each gear
    public float startPosition = -10f; // Starting X position (now just for calculations)
    public float endPosition = 1000f; // Gas station X position (now just for calculations)

    [Header("Background")]
    public GameObject roadBackground; // Assign your repeating road background
    public float backgroundScrollSpeed = 0.1f; // Multiplier for background scrolling
    public bool useRepeatingBackground = true; // Set to false if using a tileable renderer

    [Header("Gameplay")]
    public float obstacleSpawnRate = 1.5f;
    public GameObject[] obstacles;
    public Transform obstacleParent;
    public float minObstacleSpacing = 10f;
    public float spawnDistance = 20f; // How far ahead obstacles spawn
    public float despawnDistance = 10f; // How far behind obstacles despawn

    [Header("UI")]
    public TMP_Text gearText;
    public TMP_Text timerText;
    public TMP_Text distanceText;
    public GameObject gameOverPanel;
    public GameObject winPanel;

    // Private variables
    private int currentGear = 1; // Start in first gear (0-based index)
    private float gameTimer = 0f;
    private bool isGameOver = false;
    private bool hasWon = false;
    private float minYPosition; // Bottom boundary
    private float maxYPosition; // Top boundary
    private float worldSpeed; // Current world movement speed
    private float distanceTraveled = 0f; // Virtual distance traveled
    private bool isHalted = false;
    public float haltDuration = 1.0f; // How long the player is halted after hitting an obstacle


    // For repeating backgrounds
    private List<SpriteRenderer> backgrounds = new List<SpriteRenderer>();
    private float backgroundSize;

    // Start is called before the first frame update
    void Start()
    {
        if (playerCar == null)
            playerCar = GameObject.FindGameObjectWithTag("Player");

        if (obstacleParent == null)
            obstacleParent = new GameObject("Obstacles").transform;

        // Calculate lane boundaries
        minYPosition = -laneWidth / 2;
        maxYPosition = laneWidth / 2;

        // Initialize player position (fixed X position)
        playerCar.transform.position = new Vector3(0, 0, 0);

        // Setup background if using repeating sprites
        if (useRepeatingBackground && roadBackground != null)
        {
            SetupRepeatingBackground();
        }

        // Setup UI
        UpdateGearText();

        // Hide end game panels
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);

        // Set initial world speed
        worldSpeed = gearSpeeds[currentGear];

        // Start obstacle spawning
        StartCoroutine(SpawnObstacles());
    }

    // Setup repeating background (if using sprite-based backgrounds)
    void SetupRepeatingBackground()
    {
        SpriteRenderer backgroundSprite = roadBackground.GetComponent<SpriteRenderer>();
        if (backgroundSprite != null)
        {
            // Calculate width of background sprite
            backgroundSize = backgroundSprite.bounds.size.x;
            backgrounds.Add(backgroundSprite);

            // Create additional copies for seamless scrolling
            for (int i = 0; i < 2; i++) // Create 2 extra copies
            {
                GameObject copy = Instantiate(roadBackground,
                    roadBackground.transform.position + new Vector3(backgroundSize * (i + 1), 0, 0),
                    Quaternion.identity);
                copy.transform.parent = roadBackground.transform.parent;
                backgrounds.Add(copy.GetComponent<SpriteRenderer>());
            }
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (isGameOver || hasWon) return;

        // Always update UI regardless of halt state
        UpdateDistanceText();

        // If halted, don't process any movement or input
        if (isHalted) Debug.Log("Player is currently halted");
        if (isHalted)
            return;

        // From this point on, code only runs when NOT halted

        // Handle player movement
        float verticalInput = Input.GetAxis("Vertical"); // Uses Up/Down arrows or W/S keys
        float newYPosition = playerCar.transform.position.y + (verticalInput * verticalMoveSpeed * Time.deltaTime);

        // Clamp position to stay within road boundaries
        newYPosition = Mathf.Clamp(newYPosition, minYPosition, maxYPosition);

        // Apply vertical movement (only Y changes, X stays fixed)
        playerCar.transform.position = new Vector3(
            playerCar.transform.position.x,
            newYPosition,
            playerCar.transform.position.z);

        // Handle gear changes (left/right)
        if (Input.GetKeyDown(KeyCode.RightArrow) && currentGear < gearSpeeds.Length - 1)
        {
            currentGear++;
            UpdateGearText();
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow) && currentGear > 0)
        {
            currentGear--;
            UpdateGearText();
        }

        // Update world speed based on current gear
        worldSpeed = gearSpeeds[currentGear];

        // Track virtual distance traveled
        distanceTraveled += worldSpeed * Time.deltaTime;

        // Update game timer
        gameTimer += Time.deltaTime;
        UpdateTimerText();

        // Process world movement (obstacles and background)
        MoveObstacles();
        ScrollBackground();

        // Check if player has virtually reached the goal (gas station)
        if (distanceTraveled >= (endPosition - startPosition))
        {
            Win();
        }
    }


    // Move all obstacles toward the player
    private void MoveObstacles()
    {
        // If player is halted, don't move obstacles at all
        if (isHalted)
            return;

        foreach (Transform obstacle in obstacleParent)
        {
            // Move obstacle toward player (left)
            obstacle.position += Vector3.left * worldSpeed * Time.deltaTime;

            // Remove obstacles that have gone past the player
            if (obstacle.position.x < -despawnDistance)
            {
                Destroy(obstacle.gameObject);
            }
        }
    }

    // Scroll the background based on worldSpeed
    private void ScrollBackground()
    {
        // If player is halted, don't scroll background at all
        if (isHalted)
            return;

        if (useRepeatingBackground)
        {
            // For sprite-based repeating backgrounds
            if (backgrounds.Count > 0)
            {
                // Move all background pieces
                foreach (SpriteRenderer bg in backgrounds)
                {
                    bg.transform.position += Vector3.left * worldSpeed * Time.deltaTime * backgroundScrollSpeed;

                    // If background piece has moved off-screen to the left, move it to the right
                    if (bg.transform.position.x < -backgroundSize)
                    {
                        bg.transform.position = new Vector3(
                            bg.transform.position.x + backgroundSize * backgrounds.Count,
                            bg.transform.position.y,
                            bg.transform.position.z);
                    }
                }
            }
        }
        else if (roadBackground != null)
        {
            // For shader/material-based scrolling
            Renderer renderer = roadBackground.GetComponent<Renderer>();
            if (renderer != null && renderer.material.mainTexture != null)
            {
                // Scroll the texture
                float offset = Time.time * worldSpeed * backgroundScrollSpeed;
                renderer.material.mainTextureOffset = new Vector2(offset % 1, 0);
            }
        }
    }



  

    private void UpdateGearText()
    {
        if (gearText != null)
            gearText.text = "Gear: " + (currentGear + 1);
    }

    private void UpdateTimerText()
    {
        if (timerText != null)
        {
            string timerDisplay = isHalted ? gameTimer.ToString() : gameTimer.ToString("F2") + "s";
            timerText.text = "Time: " + timerDisplay;
        }
    }




    private void UpdateDistanceText()
    {
        if (distanceText != null)
        {
            float remainingDistance = (endPosition - startPosition) - distanceTraveled;
            distanceText.text = "Distance to Goal: " + Mathf.Max(0, remainingDistance).ToString("F1") + "m";
        }
    }

    IEnumerator SpawnObstacles()
    {
        while (!isGameOver && !hasWon)
        {
            // Skip spawning if player is halted
            if (isHalted)
            {
                // Wait a short time before checking again
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            // Random Y position within road boundaries
            float obstacleY = Random.Range(minYPosition, maxYPosition);

            // Random obstacle selection
            if (obstacles != null && obstacles.Length > 0)
            {
                GameObject obstaclePrefab = obstacles[Random.Range(0, obstacles.Length)];
                Vector3 spawnPos = new Vector3(spawnDistance, obstacleY, 0f);

                // Instantiate obstacle
                Instantiate(obstaclePrefab, spawnPos, Quaternion.identity, obstacleParent);
            }

            // Wait before next spawn - adjust based on current speed
            // Faster car = faster spawn rate to maintain obstacle density
            float adjustedRate = obstacleSpawnRate * (gearSpeeds[1] / worldSpeed);
            yield return new WaitForSeconds(Mathf.Clamp(adjustedRate, 0.2f, obstacleSpawnRate * 2));
        }
    }


    public void GameOver()
    {
        isGameOver = true;
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

    private void Win()
    {
        hasWon = true;
        if (winPanel != null)
        {
            winPanel.SetActive(true);
            // You could display final time on win panel
        }
    }

    // Call this from a collision detection script on the player
    public void OnPlayerHitObstacle(GameObject hitObstacle)
    {
        Debug.Log("Obstacle Hit!");
        // Don't process collisions while already halted
        if (isHalted) return;

        // Destroy the obstacle that was hit
        if (hitObstacle != null)
        {
            Destroy(hitObstacle);
        }

        // Reset gear to 1 (0-based index)
        currentGear = 0;
        UpdateGearText();

        // Start the halt coroutine
        StartCoroutine(HaltPlayer());
    }

    // Add this new coroutine to handle halting the player
    IEnumerator HaltPlayer()
    {
        // Set halt state
        isHalted = true;

        // Optional: Visual feedback that player is halted
        // For example, flash the player sprite or play a sound
        SpriteRenderer playerSprite = playerCar.GetComponent<SpriteRenderer>();
        if (playerSprite != null)
        {
            // Flash the car sprite
            for (int i = 0; i < 3; i++)
            {
                playerSprite.color = new Color(1f, 0.5f, 0.5f); // Reddish tint
                yield return new WaitForSeconds(0.1f);
                playerSprite.color = Color.white;
                yield return new WaitForSeconds(0.1f);
            }
        }

        // Wait for the halt duration
        yield return new WaitForSeconds(haltDuration);

        // Resume movement
        isHalted = false;
    }

    // UI button functions
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
                        Application.Quit();
#endif
    }
}
