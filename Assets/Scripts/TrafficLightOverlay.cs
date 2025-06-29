using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrafficLightOverlay : MonoBehaviour
{
    [Header("Traffic Light Settings")]
    public GameObject trafficLightPrefab; // Your traffic light graphic prefab
    public float spawnInterval = 100f; // Distance between traffic lights (100m)
    public float spawnHeight = 3f; // Height above road center
    public float spawnDistance = 20f; // How far to the right to spawn (same as obstacles)
    public float despawnDistance = 10f; // How far to the left before destroying
    public string sortingLayerName = "UI"; // Sorting layer to appear on top
    public int orderInLayer = 10; // Order within the sorting layer

    private GameController gameController;
    private float lastSpawnDistance = 0f;
    private List<GameObject> activeTrafficLights = new List<GameObject>();

    void Start()
    {
        gameController = FindObjectOfType<GameController>();
        
        // Debug logging to check if GameController is found
        if (gameController == null)
        {
            Debug.LogError("TrafficLightOverlay: GameController not found!");
        }
        else
        {
            Debug.Log("TrafficLightOverlay: GameController found successfully!");
        }
        
        // Check if prefab is assigned
        if (trafficLightPrefab == null)
        {
            Debug.LogError("TrafficLightOverlay: Traffic light prefab is not assigned!");
        }
        else
        {
            Debug.Log("TrafficLightOverlay: Traffic light prefab assigned successfully!");
        }
    }

    void Update()
    {
        if (gameController == null || gameController.IsGameOver() || gameController.HasWon())
            return;

        // Check if we should spawn a new traffic light
        float currentDistance = gameController.DistanceTraveled;
        
        // Debug logging every few seconds to track distance
        if (Time.frameCount % 180 == 0) // Every 3 seconds at 60 FPS
        {
            Debug.Log($"TrafficLightOverlay: Current distance = {currentDistance:F1}m, Last spawn = {lastSpawnDistance:F1}m, Next spawn at = {lastSpawnDistance + spawnInterval:F1}m");
        }

        if (currentDistance >= lastSpawnDistance + spawnInterval)
        {
            Debug.Log($"TrafficLightOverlay: Attempting to spawn traffic light at distance {currentDistance:F1}m");
            SpawnTrafficLight();
            lastSpawnDistance = currentDistance;
        }

        // Move existing traffic lights
        MoveTrafficLights();

        // Clean up traffic lights that have moved off-screen
        CleanupTrafficLights();
    }

    void SpawnTrafficLight()
    {
        if (trafficLightPrefab == null)
        {
            Debug.LogError("TrafficLightOverlay: Cannot spawn traffic light - prefab is null!");
            return;
        }

        // Spawn on the right side, above the road
        Vector3 spawnPos = new Vector3(spawnDistance, spawnHeight, 0f);
        Debug.Log($"TrafficLightOverlay: Spawning traffic light at position {spawnPos}");
        
        GameObject trafficLight = Instantiate(trafficLightPrefab, spawnPos, Quaternion.identity);

        // Set sorting layer to appear on top
        SpriteRenderer renderer = trafficLight.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sortingLayerName = sortingLayerName;
            renderer.sortingOrder = orderInLayer;
            Debug.Log($"TrafficLightOverlay: Set sorting layer to {sortingLayerName} with order {orderInLayer}");
        }
        else
        {
            Debug.LogWarning("TrafficLightOverlay: Traffic light prefab has no SpriteRenderer component!");
        }

        // Add to active list
        activeTrafficLights.Add(trafficLight);

        Debug.Log($"Traffic light spawned successfully at distance: {gameController.DistanceTraveled:F0}m. Active count: {activeTrafficLights.Count}");
    }

    void MoveTrafficLights()
    {
        if (gameController == null) return;

        // Get current world speed (including boost)
        float currentSpeed = gameController.IsBoostActive() ? gameController.boostSpeed : gameController.worldSpeed;

        // Move all active traffic lights left at world speed
        foreach (GameObject trafficLight in activeTrafficLights)
        {
            if (trafficLight != null)
            {
                trafficLight.transform.position += Vector3.left * currentSpeed * Time.deltaTime;
            }
        }
    }

    void CleanupTrafficLights()
    {
        // Remove traffic lights that have moved too far left
        for (int i = activeTrafficLights.Count - 1; i >= 0; i--)
        {
            if (activeTrafficLights[i] == null)
            {
                activeTrafficLights.RemoveAt(i);
                continue;
            }

            if (activeTrafficLights[i].transform.position.x < -despawnDistance)
            {
                Debug.Log($"TrafficLightOverlay: Destroying traffic light at position {activeTrafficLights[i].transform.position.x}");
                Destroy(activeTrafficLights[i]);
                activeTrafficLights.RemoveAt(i);
            }
        }
    }

    // Reset when game restarts
    public void ResetSpawner()
    {
        Debug.Log("TrafficLightOverlay: Resetting spawner");
        lastSpawnDistance = 0f;

        // Clear all existing traffic lights
        foreach (GameObject trafficLight in activeTrafficLights)
        {
            if (trafficLight != null)
            {
                Destroy(trafficLight);
            }
        }
        activeTrafficLights.Clear();
    }
}