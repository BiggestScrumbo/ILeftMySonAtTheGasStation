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
    public GearSpriteDisplay gearSpriteDisplay;
    public GoalProgressBar goalProgressBar;
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

    // Start is called before the first frame update
    void Start()
    {
        if (playerCar == null)
            playerCar = GameObject.FindGameObjectWithTag("Player");

        if (obstacleParent == null)
            obstacleParent = new GameObject("Obstacles").transform;
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
        if (isGameOver || hasWon) return;

        // Always update UI regardless of halt state
        UpdateDistanceText();

        // If win animation is playing, let the coroutine handle movement
        if (isWinAnimationPlaying || hasWon)
        {
            UpdateSpeedLines(); // Turn off speed lines during win animation
            return;
        }

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

        // Track virtual distance traveled
        distanceTraveled += worldSpeed * Time.deltaTime;

        // Update progress bar
        if (goalProgressBar != null)
        {
            goalProgressBar.UpdateProgress(distanceTraveled);
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
                // To this:
                speedFactor = obstacleMovement.GetCurrentSpeedFactor();
            }

            // Calculate base movement speed
            float baseSpeed = gearTwoSpeed * speedFactor;

            // Calculate relative speed between player and obstacle
            float relativeSpeed = worldSpeed - baseSpeed;

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




    // Scroll the background based on worldSpeed
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
            gearText.gameObject.SetActive(false); // Hide the text display

        // Update the gear sprite if available
        if (gearSpriteDisplay != null)
        {
            gearSpriteDisplay.UpdateGearSprite(currentGear);
        }
    }

    private void UpdateTimerText()
    {
        if (timerText != null)
        {
            string timerDisplay = isHalted ? gameTimer.ToString() : gameTimer.ToString("F2");
            timerText.text = timerDisplay;
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
        hasWon = true;

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

        // Turn on the speed lines with maximum intensity for zoom effect
        if (speedLines != null)
        {
            var emission = speedLines.emission;
            emission.rateOverTime = maxSpeedLineRate * 2; // More intense than normal max speed

            var main = speedLines.main;
            main.startColor = speedLineColor; // Full opacity
        }

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

        float slowdownDuration = 1.0f;
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
            float leftRoadX = -roadSectionWidth / 2 - 2f; // Position left of center with small overlap
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
