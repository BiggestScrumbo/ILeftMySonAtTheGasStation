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
    public float backgroundScrollSpeed = 0.05f; // Multiplier for background scrolling
    public float[] normalModeBackgroundMultipliers = { 1f, 1.1f, 1.2f, 1.4f, 1.6f }; // Gear-based multipliers for normal mode
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

    [Header("Boost Visual Effects")]
    public ParticleSystem speedLinesParticleSystem; // Assign your speedlines particle system in inspector
    public float speedLinesEmissionRate = 150f; // How many particles per second during boost
    public float speedLinesIntensity = 1f; // Overall intensity multiplier
    public bool enableSpeedLinesParticles = true; // Toggle speedlines on/off

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

    [Header("Speed System - Inverse World Speed")]
    public float[] worldSpeedByGear = { 15f, 12f, 9f, 6f, 3f }; // Decreases with higher gears
    public float[] backgroundSpeedMultipliers = { 1f, 1.3f, 1.7f, 2.2f, 2.8f }; // Your original values
    public float backgroundIntensity = 0.5f; // Global multiplier to tone down the effect (0.5 = 50% intensity)
    public bool useInverseSpeedSystem = true; // Toggle to enable/disable the new system

    [Header("Collision Effects")]
    public bool enableSpinAnimation = true;
    public float spinDuration = 1f;
    public float spinRotations = 3f;
    public bool enableBounceEffect = true;
    public bool enableFlashingEffect = true;

    // For repeating backgrounds
    private List<SpriteRenderer> backgrounds = new List<SpriteRenderer>();
    private float backgroundSize;

    [Header("Invulnerability System")]
    public float invulnerabilityDuration = 0.5f; // How long the player is invulnerable after getting hit
    public float flashFrequency = 0.1f; // How fast the player flashes during invulnerability
    private bool isInvulnerable = false; // Whether the player is currently invulnerable
    private float invulnerabilityTimer = 0f; // Timer for invulnerability duration

    [Header("Obstacle Set System")]
    public GameObject[] handcraftedObstacleSets; // Array of pre-assembled obstacle set prefabs
    public float obstacleSetInterval = 100f; // Distance between handcrafted sets (100m)
    public float obstacleSetSpawnDistance = 30f; // How far ahead to spawn obstacle sets
    public float minRandomObstacleGap = 20f; // ADDED: Minimum distance of random obstacles after each set
    public bool useObstacleSets = true; // Toggle the alternating system on/off

    [Header("Countdown System")]
    public TMP_Text countdownText; // Text element for displaying countdown (3, 2, 1, GO!)
    public float countdownDuration = 1f; // How long each countdown number is displayed
    public float countdownTextSize = 72f; // Size of countdown text
    public Color countdownColor = Color.white; // Color of countdown text
    private bool isCountdownActive = false; // Whether countdown is currently running
    public GameObject CountdownIndicatorUp; // Reference to the up arrow key image
    public GameObject CountdownIndicatorDown; // Reference to the down arrow key image
    public GameObject CountdownIndicatorLeft; // Reference to the left arrow key image
    public GameObject CountdownIndicatorRight; // Reference to the right arrow key image
    private bool gameStarted = false; // Whether the actual game has started (after countdown)

    // Add this to the top of the GameController class, after the existing headers
    [Header("Pause System")]
    public PauseMenuController pauseMenuController;

    // Private variables for tracking obstacle sets
    private float lastObstacleSetDistance = 0f; // Track when we last spawned a set
    private bool isObstacleSetActive = false; // Whether we're currently in an obstacle set phase
    private GameObject currentObstacleSet = null; // Reference to the active obstacle set

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

    [Header("Set Dressing")]
    public TrafficLightOverlay trafficLightOverlay; // Add this line

    // Make distanceTraveled accessible to TrafficLightOverlay
    public float DistanceTraveled => distanceTraveled;

    [Header("Win Scene")]
    public GameObject gasStationPrefab; // Assign your gas station sprite in inspector
    public GameObject gasStationBackgroundPrefab; // Gas station background prefab
    public GameObject characterPrefab; // NEW: Assign your character prefab with sprite sheet
    public float characterAnimationFrameRate = 0.5f; // NEW: How fast to swap character frames
    public float characterSpawnDelay = 1.0f; // NEW: Delay before character appears after car parks
    public Vector3 characterOffset = Vector3.zero; // NEW: Offset from car parking position
    public float forwardDriveSpeed = 10f; // How fast the car drives forward
    public float forwardDriveDistance = 5f; // How far the car goes forward
    public float obstacleTimeout = 5f; // Maximum time to wait for obstacles to clear
    public float gasStationTransitionTime = 1.5f; // How long it takes to fade between backgrounds
    public float parkingDuration = 2.0f; // How long it takes to park
    public float finalParkPosition = -3.0f; // X position where car parks (negative = left of center)

    // New parameters for zoom-off sequence
    private Vector3 gasStationCameraPosition; // Stores fixed position for gas station view
    private bool isCameraFixedAtGasStation = false;
    private GameObject spawnedCharacter = null; // NEW: Reference to spawned character
    private bool isCharacterAnimating = false; // NEW: Track character animation state
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

        // MODIFIED: Ensure MusicController is ready and start music/ambiance
        StartCoroutine(InitializeAudioSystem());

        // Start the countdown sequence
        StartCoroutine(StartCountdownSequence());

        // Setup UI
        UpdateGearText();
        UpdateBoostUI(); // Initialize boost UI

        // ADDED: Ensure win screen elements are properly hidden at scene start
        HideWinScreenElements();

        // Initially hide gameplay UI (will be shown after countdown)
        HideGameplayUIDuringCountdown();

        SetupPauseSystem();

        SetupSpeedLinesParticleSystem();

        // Ensure leaderboard is hidden during gameplay
        if (leaderboardManager != null)
        {
            leaderboardManager.HideGasStationLeaderboard();
        }

        // Hide end game panels
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);

        if (useInverseSpeedSystem)
        {
            // Ensure arrays are properly sized
            if (worldSpeedByGear.Length != gearSpeeds.Length)
            {
                Debug.LogWarning("worldSpeedByGear array size doesn't match gearSpeeds array size!");
            }
            if (backgroundSpeedMultipliers.Length != gearSpeeds.Length)
            {
                Debug.LogWarning("backgroundSpeedMultipliers array size doesn't match gearSpeeds array size!");
            }

            worldSpeed = worldSpeedByGear[currentGear];
            Debug.Log("Inverse speed system enabled!");
        }
        else
        {
            worldSpeed = gearSpeeds[currentGear];
            Debug.Log("Classic speed system enabled!");
        }
        if (useInverseSpeedSystem)
        {
            worldSpeed = worldSpeedByGear[currentGear];
        }
        else
        {
            worldSpeed = gearSpeeds[currentGear]; // Keep original behavior as fallback
        }

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

        LoadBestTime();

        // REMOVED: Don't play countdown SFX immediately in Start()
        // The countdown SFX will be played in the StartCountdownSequence() coroutine
    }

    // NEW: Add this coroutine to properly initialize audio system
    private IEnumerator InitializeAudioSystem()
    {
        // Wait one frame to ensure everything is loaded
        yield return null;

        // Ensure MusicController instance exists and is ready
        if (MusicController.Instance != null)
        {
            // Start gameplay music and ambiance
            MusicController.Instance.PlayGameplayMusic();
            Debug.Log("Audio system initialized successfully");
        }
        else
        {
            Debug.LogError("MusicController.Instance is null! Make sure MusicController exists in the scene.");
        }
    }
    // Helper method to calculate current vertical movement speed based on gear
    // Helper method to calculate current vertical movement speed based on gear
    private void UpdateVerticalMoveSpeed()
    {
        // Calculate vertical movement speed: base speed + (gear level * multiplier)
        currentVerticalMoveSpeed = verticalMoveSpeed + (currentGear * verticalSpeedMultiplier);

        // Enhanced debug info
        if (useInverseSpeedSystem)
        {
            Debug.Log($"Gear {currentGear + 1}: Vertical speed = {currentVerticalMoveSpeed:F1}, " +
                      $"World speed = {worldSpeedByGear[currentGear]:F1}, " +
                      $"BG multiplier = {backgroundSpeedMultipliers[currentGear]:F1}x");
        }
        else
        {
            Debug.Log($"Gear {currentGear + 1}: Vertical speed = {currentVerticalMoveSpeed:F1}, " +
                      $"World speed = {gearSpeeds[currentGear]:F1} (classic mode)");
        }
    }

    // Helper method to hide gameplay UI elements during win animation
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

        // Hide boost charges text during win animation
        if (boostChargesText != null)
            boostChargesText.gameObject.SetActive(false);

        // Hide distance text during win animation
        if (distanceText != null)
            distanceText.gameObject.SetActive(false);
    }

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

        // Show boost charges text during normal gameplay
        if (boostChargesText != null)
            boostChargesText.gameObject.SetActive(true);

        // Show distance text during normal gameplay
        if (distanceText != null)
            distanceText.gameObject.SetActive(true);
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

    private void SetupSpeedLinesParticleSystem()
    {
        if (speedLinesParticleSystem == null)
        {
            Debug.LogWarning("Speed lines particle system not assigned! Please assign the particle system in the inspector.");
            return;
        }

        // Configure the particle system for anime-style speedlines
        var main = speedLinesParticleSystem.main;
        main.startLifetime = 0.8f; // How long each line lasts
        main.startSpeed = 25f; // How fast the lines move
        main.startSize = 0.1f; // Thickness of the lines
        main.startColor = Color.white; // Color of the speedlines
        main.maxParticles = 300; // Maximum number of particles
        main.simulationSpace = ParticleSystemSimulationSpace.World; // World space for screen overlay effect

        // Configure emission - start with 0
        var emission = speedLinesParticleSystem.emission;
        emission.rateOverTime = 0f; // Start disabled

        // Configure shape to cover the screen
        var shape = speedLinesParticleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Rectangle;

        // Make the shape cover the camera view
        Camera cam = Camera.main;
        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;
        shape.scale = new Vector3(width * 1.2f, height * 1.2f, 1f); // Slightly larger than screen

        // Position the emitter to cover the screen
        speedLinesParticleSystem.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0);

        // Configure velocity for forward movement (anime speedlines typically move toward the camera/viewer)
        var velocity = speedLinesParticleSystem.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.z = -30f; // Move toward camera (negative Z)
        velocity.x = new ParticleSystem.MinMaxCurve(-5f, 5f); // Small random horizontal movement
        velocity.y = new ParticleSystem.MinMaxCurve(-2f, 2f); // Small random vertical movement

        // Configure size over lifetime for tapering effect
        var sizeOverLifetime = speedLinesParticleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0f); // Start small
        sizeCurve.AddKey(0.3f, 1f); // Grow quickly
        sizeCurve.AddKey(1f, 0f); // Fade out
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Configure color over lifetime for fading effect
        var colorOverLifetime = speedLinesParticleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient colorGradient = new Gradient();
        colorGradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = colorGradient;

        // Configure renderer for line-like appearance
        var renderer = speedLinesParticleSystem.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.5f; // How much the velocity affects the stretching
            renderer.lengthScale = 3f; // How long the stretched particles appear
            renderer.sortingOrder = 100; // High sorting order to appear on top

            // Optional: Set a material for better visual effect
            // renderer.material = yourSpeedLinesMaterial;
        }

        // Start the system but with no emission
        speedLinesParticleSystem.Play();

        Debug.Log("Speed lines particle system configured");
    }

    private void SetupPauseSystem()
    {
        // Find pause menu controller if not assigned
        if (pauseMenuController == null)
        {
            pauseMenuController = FindObjectOfType<PauseMenuController>();
        }

        // Initially disable pause during countdown
        if (pauseMenuController != null)
        {
            pauseMenuController.DisablePause();
        }
    }

    // Activate boost mode
    private void ActivateBoost()
    {
        if (currentBoostCharges <= 0 || isBoostActive || !gameStarted) return;
        MusicController.Instance.PlayBoostActiveSFX();
        currentBoostCharges--;
        isBoostActive = true;
        boostTimer = boostDuration;

        Debug.Log($"Boost activated! Charges remaining: {currentBoostCharges}");

        // START: Add speedlines particle effect
        if (enableSpeedLinesParticles && speedLinesParticleSystem != null)
        {
            ActivateSpeedLinesParticles();
        }
        // END: Add speedlines particle effect

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

    private void DeactivateBoost()
    {
        isBoostActive = false;
        boostTimer = 0f;

        Debug.Log("Boost deactivated");

        // START: Deactivate speedlines particles
        if (speedLinesParticleSystem != null)
        {
            DeactivateSpeedLinesParticles();
        }
        // END: Deactivate speedlines particles

        // Stop visual effects
        SpriteRenderer playerSprite = playerCar.GetComponent<SpriteRenderer>();
        if (playerSprite != null)
        {
            DOTween.Kill("BoostFlash");

            // Only reset to white if not currently invulnerable (to avoid overriding invulnerability flashing)
            if (!isInvulnerable)
            {
                playerSprite.color = Color.white; // Reset to normal color
            }
        }

        UpdateBoostUI();
    }

    private void ActivateSpeedLinesParticles()
    {
        if (!enableSpeedLinesParticles || speedLinesParticleSystem == null) return;

        // Enable emission
        var emission = speedLinesParticleSystem.emission;

        // Animate the emission rate for smooth activation
        DOTween.To(() => emission.rateOverTime.constant,
                   x => {
                       var em = speedLinesParticleSystem.emission;
                       em.rateOverTime = x * speedLinesIntensity;
                   },
                   speedLinesEmissionRate,
                   0.3f)
               .SetEase(Ease.OutQuad);

        Debug.Log("Speed lines particles activated");
    }
    private void DeactivateSpeedLinesParticles()
    {
        if (speedLinesParticleSystem == null) return;

        // Animate emission rate down to 0
        var emission = speedLinesParticleSystem.emission;

        DOTween.To(() => emission.rateOverTime.constant,
                   x => {
                       var em = speedLinesParticleSystem.emission;
                       em.rateOverTime = x;
                   },
                   0f,
                   0.5f)
               .SetEase(Ease.InQuad);

        Debug.Log("Speed lines particles deactivated");
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
        // If player is halted or game hasn't started, don't process obstacles
        if (isHalted || !gameStarted)
            return;

        // Check if our current obstacle set has passed the player
        if (isObstacleSetActive && currentObstacleSet != null)
        {
            // Check if ALL child obstacles in the set have passed the despawn point
            bool allObstaclesPassed = true;
            int childCount = currentObstacleSet.transform.childCount;
            int passedCount = 0;

            foreach (Transform child in currentObstacleSet.transform)
            {
                if (child != null)
                {
                    if (child.position.x <= -despawnDistance)
                    {
                        passedCount++;
                    }
                    else
                    {
                        allObstaclesPassed = false;
                    }
                }
            }

            // Also check if the parent container itself has passed
            bool parentPassed = currentObstacleSet.transform.position.x < -despawnDistance;

            // Debug information
            Debug.Log($"Obstacle Set Status: Parent passed={parentPassed}, " +
                      $"Children passed={passedCount}/{childCount}, " +
                      $"Set position={currentObstacleSet.transform.position.x:F1}");

            // End obstacle set phase only when both conditions are met:
            // 1. The parent container has moved past despawn point, AND
            // 2. All individual obstacles within the set have also passed
            if (parentPassed && allObstaclesPassed)
            {
                Debug.Log("All obstacles in set have passed, returning to random spawning");

                // FIXED: Reset the obstacle set timer to current distance to prevent immediate respawning
                lastObstacleSetDistance = distanceTraveled;
                isObstacleSetActive = false;
                currentObstacleSet = null; // Will be destroyed by the normal cleanup below

                Debug.Log($"Obstacle set timer reset. Next set will spawn at distance {distanceTraveled + obstacleSetInterval:F1}m");
            }
        }

        // Get a reference speed (2nd gear speed) to compare against
        float gearTwoSpeed = gearSpeeds.Length > 1 ? gearSpeeds[1] : 10f;
        float minimumRelativeSpeed = 3f;

        foreach (Transform obstacle in obstacleParent)
        {
            if (obstacle == null) continue;

            // Handle obstacle set movement (they move as a whole unit)
            if (obstacle == currentObstacleSet)
            {
                // Move the entire obstacle set
                float currentPlayerSpeed = isBoostActive ? boostSpeed : worldSpeed;
                float relativeSpeed = currentPlayerSpeed * 0.7f; // Obstacle sets move at 70% of player speed

                // Special handling for 1st gear
                if (currentGear == 0 && relativeSpeed <= 0 && !isBoostActive)
                {
                    relativeSpeed = minimumRelativeSpeed;
                }

                obstacle.position += Vector3.left * relativeSpeed * Time.deltaTime;
            }
            else
            {
                // Handle individual obstacle movement (your existing logic)
                ObstacleMovement obstacleMovement = obstacle.GetComponent<ObstacleMovement>();
                float speedFactor = 0.7f;

                if (obstacleMovement != null)
                {
                    speedFactor = obstacleMovement.GetCurrentSpeedFactor();
                }

                float baseSpeed = gearTwoSpeed * speedFactor;
                float currentPlayerSpeed = isBoostActive ? boostSpeed : worldSpeed;
                float relativeSpeed = currentPlayerSpeed - baseSpeed;

                if (currentGear == 0 && relativeSpeed <= 0 && !isBoostActive)
                {
                    relativeSpeed = minimumRelativeSpeed;
                }

                obstacle.position += Vector3.left * relativeSpeed * Time.deltaTime;
            }

            // Remove obstacles that have gone past the player
            if (obstacle.position.x < -despawnDistance)
            {
                // If this was our current obstacle set, mark it as inactive
                if (obstacle == currentObstacleSet)
                {
                    Debug.Log("Obstacle set container destroyed, returning to random spawning");

                    // FIXED: Reset the obstacle set timer to current distance
                    lastObstacleSetDistance = distanceTraveled;
                    isObstacleSetActive = false;
                    currentObstacleSet = null;

                    Debug.Log($"Obstacle set timer reset. Next set will spawn at distance {distanceTraveled + obstacleSetInterval:F1}m");
                }

                Destroy(obstacle.gameObject);
            }
        }
    }

    // Move collectibles (cigarette packs)
    private void MoveCollectibles()
    {
        if (isHalted || collectibleParent == null || !gameStarted)
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
        // If player is halted, don't automatically scroll
        if (isHalted)
            return;

        // Debug logging to see if method is being called
        Debug.Log($"ScrollBackground called - gameStarted: {gameStarted}, backgrounds.Count: {backgrounds.Count}");

        // Calculate effective scroll speed
        float baseScrollSpeed;

        // Use slower speed during countdown for visual interest
        if (!gameStarted)
        {
            baseScrollSpeed = 5f; // Fixed speed during countdown
            Debug.Log($"Countdown mode - using fixed speed: {baseScrollSpeed}");
        }
        else
        {
            baseScrollSpeed = isBoostActive ? boostSpeed : worldSpeed;
            Debug.Log($"Game mode - using calculated speed: {baseScrollSpeed}");
        }

        // Apply gear-based multiplier based on which system is active
        float scrollMultiplier = backgroundScrollSpeed;

        if (!gameStarted)
        {
            // During countdown, use a simple multiplier
            scrollMultiplier = 0.1f; // Simple multiplier for countdown
        }
        else if (useInverseSpeedSystem)
        {
            // Calculate the gear effect, then scale it by intensity
            float gearEffect = backgroundSpeedMultipliers[currentGear] - 1f; // Get the bonus part (above 1.0)
            float scaledGearEffect = gearEffect * backgroundIntensity; // Scale the bonus
            float finalMultiplier = 1f + scaledGearEffect; // Add back to base 1.0
            scrollMultiplier *= finalMultiplier;
        }
        else
        {
            // Normal system: use gear-based multipliers
            if (currentGear < normalModeBackgroundMultipliers.Length)
            {
                scrollMultiplier *= normalModeBackgroundMultipliers[currentGear];
            }
        }

        Debug.Log($"Final scroll calculation - baseSpeed: {baseScrollSpeed}, multiplier: {scrollMultiplier}, finalSpeed: {baseScrollSpeed * scrollMultiplier}");

        if (useRepeatingBackground)
        {
            // For sprite-based repeating backgrounds
            if (backgrounds.Count > 0)
            {
                Debug.Log($"Moving {backgrounds.Count} background sprites");
                // Move all background pieces
                foreach (SpriteRenderer bg in backgrounds)
                {
                    if (bg == null)
                    {
                        Debug.LogWarning("Found null background sprite!");
                        continue;
                    }

                    Vector3 oldPosition = bg.transform.position;
                    bg.transform.position += Vector3.left * baseScrollSpeed * Time.deltaTime * scrollMultiplier;
                    Debug.Log($"Background moved from {oldPosition} to {bg.transform.position}");

                    // If background piece has moved off-screen to the left, move it to the right
                    if (bg.transform.position.x < -backgroundSize)
                    {
                        bg.transform.position = new Vector3(
                            bg.transform.position.x + backgroundSize * backgrounds.Count,
                            bg.transform.position.y,
                            bg.transform.position.z);
                        Debug.Log($"Background wrapped to position: {bg.transform.position}");
                    }
                }
            }
            else
            {
                Debug.LogWarning("No backgrounds found in list!");
            }
        }
        else if (roadBackground != null)
        {
            // For shader/material-based scrolling
            Renderer renderer = roadBackground.GetComponent<Renderer>();
            if (renderer != null && renderer.material.mainTexture != null)
            {
                // Scroll the texture
                float offset = Time.time * baseScrollSpeed * scrollMultiplier;
                renderer.material.mainTextureOffset = new Vector2(offset % 1, 0);
                Debug.Log($"Texture offset set to: {renderer.material.mainTextureOffset}");
            }
            else
            {
                Debug.LogWarning("Road background renderer or material is null!");
            }
        }
    }



    // ADDED: New method to hide all win screen elements at scene start
    private void HideWinScreenElements()
    {
        // Hide gas station time display texts (these are shown during win animation)
        if (currentTimeText != null)
            currentTimeText.gameObject.SetActive(false);

        if (bestTimeText != null)
            bestTimeText.gameObject.SetActive(false);

        // Ensure win panel is hidden
        if (winPanel != null)
            winPanel.SetActive(false);

        // Make sure leaderboard is hidden
        if (leaderboardManager != null)
        {
            leaderboardManager.HideGasStationLeaderboard();
        }

        Debug.Log("Win screen elements hidden at scene start");
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

    // NEW: Coroutine to animate the character sprite
    IEnumerator AnimateCharacter()
    {
        while (isCharacterAnimating && spawnedCharacter != null)
        {
            // Get the obstacle animation component (reusing existing system)
            ObstacleAnimation characterAnimation = spawnedCharacter.GetComponent<ObstacleAnimation>();
            if (characterAnimation != null)
            {
                characterAnimation.SwapFrame();
            }

            // Wait before next frame swap
            yield return new WaitForSeconds(characterAnimationFrameRate);
        }
    }

    // NEW: Method to spawn and setup the character
    // NEW: Method to spawn character at gas station (without fade-in effect)
    private void SpawnCharacterForGasStation(Vector3 position)
    {
        if (characterPrefab == null) return;

        // Spawn the character at the specified position
        spawnedCharacter = Instantiate(characterPrefab, position, Quaternion.identity);

        // Add ObstacleAnimation component if it doesn't exist
        ObstacleAnimation characterAnimation = spawnedCharacter.GetComponent<ObstacleAnimation>();
        if (characterAnimation == null)
        {
            characterAnimation = spawnedCharacter.AddComponent<ObstacleAnimation>();
        }

        // Make character initially transparent (will fade in with the background)
        SpriteRenderer characterRenderer = spawnedCharacter.GetComponent<SpriteRenderer>();
        if (characterRenderer != null)
        {
            Color transparent = characterRenderer.color;
            transparent.a = 0f;
            characterRenderer.color = transparent;
        }

        // Start character animation immediately
        isCharacterAnimating = true;
        StartCoroutine(AnimateCharacter());

        Debug.Log("Character spawned at gas station and animation started");
    }

    // NEW: Method to cleanup character when needed
    private void CleanupCharacter()
    {
        if (spawnedCharacter != null)
        {
            isCharacterAnimating = false;
            Destroy(spawnedCharacter);
            spawnedCharacter = null;
            Debug.Log("Character cleaned up");
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
        // Only increment timer if the player hasn't won, timer hasn't been stopped, AND game has started
        if (!timerStopped && gameStarted)
            gameTimer += Time.deltaTime;

        if (isGameOver || hasWon) return;

        // Always update UI regardless of halt state (but only if game has started)
        if (gameStarted)
        {
            UpdateDistanceText();
            UpdateTimerText();
        }

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

        // Handle boost activation with spacebar (only if game has started)
        if (Input.GetKeyDown(KeyCode.Space) && gameStarted)
        {
            ActivateBoost();
        }

        // Update boost timer (only if game has started)
        if (isBoostActive && gameStarted)
        {
            boostTimer -= Time.deltaTime;
            if (boostTimer <= 0f)
            {
                DeactivateBoost();
            }
        }

        if (pauseMenuController != null && pauseMenuController.IsPaused())
        {
            return;
        }

        // Only increment timer if the player hasn't won, timer hasn't been stopped, AND game has started
        if (!timerStopped && gameStarted)
            gameTimer += Time.deltaTime;

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

        // In the Update() method, find this section and modify it:

        // Handle gear changes (left/right) - FIXED: Added parentheses for correct operator precedence
        if ((Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) && currentGear < gearSpeeds.Length - 1)
        {
            currentGear++;
            UpdateGearText();

            // Play gear change sound with pitch based on new gear
            if (MusicController.Instance != null)
            {
                MusicController.Instance.PlayGearChangeSFX(currentGear);
                Debug.Log($"Gear UP to {currentGear + 1} - Playing gear change SFX");
            }
        }
        else if ((Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) && currentGear > 0)
        {
            currentGear--;
            UpdateGearText();

            // Play gear change sound with pitch based on new gear
            if (MusicController.Instance != null)
            {
                MusicController.Instance.PlayGearChangeSFX(currentGear);
                Debug.Log($"Gear DOWN to {currentGear + 1} - Playing gear change SFX");
            }
        }

        // Update world speed based on current gear (but boost overrides this for movement)
        if (useInverseSpeedSystem)
        {
            worldSpeed = worldSpeedByGear[currentGear];
        }
        else
        {
            worldSpeed = gearSpeeds[currentGear];
        }

        // Track virtual distance traveled ONLY if game has started (use boost speed if active)
        if (gameStarted)
        {
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

            // Check if player has virtually reached the goal (gas station)
            if (distanceTraveled >= (endPosition - startPosition))
            {
                Win();
            }
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

        // Update invulnerability system (only if game has started)
        if (gameStarted)
        {
            UpdateInvulnerability();
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

    // New coroutine to handle the countdown sequence
    // New coroutine to handle the countdown sequence
    IEnumerator StartCountdownSequence()
    {
        isCountdownActive = true;
        gameStarted = false;

        // Hide gameplay UI during countdown
        HideGameplayUIDuringCountdown();

        // Setup countdown text if not already configured
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.fontSize = countdownTextSize;
            countdownText.color = countdownColor;
            countdownText.alignment = TextAlignmentOptions.Center;
        }

        // NEW: Play the full countdown SFX sequence ONCE at the beginning
        if (MusicController.Instance != null)
        {
            MusicController.Instance.PlayCountdownFullSFX();
            Debug.Log("Started full countdown SFX sequence");
        }

        // Countdown from 3 to 1
        for (int i = 3; i >= 1; i--)
        {
            if (countdownText != null)
            {
                countdownText.text = i.ToString();

                // Add some visual flair with DOTween
                countdownText.transform.localScale = Vector3.zero;
                countdownText.transform.DOScale(1f, 0.3f).SetEase(Ease.OutElastic);
                countdownText.DOColor(countdownColor, 0.1f);
            }

            // REMOVED: No longer play individual SFX for each number
            // The full audio file is already playing

            yield return new WaitForSeconds(countdownDuration);
        }

        // Show "GO!" message
        if (countdownText != null)
        {
            countdownText.text = "GO!";
            countdownText.transform.localScale = Vector3.zero;
            countdownText.transform.DOScale(1.2f, 0.3f).SetEase(Ease.OutElastic);
            countdownText.DOColor(Color.green, 0.1f);
        }

        // REMOVED: No longer play separate "GO!" SFX
        // The full audio file includes this

        yield return new WaitForSeconds(countdownDuration);

        // Hide countdown text and show gameplay UI
        if (countdownText != null)
        {
            countdownText.DOFade(0f, 0.5f).OnComplete(() => {
                countdownText.gameObject.SetActive(false);
            });
        }
        CountdownIndicatorUp.SetActive(false);
        CountdownIndicatorDown.SetActive(false);
        CountdownIndicatorLeft.SetActive(false);
        CountdownIndicatorRight.SetActive(false);

        // Show gameplay UI after countdown
        ShowGameplayUI();

        // Mark game as started
        isCountdownActive = false;
        gameStarted = true;

        if (pauseMenuController != null)
        {
            pauseMenuController.EnablePause();
        }

        Debug.Log("Countdown complete - Game started!");
    }

    // Helper method to hide gameplay UI during countdown
    private void HideGameplayUIDuringCountdown()
    {
        if (timerText != null)
            timerText.gameObject.SetActive(false);

        if (goalProgressBar != null)
            goalProgressBar.gameObject.SetActive(false);

        if (flagProgressIndicator != null)
            flagProgressIndicator.gameObject.SetActive(false);

        if (boostChargesText != null)
            boostChargesText.gameObject.SetActive(false);

        if (distanceText != null)
            distanceText.gameObject.SetActive(false);
    }

    IEnumerator SpawnObstacles()
    {
        while (!isGameOver && !hasWon)
        {
            // Skip spawning if player is halted, won, OR game hasn't started yet
            if (isHalted || hasWon || isWinAnimationPlaying || !gameStarted)
            {
                // Wait a short time before checking again
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            // Debug the current state
            Debug.Log($"SpawnObstacles: Distance={distanceTraveled:F1}, isObstacleSetActive={isObstacleSetActive}, " +
                      $"distanceSinceLastSet={distanceTraveled - lastObstacleSetDistance:F1}");

            // Check if we should spawn a handcrafted obstacle set
            if (useObstacleSets && ShouldSpawnObstacleSet())
            {
                SpawnHandcraftedObstacleSet();

                // Wait longer before checking again (obstacle sets handle their own timing)
                yield return new WaitForSeconds(1f);
                continue; // Important: Skip the rest of the loop iteration
            }

            // Only spawn random obstacles if we're not in an obstacle set phase
            if (!isObstacleSetActive)
            {
                SpawnRandomObstacle();
                Debug.Log($"Random obstacle spawned at distance {distanceTraveled:F1}m");

                // Calculate spawn interval for random obstacles
                float spawnInterval = CalculateSpawnInterval();
                Debug.Log($"Next random obstacle in {spawnInterval:F2} seconds");

                // Wait before next spawn check
                yield return new WaitForSeconds(spawnInterval);
            }
            else
            {
                // If obstacle set is active, check more frequently but don't spawn
                Debug.Log($"Obstacle set active - waiting for completion at distance {distanceTraveled:F1}m");
                yield return new WaitForSeconds(0.2f); // Check every 0.2 seconds if set has finished
            }
        }
    }
    // New method to check if we should spawn an obstacle set
    // New method to check if we should spawn an obstacle set
    private bool ShouldSpawnObstacleSet()
    {
        // Check if enough distance has passed since the last obstacle set
        float distanceSinceLastSet = distanceTraveled - lastObstacleSetDistance;

        // FIXED: Ensure we have a minimum gap for random obstacles after each set
        float effectiveInterval = obstacleSetInterval + minRandomObstacleGap;

        bool shouldSpawn = distanceSinceLastSet >= effectiveInterval &&
                           !isObstacleSetActive &&
                           handcraftedObstacleSets != null &&
                           handcraftedObstacleSets.Length > 0;

        if (distanceSinceLastSet >= effectiveInterval - 10f) // Start logging when we're close
        {
            Debug.Log($"ShouldSpawnObstacleSet: distanceSinceLastSet={distanceSinceLastSet:F1}, " +
                      $"effectiveInterval={effectiveInterval}, isActive={isObstacleSetActive}, " +
                      $"shouldSpawn={shouldSpawn}");
        }

        return shouldSpawn;
    }

    // New method to spawn a handcrafted obstacle set
    // New method to spawn a handcrafted obstacle set
    private void SpawnHandcraftedObstacleSet()
    {
        // Clear any remaining random obstacles to make room for the set
        ClearRandomObstacles();

        // Select a random obstacle set
        GameObject setToSpawn = handcraftedObstacleSets[Random.Range(0, handcraftedObstacleSets.Length)];

        // Spawn the obstacle set ahead of the player
        Vector3 spawnPosition = new Vector3(obstacleSetSpawnDistance, 0, 0);
        currentObstacleSet = Instantiate(setToSpawn, spawnPosition, Quaternion.identity, obstacleParent);

        // Mark that we're now in an obstacle set phase
        isObstacleSetActive = true;

        // DON'T update lastObstacleSetDistance here - only update it when the set completes
        // This prevents the timer from advancing prematurely

        // Count the number of obstacles in this set for debugging
        int obstacleCount = currentObstacleSet.transform.childCount;

        Debug.Log($"Spawned handcrafted obstacle set: {setToSpawn.name} with {obstacleCount} obstacles at distance {distanceTraveled:F1}m");
        Debug.Log($"Next obstacle set will be eligible at distance {lastObstacleSetDistance + obstacleSetInterval + minRandomObstacleGap:F1}m");
    }

    // New method to spawn a single random obstacle (extracted from original logic)
    private void SpawnRandomObstacle()
    {
        // Try to find a valid spawn position
        Vector3 spawnPos = Vector3.zero;
        bool validPositionFound = false;
        int maxAttempts = 10;
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

            // Instantiate obstacle
            GameObject newObstacle = Instantiate(obstaclePrefab, spawnPos, Quaternion.identity, obstacleParent);

            // Remove any ObstacleMovement component if it exists (since movement is handled by MoveObstacles)
            ObstacleMovement movement = newObstacle.GetComponent<ObstacleMovement>();
            if (movement != null)
            {
                Destroy(movement);
            }
        }
    }

    // New method to calculate spawn interval (extracted from original logic)
    private float CalculateSpawnInterval()
    {
        // More aggressive spawn intervals for higher gears
        float minSpawnInterval = 0.2f;  // Reduced from 0.35f - spawn faster at max gear
        float maxSpawnInterval = 1.0f;  // Increased from 0.85f - spawn slower at 1st gear

        // Get current effective speed (including boost)
        float currentSpeed = isBoostActive ? boostSpeed : worldSpeed;

        // Create more aggressive scaling based on gear
        float gearRatio = (float)currentGear / (gearSpeeds.Length - 1);
        float exponentialGearRatio = Mathf.Pow(gearRatio, 0.4f); // Reduced from 0.6f for more aggressive curve

        // Factor in actual speed for boost mode
        float maxSpeed = gearSpeeds[gearSpeeds.Length - 1];
        float minSpeed = gearSpeeds[0];
        float speedRatio = Mathf.Clamp01((currentSpeed - minSpeed) / (maxSpeed - minSpeed));

        // Give more weight to gear ratio for consistent progression
        float combinedRatio = (exponentialGearRatio * 0.8f) + (speedRatio * 0.2f); // Increased gear weight

        // Special boost mode handling - even more obstacles during boost
        if (isBoostActive)
        {
            minSpawnInterval = 0.15f; // Reduced from 0.25f
            combinedRatio = 1.0f;
        }

        // Calculate final spawn interval with gear-based bonus
        float spawnInterval = Mathf.Lerp(maxSpawnInterval, minSpawnInterval, combinedRatio);

        // Add gear-specific bonus reduction (more obstacles at higher gears)
        float gearBonus = currentGear * 0.05f; // 0.05s reduction per gear
        spawnInterval -= gearBonus;

        // Add slight randomization
        spawnInterval += Random.Range(-0.03f, 0.03f); // Reduced randomization range

        // Ensure minimum spawn rate
        spawnInterval = Mathf.Max(spawnInterval, 0.15f); // Reduced minimum from 0.2f

        return spawnInterval;
    }

    // New method to clear random obstacles when switching to obstacle sets
    private void ClearRandomObstacles()
    {
        // Get all current obstacles
        List<Transform> obstaclesToDestroy = new List<Transform>();

        foreach (Transform obstacle in obstacleParent)
        {
            if (obstacle == null) continue;

            // Check if this is a random obstacle (not part of a set)
            // We can identify set obstacles by checking if they're children of an obstacle set GameObject
            if (obstacle.parent == obstacleParent)
            {
                obstaclesToDestroy.Add(obstacle);
            }
        }

        // Destroy the random obstacles
        foreach (Transform obstacle in obstaclesToDestroy)
        {
            Destroy(obstacle.gameObject);
        }

        Debug.Log($"Cleared {obstaclesToDestroy.Count} random obstacles for obstacle set");
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

    // Update invulnerability system
    private void UpdateInvulnerability()
    {
        if (isInvulnerable)
        {
            invulnerabilityTimer -= Time.deltaTime;

            // Handle flashing effect during invulnerability
            SpriteRenderer playerSprite = playerCar.GetComponent<SpriteRenderer>();
            if (playerSprite != null)
            {
                // Calculate flash based on time
                float flashPhase = Mathf.Sin(invulnerabilityTimer / flashFrequency * Mathf.PI * 2);
                float alpha = Mathf.Lerp(0.3f, 1.0f, (flashPhase + 1) * 0.5f);

                Color currentColor = playerSprite.color;
                currentColor.a = alpha;
                playerSprite.color = currentColor;
            }

            // End invulnerability when timer expires
            if (invulnerabilityTimer <= 0f)
            {
                EndInvulnerability();
            }
        }
    }

    // Start invulnerability period
    private void StartInvulnerability()
    {
        isInvulnerable = true;
        invulnerabilityTimer = invulnerabilityDuration;

        Debug.Log($"Invulnerability started for {invulnerabilityDuration} seconds");
    }

    // End invulnerability period
    private void EndInvulnerability()
    {
        isInvulnerable = false;
        invulnerabilityTimer = 0f;

        // Reset sprite to normal appearance
        SpriteRenderer playerSprite = playerCar.GetComponent<SpriteRenderer>();
        if (playerSprite != null)
        {
            Color currentColor = playerSprite.color;
            currentColor.a = 1.0f; // Full opacity
            playerSprite.color = currentColor;
        }

        Debug.Log("Invulnerability ended");
    }

    // Check if player is currently invulnerable
    public bool IsInvulnerable()
    {
        return isInvulnerable;
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

        if (pauseMenuController != null)
        {
            pauseMenuController.DisablePause();
        }
        timerStopped = true; // Stop the timer immediately when goal is reached
        hasWon = true; // Set hasWon immediately so timer stops at the exact moment

        // Deactivate boost if it's currently active
        if (isBoostActive)
        {
            DeactivateBoost();
            Debug.Log("Boost deactivated due to winning");
        }

        if (MusicController.Instance != null)
        {
            MusicController.Instance.FadeToWinMusic();
            MusicController.Instance.PlayWinSFX();
        }



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

            // FIXED: Position the right road further away and adjust its scale/crop if needed
            float rightRoadX = roadSectionWidth / 2 + 6f; // Increased offset from 2f to 6f
            rightRoad.transform.position = new Vector3(Camera.main.transform.position.x + rightRoadX, 0, 0);

            // FIXED: Optionally crop the right road to prevent it from extending too far left
            SpriteRenderer rightRoadRenderer = rightRoad.GetComponent<SpriteRenderer>();
            if (rightRoadRenderer != null)
            {
                // Scale down the sprite horizontally to reduce its leftward extent
                rightRoad.transform.localScale = new Vector3(0.7f, 1f, 1f);

                // Make road initially transparent
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
        // NEW: Spawn the character BEFORE the fade transition starts
        // This way the character will be visible when the gas station fades in
        Vector3 characterSpawnPosition = new Vector3(
            Camera.main.transform.position.x + finalParkPosition + characterOffset.x,
            minYPosition + (laneWidth * 0.25f) + characterOffset.y,
            characterOffset.z
        );
        SpawnCharacterForGasStation(characterSpawnPosition);
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

            // NEW: Fade in the character alongside everything else
            if (spawnedCharacter != null)
            {
                SpriteRenderer characterRenderer = spawnedCharacter.GetComponent<SpriteRenderer>();
                if (characterRenderer != null)
                {
                    Color fadeColor = characterRenderer.color;
                    fadeColor.a = t;
                    characterRenderer.color = fadeColor;
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

        // NEW: Ensure character is fully visible
        if (spawnedCharacter != null)
        {
            SpriteRenderer characterRenderer = spawnedCharacter.GetComponent<SpriteRenderer>();
            if (characterRenderer != null)
            {
                Color fadeColor = characterRenderer.color;
                fadeColor.a = 1f;
                characterRenderer.color = fadeColor;
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

        if (currentTimeText != null)
        {
            // Format time with 2 decimal places
            currentTimeText.text = gameTimer.ToString("F2");
            currentTimeText.gameObject.SetActive(true); // Only activate after animation

            // Check if this is a new best time
            if (gameTimer < bestTime)
            {
                bestTime = gameTimer;
                SaveBestTime(); // Save the new best time
                Debug.Log($"New best time achieved: {bestTime:F2}");
            }
        }

        if (bestTimeText != null)
        {
            bestTimeText.text = bestTime.ToString("F2");
            bestTimeText.gameObject.SetActive(true); // Only activate after animation
        }

        if (bestTimeText != null)
        {
            bestTimeText.text = bestTime.ToString("F2");
            bestTimeText.gameObject.SetActive(true); // Only activate after animation
        }

        // MODIFIED: Show the leaderboard only after the win animation completes
        if (leaderboardManager != null)
        {
            leaderboardManager.ShowGasStationLeaderboard();
        }

        // MODIFIED: Display win panel only at the end of the animation
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
        // Apply gear-based multiplier based on which system is active
        float finalScrollSpeed = scrollSpeed;

        if (useInverseSpeedSystem && currentGear < backgroundSpeedMultipliers.Length)
        {
            // Apply the same intensity scaling as in ScrollBackground()
            float gearEffect = backgroundSpeedMultipliers[currentGear] - 1f;
            float scaledGearEffect = gearEffect * backgroundIntensity;
            float finalMultiplier = 1f + scaledGearEffect;
            finalScrollSpeed *= finalMultiplier;
        }
        else if (!useInverseSpeedSystem && currentGear < normalModeBackgroundMultipliers.Length)
        {
            // Normal system: apply gear-based multipliers
            finalScrollSpeed *= normalModeBackgroundMultipliers[currentGear];
        }

        if (useRepeatingBackground)
        {
            // For sprite-based repeating backgrounds
            if (backgrounds.Count > 0)
            {
                // Move all background pieces
                foreach (SpriteRenderer bg in backgrounds)
                {
                    if (bg == null) continue;

                    bg.transform.position += Vector3.left * finalScrollSpeed * Time.deltaTime;

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
                float newOffset = currentOffset.x + finalScrollSpeed * Time.deltaTime;

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

        // Get the ObstacleCollision component once for both cases
        ObstacleCollision obstacleCollision = hitObstacle.GetComponent<ObstacleCollision>();

        // If boost is active, destroy the obstacle without penalty
        if (isBoostActive)
        {
            Debug.Log("Obstacle destroyed by boost!");

            if (MusicController.Instance != null)
            {
                MusicController.Instance.PlayBoostCollisionSFX();
            }

            // Trigger explosion animation before destroying obstacle
            if (obstacleCollision != null)
            {
                obstacleCollision.PlayExplosionAnimation(hitObstacle.transform.position);
            }

            // Destroy the obstacle after triggering explosion
            if (hitObstacle != null)
            {
                hitObstacle.transform.DOPunchScale(new Vector3(1.5f, 1.5f, 0), 0.2f, 10, 1f)
                    .OnComplete(() => {
                        if (hitObstacle != null)
                            Destroy(hitObstacle);
                    });
            }
            return;
        }

        if (MusicController.Instance != null)
        {
            MusicController.Instance.PlayObstacleHitSFX();
        }

        if (isHalted || isInvulnerable) return;

        // Trigger explosion animation before destroying obstacle
        if (obstacleCollision != null)
        {
            obstacleCollision.PlayExplosionAnimation(hitObstacle.transform.position);
        }

        // Destroy the obstacle after a small delay
        if (hitObstacle != null)
        {
            StartCoroutine(DestroyObstacleAfterExplosion(hitObstacle, 0.1f));
        }

        StartInvulnerability();
        currentGear = 0;
        UpdateGearText();
        StartCoroutine(HaltPlayer());
    }

    private IEnumerator DestroyObstacleAfterExplosion(GameObject obstacle, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obstacle != null)
        {
            Destroy(obstacle);
        }
    }
    // Call this from collision detection when collecting cigarette pack
    public void OnPlayerCollectBoost(GameObject collectible)
    {
        Debug.Log("Cigarette pack collected!");
        MusicController.Instance.PlayBoostCollectSFX();
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

    // Disable player collision during spin animation
    DisablePlayerCollisionTemporarily();

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

    // Re-enable player collision after spin animation
    EnablePlayerCollision();

    // Resume movement
    isHalted = false;

    Debug.Log("Player collision re-enabled after spin animation");
}

// New method to temporarily disable player collision during spin
private void DisablePlayerCollisionTemporarily()
{
    // Disable all colliders attached to the player car
    Collider2D[] colliders = playerCar.GetComponentsInChildren<Collider2D>();
    foreach (Collider2D collider in colliders)
    {
        collider.enabled = false;
    }

    Debug.Log("Player collision disabled during spin animation");
}

// New method to re-enable player collision after spin
private void EnablePlayerCollision()
{
    // Re-enable all colliders attached to the player car
    Collider2D[] colliders = playerCar.GetComponentsInChildren<Collider2D>();
    foreach (Collider2D collider in colliders)
    {
        collider.enabled = true;
    }

    // Make sure the collision script is enabled
    PlayerCollision playerCollision = playerCar.GetComponent<PlayerCollision>();
    if (playerCollision != null)
    {
        playerCollision.enabled = true;
    }

    Debug.Log("Player collision re-enabled");
}

    // Add these public methods to your GameController class:

    // Get current boost charges (for UI display)
    public int GetBoostCharges()
    {
        return currentBoostCharges;
    }

    // Get max boost charges (for UI display)
    public int GetMaxBoostCharges()
    {
        return maxBoostCharges;
    }

    // Get remaining boost time (for UI meter)
    public float GetBoostTimeRemaining()
    {
        return isBoostActive ? boostTimer : 0f;
    }

    // Get total boost duration (for UI meter)
    public float GetBoostDuration()
    {
        return boostDuration;
    }
    public void ResetObstacleSystem()
    {
        isObstacleSetActive = false;
        currentObstacleSet = null;
        lastObstacleSetDistance = 0f;

        // Clear all existing obstacles
        foreach (Transform obstacle in obstacleParent)
        {
            if (obstacle != null)
            {
                Destroy(obstacle.gameObject);
            }
        }
    }

    // UI button functions
    // UI button functions
    public void RestartGame()
    {
        // Reset obstacle system
        ResetObstacleSystem();

        // NEW: Cleanup character if it exists
        CleanupCharacter();

        // Show gameplay UI elements on restart
        ShowGameplayUI();

        // Ensure leaderboard is completely hidden when restarting
        if (leaderboardManager != null)
        {
            leaderboardManager.HideGasStationLeaderboard();
        }

        // Reset traffic light spawner
        if (trafficLightOverlay != null)
        {
            trafficLightOverlay.ResetSpawner();
        }

        // NEW: Reset pause system
        if (pauseMenuController != null)
        {
            pauseMenuController.DisablePause(); // Will be re-enabled after countdown
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public int GetCurrentGear()
    {
        return currentGear;
    }

    private void SaveBestTime()
    {
        PlayerPrefs.SetFloat("BestTime", bestTime);
        PlayerPrefs.Save(); // Force save to disk
        Debug.Log($"Best time saved: {bestTime:F2}");
    }

    // Load best time from persistent storage
    private void LoadBestTime()
    {
        // Load best time, defaulting to float.MaxValue if no saved time exists
        bestTime = PlayerPrefs.GetFloat("BestTime", float.MaxValue);
        Debug.Log($"Best time loaded: {bestTime:F2}");
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