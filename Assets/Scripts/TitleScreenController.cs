using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class TitleScreenController : MonoBehaviour
{
    [Header("Title Screen UI")]
    public Button startGameButton;
    public Button leaderboardButton;
    public Button exitGameButton;
    public GameObject leaderboardPanel;
    public Button closeLeaderboardButton;

    [Header("Title Text")]
    public TMP_Text titleText;
    public string gameTitle = "Speed Racer";

    [Header("Leaderboard Integration")]
    public LeaderboardCreatorDemo.LeaderboardManager leaderboardManager;

    [Header("Scene Management")]
    public string gameSceneName = "GameScene"; // Name of your main game scene

    [Header("Animation Settings")]
    public bool enableButtonAnimations = true;
    public float buttonHoverScale = 1.1f;
    public float animationDuration = 0.2f;

    private bool isLeaderboardOpen = false;

    void Start()
    {
        // Initialize the title screen
        InitializeTitleScreen();

        // Setup button listeners
        SetupButtonListeners();

        // Setup button animations if enabled
        if (enableButtonAnimations)
        {
            SetupButtonAnimations();
        }

        // Hide leaderboard panel initially
        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(false);
        }

        // Set title text
        if (titleText != null && !string.IsNullOrEmpty(gameTitle))
        {
            titleText.text = gameTitle;
        }

        // Ensure leaderboard manager is properly initialized for title screen use
        if (leaderboardManager != null)
        {
            // Make sure the leaderboard components are hidden initially
            leaderboardManager.HideGasStationLeaderboard();
        }

        // Start title screen music
        if (MusicController.Instance != null)
        {
            MusicController.Instance.PlayTitleScreenMusic();
        }
    }

    private void InitializeTitleScreen()
    {
        // Ensure cursor is visible and unlocked
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Reset time scale in case it was modified
        Time.timeScale = 1f;

        Debug.Log("Title Screen initialized");
    }

    private void SetupButtonListeners()
    {
        // Start Game button
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(StartGame);
        }
        else
        {
            Debug.LogWarning("Start Game button not assigned in Title Screen Controller!");
        }

        // Leaderboard button
        if (leaderboardButton != null)
        {
            leaderboardButton.onClick.AddListener(ToggleLeaderboard);
        }
        else
        {
            Debug.LogWarning("Leaderboard button not assigned in Title Screen Controller!");
        }

        // Exit Game button
        if (exitGameButton != null)
        {
            exitGameButton.onClick.AddListener(ExitGame);
        }
        else
        {
            Debug.LogWarning("Exit Game button not assigned in Title Screen Controller!");
        }

        // Close Leaderboard button
        if (closeLeaderboardButton != null)
        {
            closeLeaderboardButton.onClick.AddListener(CloseLeaderboard);
        }
    }

    private void SetupButtonAnimations()
    {
        // Add hover effects to buttons using Unity's built-in event system
        SetupButtonHoverEffect(startGameButton);
        SetupButtonHoverEffect(leaderboardButton);
        SetupButtonHoverEffect(exitGameButton);
        SetupButtonHoverEffect(closeLeaderboardButton);
    }

    private void SetupButtonHoverEffect(Button button)
    {
        if (button == null) return;

        // Add event triggers for hover effects
        var eventTrigger = button.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = button.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        }

        // Pointer Enter (hover)
        var pointerEnter = new UnityEngine.EventSystems.EventTrigger.Entry
        {
            eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter
        };
        pointerEnter.callback.AddListener((data) => { OnButtonHover(button.transform, true); });
        eventTrigger.triggers.Add(pointerEnter);

        // Pointer Exit (stop hover)
        var pointerExit = new UnityEngine.EventSystems.EventTrigger.Entry
        {
            eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit
        };
        pointerExit.callback.AddListener((data) => { OnButtonHover(button.transform, false); });
        eventTrigger.triggers.Add(pointerExit);
    }

    private void OnButtonHover(Transform buttonTransform, bool isHovering)
    {
        if (buttonTransform == null) return;

        Vector3 targetScale = isHovering ? Vector3.one * buttonHoverScale : Vector3.one;

        // Simple scale animation - you can replace this with DOTween if you have it
        StartCoroutine(AnimateButtonScale(buttonTransform, targetScale));
    }

    private System.Collections.IEnumerator AnimateButtonScale(Transform target, Vector3 targetScale)
    {
        Vector3 startScale = target.localScale;
        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationDuration;

            // Smooth interpolation
            progress = Mathf.SmoothStep(0f, 1f, progress);

            target.localScale = Vector3.Lerp(startScale, targetScale, progress);
            yield return null;
        }

        target.localScale = targetScale;
    }

    public void StartGame()
    {
        Debug.Log("Starting game...");

        // Play button click sound
        if (MusicController.Instance != null)
        {
            MusicController.Instance.PlayButtonClickSFX();
        }

        // Add a small delay for button press feedback
        StartCoroutine(StartGameWithDelay());
    }

    private System.Collections.IEnumerator StartGameWithDelay()
    {
        // Optional: Add button press animation here
        if (startGameButton != null && enableButtonAnimations)
        {
            startGameButton.transform.localScale = Vector3.one * 0.95f;
            yield return new WaitForSeconds(0.1f);
            startGameButton.transform.localScale = Vector3.one;
        }

        // Load the game scene
        if (!string.IsNullOrEmpty(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            // Fallback: load the next scene in build index
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
    }

    public void ToggleLeaderboard()
    {
        Debug.Log("Toggling leaderboard...");

        // Play button click sound
        if (MusicController.Instance != null)
        {
            MusicController.Instance.PlayButtonClickSFX();
        }

        if (isLeaderboardOpen)
        {
            CloseLeaderboard();
        }
        else
        {
            OpenLeaderboard();
        }
    }

    public void OpenLeaderboard()
    {
        Debug.Log("Opening leaderboard...");

        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(true);
            isLeaderboardOpen = true;

            // FIXED: Use ShowGasStationLeaderboard() which properly sets visibility and loads entries
            if (leaderboardManager != null)
            {
                leaderboardManager.ShowGasStationLeaderboard(); // This sets visibility flag AND loads entries
            }
        }
        else
        {
            Debug.LogWarning("Leaderboard panel not assigned!");
        }
    }

    public void CloseLeaderboard()
    {
        Debug.Log("Closing leaderboard...");

        // Play button click sound
        if (MusicController.Instance != null)
        {
            MusicController.Instance.PlayButtonClickSFX();
        }

        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(false);
            isLeaderboardOpen = false;

            // ADDED: Properly hide the leaderboard components when closing
            if (leaderboardManager != null)
            {
                leaderboardManager.HideGasStationLeaderboard();
            }
        }
    }

    public void ExitGame()
    {
        Debug.Log("Exiting game...");

        // Play button click sound
        if (MusicController.Instance != null)
        {
            MusicController.Instance.PlayButtonClickSFX();
        }

        // Add confirmation dialog in a real game
        StartCoroutine(ExitGameWithDelay());
    }

    private System.Collections.IEnumerator ExitGameWithDelay()
    {
        // Optional: Add button press animation
        if (exitGameButton != null && enableButtonAnimations)
        {
            exitGameButton.transform.localScale = Vector3.one * 0.95f;
            yield return new WaitForSeconds(0.1f);
            exitGameButton.transform.localScale = Vector3.one;
        }

        // Exit the application
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Handle back button or escape key
    void Update()
    {
        // Close leaderboard with Escape key
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isLeaderboardOpen)
            {
                CloseLeaderboard();
            }
        }
    }

    // Public methods that can be called by UI buttons directly
    public void OnStartGameButtonPressed() => StartGame();
    public void OnLeaderboardButtonPressed() => ToggleLeaderboard();
    public void OnExitGameButtonPressed() => ExitGame();
    public void OnCloseLeaderboardButtonPressed() => CloseLeaderboard();
}