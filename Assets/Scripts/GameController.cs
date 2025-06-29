using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // For UI elements
using UnityEngine.SceneManagement; // For scene management
using TMPro; // For TextMeshPro 
using DG.Tweening;

public class GameController : MonoBehaviour
{
    [Header("Player Settings")]
    public GameObject playerCar;
    public float verticalMoveSpeed = 5f; // Base vertical movement speed
    public float verticalSpeedMultiplier = 0.3f; // How much faster each gear makes vertical movement
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

    [Header("Boost Mechanic")]
    public GameObject cigarettePackPrefab; // Assign your cigarette pack prefab in inspector
    public Transform collectibleParent; // Parent for organizing collectibles in hierarchy
    public float collectibleSpawnChance = 0.15f; // 15% chance to spawn with each obstacle wave
    public float collectibleSpeed = 0.3f; // Speed factor relative to world speed (slower than obstacles)
    public float boostSpeed = 35f; // Super speed when boost is active (faster than 5th gear)
    public float boostDuration = 3f; // How long the boost lasts in seconds
    public int maxBoostCharges = 3; // Maximum boost charges player can hold
    private int currentBoostCharges = 0; // Current number of boost charges
    private bool isBoostActive = false; // Whether boost is currently active
    private float boostTimer = 0f; // Timer for boost duration

    [Header("UI")]
    public TMP_Text gearText;
    public TMP_Text timerText;
    public TMP_Text distanceText;
    public TMP_Text boostChargesText; // Text to display boost charges
    public GameObject gameOverPanel;
    public GameObject winPanel;
    public GearSpriteDisplay gearSpriteDisplay;
    public GoalProgressBar goalProgressBar;
    public FlagProgressIndicator flagProgressIndicator; // Add this line

    [Header("Leaderboard Management")]
    public LeaderboardCreatorDemo.LeaderboardManager leaderboardManager; // Reference to leaderboard manager

    // Private variables
    private int currentGear = 1; // Start in first gear (0-based index)
    public float gameTimer = 0f;
    private bool isGameOver = false;
    private bool hasWon = false;
    private bool timerStopped = false; // New flag to permanently stop the timer
    private float minYPosition; // Bottom boundary
    private float maxYPosition; // Top boundary
    private float distanceTraveled = 0f; // Virtual distance traveled
    private bool isHalted = false;
    public float haltDuration = 1.0f; // How long the player is halted after hitting an obstacle
    public float worldSpeed; // Current world movement speed
    private float currentVerticalMoveSpeed; // Current calculated vertical movement speed

    [Header("Gas Station UI")]
    public TMP_Text currentTimeText; // Text element for displaying current run time
    public TMP_Text bestTimeText;    // Text element for displaying best time
    private float bestTime = float.MaxValue; // Store the best time across all runs

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

    [Header("Win Scene")]
    public GameObject gasStationPrefab; // Assign your gas station sprite in inspector
    public GameObject gasStationBackgroundPrefab; // Gas station background prefab
    public float forwardDriveSpeed = 10f; // How fast the car drives forward
    public float forwardDriveDistance = 5f; // How far the car goes forward
    public float obstacleTimeout = 5f; // Maximum time to wait for obstacles to clear
    public float gasStationTransitionTime = 1.5f; // How long it takes to fade between backgrounds
    public float parkingDuration = 2.0f; // How long it takes to park
    public float finalParkPosition = -3.0f; // X position where car parks (negative = left of center)
                                            // New parameters for zoom-off sequence
    private Vector3 gasStationCameraPosition; // Stores fixed position for gas station view
    private bool isCameraFixedAtGasStation = false;
    public float zoomOffSpeed = 30f; // How fast the car zooms off-screen
    public float zoomOffDistance = 20f; // How far to the right the car goes when zooming off
    public float cameraFollowThreshold = 5f; // Distance threshold after which camera stops following
    public bool useCameraFixedPosition = true; // Whether to keep camera fixed during parking
    private bool isWinAnimationPlaying = false;
    private float originalBackgroundScrollSpeed;
    private Vector3 playerStartPosition; // To store player position for parking animation
    private Vector3 originalCameraPosition; // To store original camera position

    // Helper method to calculate current vertical movement speed based on gear
    private void UpdateVerticalMoveSpeed()
    {
        // Calculate vertical movement speed: base speed + (gear level * multiplier)
        currentVerticalMoveSpeed = verticalMoveSpeed + (currentGear * verticalSpeedMultiplier);

        Debug.Log($"Gear {currentGear + 1}: Vertical speed = {currentVerticalMoveSpeed:F1}");
    }

    // Helper method to hide gameplay UI elements during win animation
    private void HideGameplayUI()
    {
        // Hide timer text during win animation
        if (timerText != null)
            timerText.gameObject.SetActive(false);

        // Hide progress bar during win animation
        if (goalProgressBar != null)
            goalProgressBar.gameObject.SetActive(false);

        // Hide flag progress indicator during win animation
        if (flagProgressIndicator != null)
            flagProgressIndicator.gameObject.SetActive(false);
    }

    // Helper method to show gameplay UI elements (for restart)
    private void ShowGameplayUI()
    {
        // Show timer text during normal gameplay
        if (timerText != null)
            timerText.gameObject.SetActive(true);

        // Show progress bar during normal gameplay
        if (goalProgressBar != null)
            goalProgressBar.gameObject.SetActive(true);

        // Show flag progress indicator during normal gameplay
        if (flagProgressIndicator != null)
            flagProgressIndicator.gameObject.SetActive(true);
    }

    private void UpdateGearText()
    {
        if (gearText != null)
            gearText.gameObject.SetActive(false); // Hide the text display

        // Update the gear sprite if available
        if (gearSpriteDisplay != null)
        {
            gearSpriteDisplay.UpdateGearSprite(currentGear);
        }

        // Update vertical movement speed when gear changes
        UpdateVerticalMoveSpeed();
    }

