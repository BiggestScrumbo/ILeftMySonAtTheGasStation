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
    public float[] gearSpeeds = { 3f, 6f, 10f, 15f, 20f }; // Speed for each gear
    public float startPosition = -10f; // Starting X position
    public float endPosition = 1000f; // Gas station X position

    [Header("Gameplay")]
    public float obstacleSpawnRate = 1.5f;
    public GameObject[] obstacles;
    public Transform obstacleParent;
    public float minObstacleSpacing = 10f;

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

        // Initialize player position
        playerCar.transform.position = new Vector3(startPosition, 0, 0);

        // Setup UI
        UpdateGearText();

        // Hide end game panels
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);

        // Start obstacle spawning
        StartCoroutine(SpawnObstacles());
    }

    // Update is called once per frame
    void Update()
    {
        if (isGameOver || hasWon) return;

        // Handle freeform vertical movement (up/down)
        float verticalInput = Input.GetAxis("Vertical"); // Uses Up/Down arrows or W/S keys
        float newYPosition = playerCar.transform.position.y + (verticalInput * verticalMoveSpeed * Time.deltaTime);

        // Clamp position to stay within road boundaries
        newYPosition = Mathf.Clamp(newYPosition, minYPosition, maxYPosition);

        // Apply vertical movement
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

        // Move forward based on current gear
        playerCar.transform.position += Vector3.right * gearSpeeds[currentGear] * Time.deltaTime;

        // Update game timer
        gameTimer += Time.deltaTime;
        UpdateTimerText();

        // Update distance to goal
        UpdateDistanceText();

        // Check if player reached the goal (gas station)
        if (playerCar.transform.position.x >= endPosition)
        {
            Win();
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
            timerText.text = "Time: " + gameTimer.ToString("F2") + "s";
    }

    private void UpdateDistanceText()
    {
        if (distanceText != null)
        {
            float remainingDistance = endPosition - playerCar.transform.position.x;
            distanceText.text = "Distance to Goal: " + Mathf.Max(0, remainingDistance).ToString("F1") + "m";
        }
    }

    IEnumerator SpawnObstacles()
    {
        float spawnX = playerCar.transform.position.x + 30f; // Start spawning ahead of player

        while (!isGameOver && !hasWon)
        {
            // Random Y position within road boundaries
            float obstacleY = Random.Range(minYPosition, maxYPosition);

            // Random obstacle selection
            if (obstacles != null && obstacles.Length > 0)
            {
                GameObject obstaclePrefab = obstacles[Random.Range(0, obstacles.Length)];
                Vector3 spawnPos = new Vector3(spawnX, obstacleY, 0f);

                // Instantiate obstacle
                Instantiate(obstaclePrefab, spawnPos, Quaternion.identity, obstacleParent);
            }

            // Move spawn position forward
            spawnX += Random.Range(minObstacleSpacing, minObstacleSpacing * 2);

            // Wait before next spawn
            yield return new WaitForSeconds(obstacleSpawnRate);
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
    public void OnPlayerHitObstacle()
    {
        GameOver();
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
