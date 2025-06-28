using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // For UI elements
using UnityEngine.SceneManagement; // For scene management
using TMPro; // For TextMeshPro 
using DG.Tweening;

public class ControllerTest : MonoBehaviour
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
    public bool IsHalted() { return isHalted; }
    public bool IsGameOver() { return isGameOver; }
    public bool HasWon() { return hasWon; }

    [Header("UI")]
    public TMP_Text gearText;
    public TMP_Text timerText;
    public TMP_Text distanceText;
    public GameObject gameOverPanel;
    public GameObject winPanel;
    public GoalProgressBar goalProgressBar;  // Assign in inspector
    // Private variables
    private int currentGear = 1; // Start in first gear (0-based index)
    private float gameTimer = 0f;
    private bool isGameOver = false;
    private bool hasWon = false;
    private float minYPosition; // Bottom boundary
    private float maxYPosition; // Top boundary
    
    private float distanceTraveled = 0f; // Virtual distance traveled
    private bool isHalted = false;
    public float haltDuration = 1.0f; // How long the player is halted after hitting an obstacle


    public float worldSpeed; // Current world movement speed



    [Header("Collision Effects")]
    public bool enableSpinAnimation = true;
    public float spinDuration = 1f;
    public float spinRotations = 3f;
    public bool enableBounceEffect = true;
    public bool enableFlashingEffect = true;

    // For repeating backgrounds
    private List<SpriteRenderer> backgrounds = new List<SpriteRenderer>();
    private float backgroundSize;

    [Header("Obstacle Movement")]
    public float[] obstacleSpeedFactors = { 0.5f, 0.7f, 0.8f }; // Percentage of world speed
    public bool randomizeObstacleSpeed = true; // Whether to randomiz
    
    [Header("Obstacle Animation")]
    public float animationFrameRate = 0.5f; // How fast to swap frames (seconds)

    [Header("Player Animation")]
    public float playerAnimationFrameRate = 0.1f; // Faster player animation (less seconds between swaps)
    private ObstacleAnimation playerAnimation;

    [Header("Lane System")]
    public int numberOfLanes = 5;       // Number of distinct lanes on the road
    public float[] lanePositions;       // Will store Y positions of each lane

    [Header("Speed Effects")]
    public ParticleSystem speedLines;               // Assign a particle system in the inspector
    public int minGearForSpeedLines = 2;            // Only show speed lines in gear 3 and above (0-based)
    public Color speedLineColor = Color.white;      // Color of speed lines
    public float maxSpeedLineRate = 100f;           // Maximum emission rate at top speed


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

        if (playerCar == null)
            playerCar = GameObject.FindGameObjectWithTag("Player");

        if (obstacleParent == null)
            obstacleParent = new GameObject("Obstacles").transform;

        // Calculate lane boundaries
        minYPosition = -laneWidth / 2;
        maxYPosition = laneWidth / 2;

        // Setup lane positions
        SetupLanes();

        // Start obstacle spawning
        StartCoroutine(SpawnObstacles());

        // Start obstacle animation
        StartCoroutine(AnimateObstacles());

        //start player animation
        SetupPlayerAnimation();

        //setup speed lines
        SetupSpeedLines();
    }

    void SetupLanes()
    {
        // Initialize lane positions array
        lanePositions = new float[numberOfLanes];

        // Calculate lane spacing based on road width
        float laneSpacing = laneWidth / (numberOfLanes);
        float firstLaneY = minYPosition + (laneSpacing / 2); // Center of first lane

        // Calculate Y position for each lane
        for (int i = 0; i < numberOfLanes; i++)
        {
            lanePositions[i] = firstLaneY + (i * laneSpacing);
            Debug.Log($"Lane {i} position: {lanePositions[i]}");
        }
    }
    void SetupPlayerAnimation()
    {
        if (playerCar != null)
        {
            // Get or add animation component to player
            playerAnimation = playerCar.GetComponent<ObstacleAnimation>();
            if (playerAnimation == null)
            {
                playerAnimation = playerCar.AddComponent<ObstacleAnimation>();
            }

            // Start player animation coroutine
            StartCoroutine(AnimatePlayer());
        }
    }

    IEnumerator AnimatePlayer()
    {
        while (true)
        {
            if (!isGameOver && !hasWon)
            {
                // Animate player regardless of halt state for visual interest
                if (playerAnimation != null)
                {
                    playerAnimation.SwapFrame();
                }
            }

            // Use player-specific animation rate (typically faster than obstacles)
            yield return new WaitForSeconds(playerAnimationFrameRate);
        }
    }

    // Setup repeating background
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

        //if (isGameOver || hasWon) return;

        // Always update UI regardless of halt state
        UpdateDistanceText();
        
        // If halted, don't process any movement or input
        if (isHalted)
        {
            // Even when halted, we should update speed lines (to turn them off)
            UpdateSpeedLines();
            return;
        }
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
        // If player is halted, don't process obstacles
        if (isHalted)
            return;

        foreach (Transform obstacle in obstacleParent)
        {
            if (obstacle == null) continue;

            // Don't move obstacles at all - they stay where they are
            // The background movement creates the illusion of passing obstacles

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
            timerText.text = timerDisplay;
        }
    }




    private void UpdateDistanceText()
    {
        // Calculate distance for both text (if enabled) and progress bar
        float remainingDistance = (endPosition - startPosition) - distanceTraveled;
        float totalDistance = (endPosition - startPosition);
        float distanceProgress = distanceTraveled;

        // Update text if it's still being used
        if (distanceText != null && distanceText.gameObject.activeInHierarchy)
        {
            distanceText.text = "Distance to Goal: " + Mathf.Max(0, remainingDistance).ToString("F1") + "m";
        }

        // Update progress bar
        if (goalProgressBar != null)
        {
            goalProgressBar.UpdateProgress(distanceProgress);
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

            // Select a random lane for this obstacle
            int laneIndex = Random.Range(0, numberOfLanes);
            float obstacleY = lanePositions[laneIndex];

            // Random obstacle selection
            if (obstacles != null && obstacles.Length > 0)
            {
                GameObject obstaclePrefab = obstacles[Random.Range(0, obstacles.Length)];

                // Position obstacles ahead of the player's view in the selected lane
                Vector3 spawnPos = new Vector3(spawnDistance, obstacleY, 0f);

                // Instantiate obstacle - no movement component needed
                GameObject newObstacle = Instantiate(obstaclePrefab, spawnPos, Quaternion.identity, obstacleParent);

                // Remove any ObstacleMovement component if it exists
                ObstacleMovement movement = newObstacle.GetComponent<ObstacleMovement>();
                if (movement != null)
                {
                    Destroy(movement);
                }
            }

            // Calculate spawn interval based on player speed
            float minSpawnInterval = 0.50f; // Fastest spawn rate at max speed
            float maxSpawnInterval = 0.75f; // Slowest spawn rate at min speed

            // Calculate normalized speed (0.0 to 1.0) based on min/max gear speeds
            float maxSpeed = gearSpeeds[gearSpeeds.Length - 1];
            float minSpeed = gearSpeeds[0];
            float normalizedSpeed = Mathf.Clamp01((worldSpeed - minSpeed) / (maxSpeed - minSpeed));

            // Lerp between max and min spawn interval based on normalized speed
            float spawnInterval = Mathf.Lerp(maxSpawnInterval, minSpawnInterval, normalizedSpeed);

            // Wait before next spawn
            yield return new WaitForSeconds(spawnInterval);
        }
    }


    IEnumerator AnimateObstacles()
    {
        while (true)
        {
            if (!isGameOver && !hasWon && !isHalted)
            {
                // Find all obstacles and swap their sprites
                foreach (Transform obstacle in obstacleParent)
                {
                    if (obstacle == null) continue;

                    // Get the sprite renderer
                    SpriteRenderer renderer = obstacle.GetComponent<SpriteRenderer>();
                    if (renderer == null) continue;

                    // Get the obstacle animation component
                    ObstacleAnimation animation = obstacle.GetComponent<ObstacleAnimation>();
                    if (animation == null)
                    {
                        // Add animation component if it doesn't exist
                        animation = obstacle.gameObject.AddComponent<ObstacleAnimation>();
                    }

                    // Swap to the next frame
                    animation.SwapFrame();
                }
            }

            // Wait before next animation frame swap
            yield return new WaitForSeconds(animationFrameRate);
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

        // Get the transform to animate
        Transform playerTransform = playerCar.transform;

        // Mario Kart style spin animation with DOTween
        // Spin the car around the z-axis 3 times (1080 degrees)
        playerTransform.DORotate(new Vector3(0, 0, 1080), haltDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.OutQuad);

        // Optional: Add a little bounce effect
        playerTransform.DOPunchScale(new Vector3(0.2f, 0.2f, 0.2f), haltDuration, 10, 1)
            .SetEase(Ease.OutElastic);

        // Optional: Visual feedback with color flashing
        SpriteRenderer playerSprite = playerCar.GetComponent<SpriteRenderer>();
        if (playerSprite != null)
        {
            // Flash the car sprite with DOTween
            playerSprite.DOColor(new Color(1f, 0.5f, 0.5f), haltDuration * 0.25f)
                .SetLoops(4, LoopType.Yoyo);
        }

        // Wait for the halt duration
        yield return new WaitForSeconds(haltDuration);

        // Reset rotation to normal when done
        playerTransform.DORotate(Vector3.zero, 0.3f);

        // Resume movement
        isHalted = false;
    }

    // UI button functions
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Add this to your Start() method after other initializations
    void SetupSpeedLines()
    {
        // Create speed lines if not assigned
        if (speedLines == null)
        {
            // Your existing setup code...

            var renderer = speedLines.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.3f;
            renderer.lengthScale = 2f;
            renderer.sortingOrder = 10;

            // Create default material for particles
            Material particleMaterial = new Material(Shader.Find("Particles/Standard Unlit"));
            renderer.material = particleMaterial;

            Debug.Log("Speed lines particle system created");
        }

        // Initially disable speed lines but make sure system is enabled
        if (speedLines != null)
        {
            var emission = speedLines.emission;
            emission.rateOverTime = 0;
            speedLines.Play(); // Make sure the system is playing
            Debug.Log($"Speed lines setup completed. Will show at gear {minGearForSpeedLines + 1} and above");
        }
    }


    // Add this to your Update() method after updating world speed
    void UpdateSpeedLines()
    {
        if (speedLines != null)
        {
            var emission = speedLines.emission;

            if (isHalted || isGameOver || hasWon || currentGear < minGearForSpeedLines)
            {
                // Turn off speed lines when halted, game over, or at low gears
                emission.rateOverTime = 0;
            }
            else
            {
                // Calculate speed line intensity based on current gear
                float speedRatio = (float)(currentGear - minGearForSpeedLines) /
                                   (gearSpeeds.Length - 1 - minGearForSpeedLines);
                speedRatio = Mathf.Clamp01(speedRatio);

                // Adjust emission rate based on speed
                emission.rateOverTime = speedRatio * maxSpeedLineRate;

                // Optional: Adjust color intensity with speed
                var main = speedLines.main;
                main.startColor = new Color(
                    speedLineColor.r,
                    speedLineColor.g,
                    speedLineColor.b,
                    speedLineColor.a * speedRatio);
            }
        }
    }


    // Add this public method to get the current gear
    public int GetCurrentGear()
    {
        return currentGear;
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