    // Update boost charges UI
    private void UpdateBoostUI()
    {
        if (boostChargesText != null)
        {
            boostChargesText.text = $"Boost: {currentBoostCharges}/{maxBoostCharges}";

            // Change color based on boost status
            if (isBoostActive)
            {
                boostChargesText.color = Color.yellow; // Yellow during boost
            }
            else if (currentBoostCharges > 0)
            {
                boostChargesText.color = Color.green; // Green when charges available
            }
            else
            {
                boostChargesText.color = Color.white; // White when no charges
            }
        }
    }

    // Activate boost mode
    private void ActivateBoost()
    {
        if (currentBoostCharges <= 0 || isBoostActive) return;

        currentBoostCharges--;
        isBoostActive = true;
        boostTimer = boostDuration;

        Debug.Log($"Boost activated! Charges remaining: {currentBoostCharges}");

        // Visual effect for player car during boost
        SpriteRenderer playerSprite = playerCar.GetComponent<SpriteRenderer>();
        if (playerSprite != null)
        {
            // Flash effect during boost
            playerSprite.DOColor(Color.cyan, 0.2f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetId("BoostFlash");
        }

        UpdateBoostUI();
    }

    // Deactivate boost mode
    private void DeactivateBoost()
    {
        isBoostActive = false;
        boostTimer = 0f;

        Debug.Log("Boost deactivated");

        // Stop visual effects
        SpriteRenderer playerSprite = playerCar.GetComponent<SpriteRenderer>();
        if (playerSprite != null)
        {
            DOTween.Kill("BoostFlash");
            playerSprite.color = Color.white; // Reset to normal color
        }

        UpdateBoostUI();
    }

    // Add boost charge (called when collecting cigarette pack)
    public void AddBoostCharge()
    {
        if (currentBoostCharges < maxBoostCharges)
        {
            currentBoostCharges++;
            Debug.Log($"Boost charge collected! Total charges: {currentBoostCharges}");
            UpdateBoostUI();

            // Visual feedback for collecting boost
            if (playerCar != null)
            {
                playerCar.transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0), 0.3f, 10, 0.5f);
            }
        }
    }

    // Check if player is in boost mode (used by collision detection)
    public bool IsBoostActive()
    {
        return isBoostActive;
    }

    private void MoveObstacles()
    {
        // If player is halted, don't process obstacles
        if (isHalted)
            return;

        // Get a reference speed (2nd gear speed) to compare against
        float gearTwoSpeed = gearSpeeds.Length > 1 ? gearSpeeds[1] : 10f;

        // Special minimum speed for 1st gear to ensure forward movement
        float minimumRelativeSpeed = 3f;

        foreach (Transform obstacle in obstacleParent)
        {
            if (obstacle == null) continue;

            // Get obstacle's speed factor if it has a movement component
            ObstacleMovement obstacleMovement = obstacle.GetComponent<ObstacleMovement>();
            float speedFactor = 0.7f; // Default speed factor

            if (obstacleMovement != null)
            {
                speedFactor = obstacleMovement.GetCurrentSpeedFactor();
            }

            // Calculate base movement speed
            float baseSpeed = gearTwoSpeed * speedFactor;

            // Use boost speed if active, otherwise use current world speed
            float currentPlayerSpeed = isBoostActive ? boostSpeed : worldSpeed;

            // Calculate relative speed between player and obstacle
            float relativeSpeed = currentPlayerSpeed - baseSpeed;

            // Special handling for 1st gear - ensure obstacles always move forward
            if (currentGear == 0 && relativeSpeed <= 0 && !isBoostActive)
            {
                // Force a minimum positive relative speed in 1st gear
                relativeSpeed = minimumRelativeSpeed;
            }

            // Move obstacle based on relative speed
            obstacle.position += Vector3.left * relativeSpeed * Time.deltaTime;

            // Remove obstacles that have gone past the player
            if (obstacle.position.x < -despawnDistance)
            {
                Destroy(obstacle.gameObject);
            }
        }
    }

    // Move collectibles (cigarette packs)
    private void MoveCollectibles()
    {
        if (isHalted || collectibleParent == null)
            return;

        foreach (Transform collectible in collectibleParent)
        {
            if (collectible == null) continue;

            // Use boost speed if active, otherwise use current world speed
            float currentPlayerSpeed = isBoostActive ? boostSpeed : worldSpeed;

            // Collectibles move slower than obstacles
            float collectibleMovementSpeed = currentPlayerSpeed * collectibleSpeed;

            // Move collectible based on its speed
            collectible.position += Vector3.left * collectibleMovementSpeed * Time.deltaTime;

            // Remove collectibles that have gone past the player
            if (collectible.position.x < -despawnDistance)
            {
                Destroy(collectible.gameObject);
            }
        }
    }

    private void ScrollBackground()
    {
        // If player is halted or if we're in win animation, don't automatically scroll
        if (isHalted)
            return;

        // When win animation is playing, the coroutine controls the scroll speed

        if (useRepeatingBackground)
        {
            // For sprite-based repeating backgrounds
            if (backgrounds.Count > 0)
            {
                // Use boost speed if active for background scrolling
                float scrollSpeed = isBoostActive ? boostSpeed : worldSpeed;

                // Move all background pieces
                foreach (SpriteRenderer bg in backgrounds)
                {
                    bg.transform.position += Vector3.left * scrollSpeed * Time.deltaTime * backgroundScrollSpeed;

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
                // Use boost speed if active for texture scrolling
                float scrollSpeed = isBoostActive ? boostSpeed : worldSpeed;

                // Scroll the texture
                float offset = Time.time * scrollSpeed * backgroundScrollSpeed;
                renderer.material.mainTextureOffset = new Vector2(offset % 1, 0);
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        if (playerCar == null)
            playerCar = GameObject.FindGameObjectWithTag("Player");

        if (obstacleParent == null)
            obstacleParent = new GameObject("Obstacles").transform;

        // Setup collectible parent if not assigned
        if (collectibleParent == null)
            collectibleParent = new GameObject("Collectibles").transform;

        if (goalProgressBar != null)
        {
            float totalRaceDistance = endPosition - startPosition;
            goalProgressBar.Initialize(totalRaceDistance);
        }

        // Initialize flag progress indicator
        if (flagProgressIndicator != null)
        {
            float totalRaceDistance = endPosition - startPosition;
            flagProgressIndicator.Initialize(totalRaceDistance);
        }

        if (goalProgressBar != null)
        {
            float totalRaceDistance = endPosition - startPosition;
            goalProgressBar.Initialize(totalRaceDistance);
        }
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
        UpdateBoostUI(); // Initialize boost UI
        if (bestTimeText != null) bestTimeText.text = ""; // Initialize best time text
        if (currentTimeText != null) currentTimeText.text = ""; // Initialize current time text

        // Show gameplay UI elements at start
        ShowGameplayUI();

        // Ensure leaderboard is hidden during gameplay
        if (leaderboardManager != null)
        {
            leaderboardManager.HideGasStationLeaderboard();
        }

        // Hide end game panels
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);

        // Set initial world speed
        worldSpeed = gearSpeeds[currentGear];

        // Setup lane positions
        SetupLanes();

        // Initialize vertical movement speed
        UpdateVerticalMoveSpeed();

        // Start obstacle spawning
        StartCoroutine(SpawnObstacles());

        // Start collectible spawning
        StartCoroutine(SpawnCollectibles());

        // Start obstacle animation
        StartCoroutine(AnimateObstacles());

        //start player animation
        SetupPlayerAnimation();
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
            // Use our new PlayerCarAnimation component instead of ObstacleAnimation
            PlayerCarAnimation playerCarAnimation = playerCar.GetComponent<PlayerCarAnimation>();
            if (playerCarAnimation == null)
            {
                // Remove any existing ObstacleAnimation component if found
                ObstacleAnimation existingAnimation = playerCar.GetComponent<ObstacleAnimation>();
                if (existingAnimation != null)
                    Destroy(existingAnimation);

                // Add the gear-aware animation component
                playerCarAnimation = playerCar.AddComponent<PlayerCarAnimation>();
            }

            // Start player animation coroutine
            StartCoroutine(AnimatePlayer());
        }
    }

    IEnumerator AnimatePlayer()
    {
        while (true)
        {
            // MODIFIED: Continue player animation during win animation, only stop when game is over
            if (!isGameOver) // Removed the hasWon condition
            {
                // Animate player regardless of halt state or win state for visual interest
                PlayerCarAnimation playerCarAnimation = playerCar.GetComponent<PlayerCarAnimation>();
                if (playerCarAnimation != null)
                {
                    playerCarAnimation.SwapFrame();
                }
            }

            // Use player-specific animation rate
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
        // Only increment timer if the player hasn't won and timer hasn't been stopped
        if (!timerStopped)
            gameTimer += Time.deltaTime;

        if (isGameOver || hasWon) return;

        // Always update UI regardless of halt state
        UpdateDistanceText();
        UpdateTimerText();

        // If win animation is playing, let the coroutine handle movement
        if (isWinAnimationPlaying || hasWon)
        {
            return;
        }

        // If halted, don't process any movement or input
        if (isHalted)
        {
            return;
        }
        // From this point on, code only runs when NOT halted

        // Handle boost activation with spacebar
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ActivateBoost();
        }

        // Update boost timer
        if (isBoostActive)
        {
            boostTimer -= Time.deltaTime;
            if (boostTimer <= 0f)
            {
                DeactivateBoost();
            }
        }

        // Handle player movement with gear-based vertical speed
        float verticalInput = Input.GetAxis("Vertical"); // Uses Up/Down arrows or W/S keys
        float newYPosition = playerCar.transform.position.y + (verticalInput * currentVerticalMoveSpeed * Time.deltaTime);

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

        // Update world speed based on current gear (but boost overrides this for movement)
        worldSpeed = gearSpeeds[currentGear];

        // Track virtual distance traveled (use boost speed if active)
        float currentSpeed = isBoostActive ? boostSpeed : worldSpeed;
        distanceTraveled += currentSpeed * Time.deltaTime;

        // Update progress bar
        if (goalProgressBar != null)
        {
            goalProgressBar.UpdateProgress(distanceTraveled);
        }

        // Update flag progress indicator
        if (flagProgressIndicator != null)
        {
            flagProgressIndicator.UpdateProgress(distanceTraveled);
        }

        // Update game timer
        gameTimer += Time.deltaTime;
        UpdateTimerText();

        // Process world movement (obstacles, collectibles, and background)
        MoveObstacles();
        MoveCollectibles();
        ScrollBackground();

        // Check if player has virtually reached the goal (gas station)
        if (distanceTraveled >= (endPosition - startPosition))
        {
            Win();
        }
    }
    private void UpdateTimerText()
    {
        if (timerText != null)
        {
            // Always show timer with 2 decimal places
            timerText.text = gameTimer.ToString("F2");
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
            // Skip spawning if player is halted or won
            if (isHalted || hasWon || isWinAnimationPlaying)
            {
                // Wait a short time before checking again
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            // Try to find a valid spawn position
            Vector3 spawnPos = Vector3.zero;
            bool validPositionFound = false;
            int maxAttempts = 10; // Prevent infinite loops
            int attempts = 0;

            while (!validPositionFound && attempts < maxAttempts)
            {
                // Select a random lane for this obstacle
                int laneIndex = Random.Range(0, numberOfLanes);
                float obstacleY = lanePositions[laneIndex];

                // Position obstacles ahead of the player's view in the selected lane
                spawnPos = new Vector3(spawnDistance, obstacleY, 0f);

                // Check if this position is clear of other obstacles
                validPositionFound = IsSpawnPositionClear(spawnPos);
                attempts++;
            }

            // Only spawn if we found a valid position
            if (validPositionFound && obstacles != null && obstacles.Length > 0)
            {
                GameObject obstaclePrefab = obstacles[Random.Range(0, obstacles.Length)];

                // Instantiate obstacle - no movement component needed
                GameObject newObstacle = Instantiate(obstaclePrefab, spawnPos, Quaternion.identity, obstacleParent);

                // Remove any ObstacleMovement component if it exists
                ObstacleMovement movement = newObstacle.GetComponent<ObstacleMovement>();
                if (movement != null)
                {
                    Destroy(movement);
                }
            }

            // ENHANCED: More aggressive spawn rate scaling based on gear
            float minSpawnInterval = 0.35f; // Faster spawn rate at max speed (reduced from 0.50f)
            float maxSpawnInterval = 0.85f; // Slower spawn rate at min speed (increased from 0.75f)

            // Get current effective speed (including boost)
            float currentSpeed = isBoostActive ? boostSpeed : worldSpeed;

            // Create a more aggressive scaling curve using gear index directly
            float gearRatio = (float)currentGear / (gearSpeeds.Length - 1); // 0.0 to 1.0 based on gear

            // Apply exponential scaling to make higher gears much more intense
            float exponentialGearRatio = Mathf.Pow(gearRatio, 0.6f); // Makes higher gears more pronounced

            // Also factor in actual speed for boost mode
            float maxSpeed = gearSpeeds[gearSpeeds.Length - 1];
            float minSpeed = gearSpeeds[0];
            float speedRatio = Mathf.Clamp01((currentSpeed - minSpeed) / (maxSpeed - minSpeed));

            // Combine gear ratio and speed ratio, giving more weight to gear
            float combinedRatio = (exponentialGearRatio * 0.7f) + (speedRatio * 0.3f);

            // Special boost mode - even faster spawning
            if (isBoostActive)
            {
                minSpawnInterval = 0.25f; // Very fast during boost
                combinedRatio = 1.0f; // Maximum spawn rate
            }

            // Calculate final spawn interval
            float spawnInterval = Mathf.Lerp(maxSpawnInterval, minSpawnInterval, combinedRatio);

            // Add slight randomization to make it feel more organic
            spawnInterval += Random.Range(-0.05f, 0.05f);
            spawnInterval = Mathf.Max(spawnInterval, 0.2f); // Ensure minimum interval

            // Debug info to see the scaling in action
            Debug.Log($"Gear {currentGear + 1}: Spawn interval = {spawnInterval:F2}s (Ratio: {combinedRatio:F2})");

            // Wait before next spawn
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    // New coroutine for spawning collectibles (cigarette packs)
    IEnumerator SpawnCollectibles()
    {
        while (!isGameOver && !hasWon)
        {
            // Skip spawning if player is halted or won
            if (isHalted || hasWon || isWinAnimationPlaying)
            {
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            // Check if we should spawn a collectible this cycle
            if (Random.Range(0f, 1f) < collectibleSpawnChance && cigarettePackPrefab != null)
            {
                // Select a random lane for the collectible
                int laneIndex = Random.Range(0, numberOfLanes);
                float collectibleY = lanePositions[laneIndex];

                // Position collectible ahead of the player's view
                Vector3 spawnPos = new Vector3(spawnDistance + 5f, collectibleY, 0f); // Spawn slightly further than obstacles

                // Check if this position is clear
                if (IsCollectibleSpawnPositionClear(spawnPos))
                {
                    GameObject newCollectible = Instantiate(cigarettePackPrefab, spawnPos, Quaternion.identity, collectibleParent);

                    // Add a tag to identify it as a collectible
                    newCollectible.tag = "Collectible";

                    Debug.Log("Cigarette pack spawned at: " + spawnPos);
                }
            }

            // Wait before checking for next collectible spawn (longer interval than obstacles)
            yield return new WaitForSeconds(Random.Range(2f, 4f));
        }
    }

    // Helper method to check if a spawn position is clear of other obstacles
    private bool IsSpawnPositionClear(Vector3 spawnPosition)
    {
        // Check all existing obstacles
        foreach (Transform obstacle in obstacleParent)
        {
            if (obstacle == null) continue;

            // Calculate distance between spawn position and existing obstacle
            float distance = Vector3.Distance(spawnPosition, obstacle.position);

            // If too close, position is not clear
            if (distance < minObstacleSpacing)
            {
                return false;
            }
        }

        return true; // Position is clear
    }

    // Helper method to check if a collectible spawn position is clear
    private bool IsCollectibleSpawnPositionClear(Vector3 spawnPosition)
    {
        // Check against obstacles
        if (!IsSpawnPositionClear(spawnPosition))
            return false;

        // Check against other collectibles
        if (collectibleParent != null)
        {
            foreach (Transform collectible in collectibleParent)
            {
                if (collectible == null) continue;

                float distance = Vector3.Distance(spawnPosition, collectible.position);
                if (distance < minObstacleSpacing)
                {
                    return false;
                }
            }
        }

        return true;
    }


    IEnumerator AnimateObstacles()
    {
        while (true)
        {
            // MODIFIED: Continue obstacle animations during win animation
            if (!isGameOver && !isHalted) // Removed the hasWon condition
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
        timerStopped = true; // Stop the timer immediately when goal is reached
        hasWon = true; // Set hasWon immediately so timer stops at the exact moment

        // Hide gameplay UI elements when win animation starts
        HideGameplayUI();

        // Start the win animation sequence
        StartCoroutine(PlayWinScene());
    }

    private void DisablePlayerCollision()
    {
        // Disable all colliders attached to the player car
        Collider2D[] colliders = playerCar.GetComponentsInChildren<Collider2D>();
        foreach (Collider2D collider in colliders)
        {
            collider.enabled = false;
        }

        // Also disable the collision script
        PlayerCollision playerCollision = playerCar.GetComponent<PlayerCollision>();
        if (playerCollision != null)
        {
            playerCollision.enabled = false;
        }

        Debug.Log("Player collisions disabled for win animation");
    }

    IEnumerator PlayWinScene()
    {
        isWinAnimationPlaying = true;
        isCameraFixedAtGasStation = false; // Reset camera control flag at start

        // Disable player collision detection during win animation
        DisablePlayerCollision();
        int savedGear = currentGear;
        // Override HasWon() to return false during animation so that animations keep running
        hasWon = false; // Temporarily set to false to allow animations to continue
        // Store original values
        originalBackgroundScrollSpeed = backgroundScrollSpeed;
        playerStartPosition = playerCar.transform.position;
        originalCameraPosition = Camera.main.transform.position;
        float startGearSpeed = worldSpeed;

        // PHASE 1: Initial drive forward while background continues looping
        float driveDuration = forwardDriveDistance / forwardDriveSpeed;
        float timer = 0f;
        Vector3 startPos = playerCar.transform.position;
        Vector3 forwardPos = startPos + new Vector3(forwardDriveDistance, 0, 0);

        Debug.Log("Win Phase 1: Initial player drive forward");

        // First phase: Car drives forward while background keeps scrolling
        while (timer < driveDuration)
        {
            // Move player car forward
            playerCar.transform.position = Vector3.Lerp(
                startPos,
                forwardPos,
                timer / driveDuration);

            // Keep scrolling background at normal speed
            ScrollBackgroundManually(worldSpeed * backgroundScrollSpeed);
            MoveObstaclesManually(worldSpeed);

            // Update timer
            timer += Time.deltaTime;
            yield return null;
        }

        // Ensure player reaches exact position
        playerCar.transform.position = forwardPos;

        // PHASE 2: Wait for obstacles to clear or timeout
        Debug.Log("Win Phase 2: Waiting for obstacles to clear");

        timer = 0f;
        bool obstaclesCleared = false;

        // Continue with fixed speed (using a slightly higher gear speed for faster clearing)
        float clearingSpeed = gearSpeeds[Mathf.Min(currentGear + 1, gearSpeeds.Length - 1)];

        // Wait until all obstacles are gone or timeout is reached
        while (timer < obstacleTimeout)
        {
            // Check if there are any obstacles left
            int obstacleCount = obstacleParent.childCount;

            if (obstacleCount == 0)
            {
                obstaclesCleared = true;
                Debug.Log("All obstacles cleared!");
                break;
            }

            // Continue scrolling background and moving obstacles
            ScrollBackgroundManually(clearingSpeed * backgroundScrollSpeed);
            MoveObstaclesManually(clearingSpeed);

            // Update timer
            timer += Time.deltaTime;
            yield return null;
        }

        if (!obstaclesCleared)
        {
            Debug.Log("Obstacle timeout reached, destroying remaining obstacles");
            // Destroy any remaining obstacles
            foreach (Transform child in obstacleParent)
            {
                Destroy(child.gameObject);
            }
        }

        // PHASE 3: Zoom car off to the right (player drives away)
        Debug.Log("Win Phase 3: Player zooming off-screen");

        // Calculate the zoom-off endpoint (off the right side of the screen)
        Vector3 zoomOffPos = forwardPos + new Vector3(zoomOffDistance, 0, 0);
        float zoomOffDuration = zoomOffDistance / zoomOffSpeed;

        // Get player sprite renderer for fading out later
        SpriteRenderer playerSprite = playerCar.GetComponent<SpriteRenderer>();

        timer = 0f;
        bool cameraStopped = false;
        Vector3 cameraFixedPosition = Camera.main.transform.position;
        float screenEdgeX = Camera.main.transform.position.x + (Camera.main.orthographicSize * Camera.main.aspect);

        // Zoom the car off to the right
        while (timer < zoomOffDuration)
        {
            float t = timer / zoomOffDuration;

            // Accelerating movement for zoom effect (ease-in)
            float easedT = Mathf.Pow(t, 0.7f); // Subtle acceleration curve

            // Move player car rapidly to the right
            playerCar.transform.position = Vector3.Lerp(
                forwardPos,
                zoomOffPos,
                easedT);

            // Gradually fade out the sprite as it approaches the edge of the screen
            if (playerSprite != null)
            {
                // Calculate how close the car is to the screen edge
                float distanceToEdge = screenEdgeX - playerCar.transform.position.x;
                // Start fading when car is within 80% of the way to screen edge
                if (distanceToEdge < 4.0f)
                {
                    // Map distance to alpha (4 units from edge = 1, 0 units = 0)
                    float alpha = Mathf.Clamp01(distanceToEdge / 4.0f);
                    Color fadeColor = playerSprite.color;
                    fadeColor.a = alpha;
                    playerSprite.color = fadeColor;
                }
            }

            // If using fixed camera, stop camera following after threshold
            if (useCameraFixedPosition && !cameraStopped)
            {
                float distanceMoved = Vector3.Distance(forwardPos, playerCar.transform.position);
                if (distanceMoved > cameraFollowThreshold)
                {
                    cameraStopped = true;
                    cameraFixedPosition = Camera.main.transform.position;
                }
            }

            // Keep camera at fixed position if we've passed the threshold
            if (cameraStopped)
            {
                Camera.main.transform.position = cameraFixedPosition;
            }

            // Continue scrolling background at a decreasing rate to simulate car driving away
            float scrollFactor = Mathf.Lerp(1.0f, 0.2f, t);
            ScrollBackgroundManually(clearingSpeed * backgroundScrollSpeed * scrollFactor);

            // Update timer
            timer += Time.deltaTime;
            yield return null;
        }

        // Completely hide the car sprite (don't just disable the GameObject)
        if (playerSprite != null)
        {
            Color transparent = playerSprite.color;
            transparent.a = 0f;
            playerSprite.color = transparent;
        }

        // PHASE 4: Slow down the scrolling to a stop
        Debug.Log("Win Phase 4: Slowing background to a stop");

        float slowdownDuration = .5f;
        timer = 0f;

        while (timer < slowdownDuration)
        {
            float t = timer / slowdownDuration;
            float currentSpeed = Mathf.Lerp(clearingSpeed * 0.2f, 0, t); // Start from 20% speed

            // Scroll background at gradually decreasing speed
            ScrollBackgroundManually(currentSpeed * backgroundScrollSpeed);

            timer += Time.deltaTime;
            yield return null;
        }
        // PHASE 5: Transition to gas station background with road extensions for better looping
        Debug.Log("Win Phase 5: Transitioning to gas station background with road extensions");

        // Create a new parent for our composite background
        GameObject compositeBackground = new GameObject("CompositeBackground");
        compositeBackground.transform.position = new Vector3(Camera.main.transform.position.x, 0, 0);

        // Create road sections around the gas station for better looping
        // Get the width of road background for positioning
        float roadSectionWidth = (roadBackground != null) ?
            roadBackground.GetComponent<SpriteRenderer>().bounds.size.x : 20f;

        // Create road section BEFORE gas station (left side)
        GameObject leftRoad = null;
        if (roadBackground != null)
        {
            leftRoad = Instantiate(roadBackground, compositeBackground.transform);
            leftRoad.name = "LeftRoadSection";
            float leftRoadX = -roadSectionWidth / 2 - 9.5f; // Position left of center with small overlap
            leftRoad.transform.position = new Vector3(Camera.main.transform.position.x + leftRoadX, 0, 0);

            // Make road initially transparent
            SpriteRenderer leftRoadRenderer = leftRoad.GetComponent<SpriteRenderer>();
            if (leftRoadRenderer != null)
            {
                Color transparent = leftRoadRenderer.color;
                transparent.a = 0f;
                leftRoadRenderer.color = transparent;
            }
        }

        // Create gas station background centered on screen
        GameObject gasStationBackground = null;
        if (gasStationBackgroundPrefab != null)
        {
            // Position the gas station background at the center of the camera view
            gasStationBackground = Instantiate(
                gasStationBackgroundPrefab,
                new Vector3(Camera.main.transform.position.x, 0, 0),
                Quaternion.identity,
                compositeBackground.transform);

            // Hide it initially (we'll fade it in)
            SpriteRenderer gasStationRenderer = gasStationBackground.GetComponent<SpriteRenderer>();
            if (gasStationRenderer != null)
            {
                Color transparent = gasStationRenderer.color;
                transparent.a = 0f;
                gasStationRenderer.color = transparent;
            }
        }

        // Store the ideal camera position for the gas station scene
        gasStationCameraPosition = Camera.main.transform.position;
        isCameraFixedAtGasStation = true;

        // Then add a new method to fix camera position
        void LateUpdate()
        {
            // If we're in the gas station scene, make sure camera stays put
            if (isCameraFixedAtGasStation && Camera.main != null)
            {
                Camera.main.transform.position = new Vector3(
                    gasStationCameraPosition.x,
                    gasStationCameraPosition.y,
                    gasStationCameraPosition.z
                );
            }
        }

        // Create road section AFTER gas station (right side)
        GameObject rightRoad = null;
        if (roadBackground != null)
        {
            rightRoad = Instantiate(roadBackground, compositeBackground.transform);
            rightRoad.name = "RightRoadSection";
            float rightRoadX = roadSectionWidth / 2 + 2f; // Position right of center with small overlap
            rightRoad.transform.position = new Vector3(Camera.main.transform.position.x + rightRoadX, 0, 0);

            // Make road initially transparent
            SpriteRenderer rightRoadRenderer = rightRoad.GetComponent<SpriteRenderer>();
            if (rightRoadRenderer != null)
            {
                Color transparent = rightRoadRenderer.color;
                transparent.a = 0f;
                rightRoadRenderer.color = transparent;
            }
        }

        // Spawn gas station in the center of the screen
        GameObject gasStation = null;
        if (gasStationPrefab != null)
        {
            // Position gas station at the center of the screen
            gasStation = Instantiate(
                gasStationPrefab,
                new Vector3(Camera.main.transform.position.x, 0, 0),
                Quaternion.identity,
                compositeBackground.transform);

            // Make it initially transparent
            SpriteRenderer stationRenderer = gasStation.GetComponent<SpriteRenderer>();
            if (stationRenderer != null)
            {
                Color transparent = stationRenderer.color;
                transparent.a = 0f;
                stationRenderer.color = transparent;
            }
        }

        // Fade between backgrounds
        timer = 0f;
        while (timer < gasStationTransitionTime)
        {
            float t = timer / gasStationTransitionTime;

            // Fade out road background
            foreach (SpriteRenderer roadSprite in backgrounds)
            {
                if (roadSprite != null)
                {
                    Color fadeColor = roadSprite.color;
                    fadeColor.a = 1f - t;
                    roadSprite.color = fadeColor;
                }
            }

            // Fade in gas station background with road extensions
            if (gasStationBackground != null)
            {
                SpriteRenderer gasStationRenderer = gasStationBackground.GetComponent<SpriteRenderer>();
                if (gasStationRenderer != null)
                {
                    Color fadeColor = gasStationRenderer.color;
                    fadeColor.a = t;
                    gasStationRenderer.color = fadeColor;
                }
            }

            // Fade in left road section
            if (leftRoad != null)
            {
                SpriteRenderer leftRoadRenderer = leftRoad.GetComponent<SpriteRenderer>();
                if (leftRoadRenderer != null)
                {
                    Color fadeColor = leftRoadRenderer.color;
                    fadeColor.a = t;
                    leftRoadRenderer.color = fadeColor;
                }
            }

            // Fade in right road section
            if (rightRoad != null)
            {
                SpriteRenderer rightRoadRenderer = rightRoad.GetComponent<SpriteRenderer>();
                if (rightRoadRenderer != null)
                {
                    Color fadeColor = rightRoadRenderer.color;
                    fadeColor.a = t;
                    rightRoadRenderer.color = fadeColor;
                }
            }

            // Fade in gas station
            if (gasStation != null)
            {
                SpriteRenderer stationRenderer = gasStation.GetComponent<SpriteRenderer>();
                if (stationRenderer != null)
                {
                    Color fadeColor = stationRenderer.color;
                    fadeColor.a = t;
                    stationRenderer.color = fadeColor;
                }
            }

            // Update timer
            timer += Time.deltaTime;
            yield return null;
        }

        // Ensure final opacity values
        foreach (SpriteRenderer roadSprite in backgrounds)
        {
            if (roadSprite != null)
            {
                Color fadeColor = roadSprite.color;
                fadeColor.a = 0f;
                roadSprite.color = fadeColor;
            }
        }

        if (gasStationBackground != null)
        {
            SpriteRenderer gasStationRenderer = gasStationBackground.GetComponent<SpriteRenderer>();
            if (gasStationRenderer != null)
            {
                Color fadeColor = gasStationRenderer.color;
                fadeColor.a = 1f;
                gasStationRenderer.color = fadeColor;
            }
        }

        if (leftRoad != null)
        {
            SpriteRenderer leftRoadRenderer = leftRoad.GetComponent<SpriteRenderer>();
            if (leftRoadRenderer != null)
            {
                Color fadeColor = leftRoadRenderer.color;
                fadeColor.a = 1f;
                leftRoadRenderer.color = fadeColor;
            }
        }

        if (rightRoad != null)
        {
            SpriteRenderer rightRoadRenderer = rightRoad.GetComponent<SpriteRenderer>();
            if (rightRoadRenderer != null)
            {
                Color fadeColor = rightRoadRenderer.color;
                fadeColor.a = 1f;
                rightRoadRenderer.color = fadeColor;
            }
        }

        if (gasStation != null)
        {
            SpriteRenderer stationRenderer = gasStation.GetComponent<SpriteRenderer>();
            if (stationRenderer != null)
            {
                Color fadeColor = stationRenderer.color;
                fadeColor.a = 1f;
                stationRenderer.color = fadeColor;
            }
        }
        // Lock camera position at gas station 
        // Fix the camera at the gas station center position
        gasStationCameraPosition = Camera.main.transform.position;
        isCameraFixedAtGasStation = true;
        Debug.Log("Camera locked at gas station position: " + gasStationCameraPosition);


        // PHASE 6: Move car from off-screen LEFT to gas station
        Debug.Log("Win Phase 6: Moving car from left to gas station");

        // Reset car position to just off-screen to the LEFT and near the BOTTOM of the screen
        float offScreenX = Camera.main.transform.position.x - (Camera.main.orthographicSize * Camera.main.aspect) - 3f;
        float fixedEntryY = minYPosition + (laneWidth * 0.25f); // Position at 1/4 up from the bottom of the road

        // Move the car to the off-screen position while it's still invisible
        playerCar.transform.position = new Vector3(offScreenX, fixedEntryY, playerStartPosition.z);

        // Reset car rotation to normal
        playerCar.transform.rotation = Quaternion.identity;
        playerCar.transform.localScale = Vector3.one;

        // Prepare to slowly fade in the car as it enters
        SpriteRenderer carRenderer = playerCar.GetComponent<SpriteRenderer>();
        if (carRenderer != null)
        {
            Color transparent = carRenderer.color;
            transparent.a = 0f;
            carRenderer.color = transparent;
        }

        // Calculate target position for parking - slightly left of center
        float parkX = 0;
        if (gasStation != null)
        {
            parkX = gasStation.transform.position.x + finalParkPosition;
        }
        else
        {
            parkX = Camera.main.transform.position.x + finalParkPosition;
        }

        // Define parking position
        Vector3 parkPosition = new Vector3(parkX, fixedEntryY, playerStartPosition.z);

        // Calculate entry point (a bit inside the screen)
        float entryX = Camera.main.transform.position.x - (Camera.main.orthographicSize * Camera.main.aspect) + 2f;
        Vector3 entryPosition = new Vector3(entryX, fixedEntryY, playerStartPosition.z);

        // First, move from off-screen to entry point while fading in
        float entryDuration = 0.5f; // 0.5 seconds to enter the screen
        timer = 0f;

        while (timer < entryDuration)
        {
            float t = timer / entryDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            // Move car to entry point
            playerCar.transform.position = Vector3.Lerp(
                new Vector3(offScreenX, fixedEntryY, playerStartPosition.z),
                entryPosition,
                smoothT);

            // Fade in the car sprite
            if (carRenderer != null)
            {
                Color fadeColor = carRenderer.color;
                fadeColor.a = t; // Linear fade from 0 to 1
                carRenderer.color = fadeColor;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // Now move from entry point to parking position
        timer = 0f;
        while (timer < parkingDuration)
        {
            float t = timer / parkingDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            // Move player car toward gas station from entry point
            playerCar.transform.position = Vector3.Lerp(
                entryPosition,
                parkPosition,
                smoothT);

            timer += Time.deltaTime;
            yield return null;
        }

        // Ensure player reaches exact position
        playerCar.transform.position = parkPosition;

        // Add a small celebration effect
        if (playerCar != null)
        {
            // Small bounce effect when parked
            playerCar.transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0), 0.5f, 10, 0.5f)
                .SetEase(Ease.OutElastic);
        }
        // Wait a moment before showing win panel
        yield return new WaitForSeconds(0.5f);

        // Update and show time texts in the gas station prefab if they exist
        if (currentTimeText != null)
        {
            // Format time with 2 decimal places
            currentTimeText.text = gameTimer.ToString("F2");
            currentTimeText.gameObject.SetActive(true);

            // Check if this is a new best time
            if (gameTimer < bestTime)
            {
                bestTime = gameTimer;
            }
        }

        if (bestTimeText != null)
        {
            bestTimeText.text = bestTime.ToString("F2");
            bestTimeText.gameObject.SetActive(true);
        }

        // Show the leaderboard after the win animation completes
        if (leaderboardManager != null)
        {
            leaderboardManager.ShowGasStationLeaderboard();
        }

        // Display win panel at the end of the animation
        if (winPanel != null)
        {
            winPanel.SetActive(true);
        }

        isWinAnimationPlaying = false;
        hasWon = true; // Now set it back to true when we're done
    }

    // Helper method to manually scroll background during win animation
    private void ScrollBackgroundManually(float scrollSpeed)
    {
        if (useRepeatingBackground)
        {
            // For sprite-based repeating backgrounds
            if (backgrounds.Count > 0)
            {
                // Move all background pieces
                foreach (SpriteRenderer bg in backgrounds)
                {
                    if (bg == null) continue;

                    bg.transform.position += Vector3.left * scrollSpeed * Time.deltaTime;

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
                // Get current offset
                Vector2 currentOffset = renderer.material.mainTextureOffset;

                // Add to the X offset
                float newOffset = currentOffset.x + scrollSpeed * Time.deltaTime;

                // Set the new offset, keeping it in the 0-1 range
                renderer.material.mainTextureOffset = new Vector2(newOffset % 1, currentOffset.y);
            }
        }
    }

    // Helper method to manually move obstacles during win animation
    private void MoveObstaclesManually(float currentSpeed)
    {
        // Get a reference speed (2nd gear speed) to compare against
        float gearTwoSpeed = gearSpeeds.Length > 1 ? gearSpeeds[1] : 10f;

        // Special minimum speed for 1st gear to ensure forward movement
        float minimumRelativeSpeed = 3f;

        foreach (Transform obstacle in obstacleParent)
        {
            if (obstacle == null) continue;

            // Get obstacle's speed factor if it has a movement component
            ObstacleMovement obstacleMovement = obstacle.GetComponent<ObstacleMovement>();
            float speedFactor = 0.7f; // Default speed factor

            if (obstacleMovement != null)
            {
                speedFactor = obstacleMovement.GetCurrentSpeedFactor();
            }

            // Calculate base movement speed
            float baseSpeed = gearTwoSpeed * speedFactor;

            // Calculate relative speed between player and obstacle
            float relativeSpeed = currentSpeed - baseSpeed;

            // Special handling for 1st gear - ensure obstacles always move forward
            if (currentGear == 0 && relativeSpeed <= 0)
            {
                // Force a minimum positive relative speed in 1st gear
                relativeSpeed = minimumRelativeSpeed;
            }

            // Move obstacle based on relative speed
            obstacle.position += Vector3.left * relativeSpeed * Time.deltaTime;

            // Remove obstacles that have gone past the player
            if (obstacle.position.x < -despawnDistance)
            {
                Destroy(obstacle.gameObject);
            }
        }
    }



    // Call this from a collision detection script on the player
    public void OnPlayerHitObstacle(GameObject hitObstacle)
    {
        Debug.Log("Obstacle Hit!");

        // If boost is active, destroy the obstacle without penalty
        if (isBoostActive)
        {
            Debug.Log("Obstacle destroyed by boost!");

            // Destroy the obstacle
            if (hitObstacle != null)
            {
                // Visual effect for destroying obstacle during boost
                hitObstacle.transform.DOPunchScale(new Vector3(1.5f, 1.5f, 0), 0.2f, 10, 1f)
                    .OnComplete(() => {
                        if (hitObstacle != null)
                            Destroy(hitObstacle);
                    });
            }
            return; // Don't halt or reset gear during boost
        }

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

    // Call this from collision detection when collecting cigarette pack
    public void OnPlayerCollectBoost(GameObject collectible)
    {
        Debug.Log("Cigarette pack collected!");

        // Add boost charge
        AddBoostCharge();

        // Destroy the collectible
        if (collectible != null)
        {
            // Visual effect for collecting
            collectible.transform.DOPunchScale(new Vector3(1.5f, 1.5f, 0), 0.3f, 10, 1f)
                .OnComplete(() => {
                    if (collectible != null)
                        Destroy(collectible);
                });
        }
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
        // Show gameplay UI elements on restart
        ShowGameplayUI();

        // Ensure leaderboard is completely hidden when restarting
        if (leaderboardManager != null)
        {
            leaderboardManager.HideGasStationLeaderboard();
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

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