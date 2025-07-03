using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;

public class PauseMenuController : MonoBehaviour
{
    [Header("Pause Menu UI")]
    public GameObject pauseMenuPanel;
    public Button resumeButton;
    public Button restartButton;
    public Button mainMenuButton;
    public Button quitButton;

    [Header("Volume Controls")]
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public TMP_Text musicVolumeText;
    public TMP_Text sfxVolumeText;

    [Header("Settings")]
    public KeyCode pauseKey = KeyCode.Escape;
    public bool canPauseOnStart = false; // Prevent pausing during countdown
    public float fadeAnimationDuration = 0.3f;

    [Header("Visual Effects")]
    public CanvasGroup pauseMenuCanvasGroup;
    public bool enableBackgroundBlur = true;
    public Color backgroundOverlayColor = new Color(0, 0, 0, 0.5f);
    public Image backgroundOverlay;

    // Private variables
    private bool isPaused = false;
    private bool canPause = false;
    private GameController gameController;

    // Static reference for easy access
    public static PauseMenuController Instance { get; private set; }

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Find game controller
        gameController = FindObjectOfType<GameController>();
        if (gameController == null)
        {
            Debug.LogWarning("PauseMenuController: GameController not found!");
        }
    }

    void Start()
    {
        // Initialize pause menu
        InitializePauseMenu();

        // Setup button listeners
        SetupButtonListeners();

        // Setup volume sliders
        SetupVolumeSliders();

        // Hide pause menu initially
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }

        // Set initial pause state
        canPause = canPauseOnStart;
    }

    void Update()
    {
        // Handle pause input
        if (Input.GetKeyDown(pauseKey) && canPause)
        {
            if (gameController != null)
            {
                // Don't allow pausing during win animation or game over
                if (gameController.IsGameOver() || gameController.HasWon())
                    return;
            }

            TogglePause();
        }
    }

    private void InitializePauseMenu()
    {
        // Setup canvas group for smooth fading
        if (pauseMenuCanvasGroup == null && pauseMenuPanel != null)
        {
            pauseMenuCanvasGroup = pauseMenuPanel.GetComponent<CanvasGroup>();
            if (pauseMenuCanvasGroup == null)
            {
                pauseMenuCanvasGroup = pauseMenuPanel.AddComponent<CanvasGroup>();
            }
        }

        // Setup background overlay
        if (backgroundOverlay != null)
        {
            backgroundOverlay.color = backgroundOverlayColor;
        }

        Debug.Log("Pause Menu initialized");
    }

    private void SetupButtonListeners()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(ResumeGame);
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }
    }

    private void SetupVolumeSliders()
    {
        // Load saved volume settings
        LoadVolumeSettings();

        // Setup music volume slider
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.minValue = 0f;
            musicVolumeSlider.maxValue = 1f;
            musicVolumeSlider.value = GetMusicVolume();
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        // Setup SFX volume slider
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.minValue = 0f;
            sfxVolumeSlider.maxValue = 1f;
            sfxVolumeSlider.value = GetSFXVolume();
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }

        // Update volume text displays
        UpdateVolumeTexts();
    }

    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        if (isPaused) return;

        isPaused = true;

        // Pause the game
        Time.timeScale = 0f;

        // Pause audio
        if (MusicController.Instance != null)
        {
            MusicController.Instance.PauseMusic();
        }

        // Show pause menu with animation
        ShowPauseMenu();

        // Play pause sound effect (using unscaled time)
        if (MusicController.Instance != null)
        {
            MusicController.Instance.PlayButtonClickSFX();
        }

        Debug.Log("Game paused");
    }

    public void ResumeGame()
    {
        if (!isPaused) return;

        // Play resume sound effect first
        if (MusicController.Instance != null)
        {
            MusicController.Instance.PlayButtonClickSFX();
        }

        // Hide pause menu with animation
        HidePauseMenu(() => {
            isPaused = false;

            // Resume the game
            Time.timeScale = 1f;

            // Resume audio
            if (MusicController.Instance != null)
            {
                MusicController.Instance.ResumeMusic();
            }

            Debug.Log("Game resumed");
        });
    }

    public void RestartGame()
    {
        // Play button click sound
        if (MusicController.Instance != null)
        {
            MusicController.Instance.PlayButtonClickSFX();
        }

        // Save current volume settings
        SaveVolumeSettings();

        // Reset time scale
        Time.timeScale = 1f;

        // Restart the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToMainMenu()
    {
        // Play button click sound
        if (MusicController.Instance != null)
        {
            MusicController.Instance.PlayButtonClickSFX();
        }

        // Save current volume settings
        SaveVolumeSettings();

        // Reset time scale
        Time.timeScale = 1f;

        // Load main menu scene (assuming it's the first scene)
        SceneManager.LoadScene(0);
    }

    public void QuitGame()
    {
        // Play button click sound
        if (MusicController.Instance != null)
        {
            MusicController.Instance.PlayButtonClickSFX();
        }

        // Save current volume settings
        SaveVolumeSettings();

        // Quit the application
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ShowPauseMenu()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);

            // Animate fade in
            if (pauseMenuCanvasGroup != null)
            {
                pauseMenuCanvasGroup.alpha = 0f;
                pauseMenuCanvasGroup.DOFade(1f, fadeAnimationDuration)
                    .SetUpdate(true); // Use unscaled time
            }
        }
    }

    private void HidePauseMenu(System.Action onComplete = null)
    {
        if (pauseMenuCanvasGroup != null)
        {
            pauseMenuCanvasGroup.DOFade(0f, fadeAnimationDuration)
                .SetUpdate(true) // Use unscaled time
                .OnComplete(() => {
                    if (pauseMenuPanel != null)
                    {
                        pauseMenuPanel.SetActive(false);
                    }
                    onComplete?.Invoke();
                });
        }
        else
        {
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(false);
            }
            onComplete?.Invoke();
        }
    }

    #region Volume Control

    private void OnMusicVolumeChanged(float value)
    {
        if (MusicController.Instance != null)
        {
            MusicController.Instance.SetMusicVolume(value);
        }

        UpdateVolumeTexts();

        // Save setting immediately
        SaveVolumeSettings();
    }

    private void OnSFXVolumeChanged(float value)
    {
        if (MusicController.Instance != null)
        {
            MusicController.Instance.SetSFXVolume(value);

            // Play a quick SFX to test the volume
            MusicController.Instance.PlayButtonClickSFX();
        }

        UpdateVolumeTexts();

        // Save setting immediately
        SaveVolumeSettings();
    }

    private void UpdateVolumeTexts()
    {
        if (musicVolumeText != null)
        {
            musicVolumeText.text = $"Music: {Mathf.RoundToInt(GetMusicVolume() * 100)}%";
        }

        if (sfxVolumeText != null)
        {
            sfxVolumeText.text = $"SFX: {Mathf.RoundToInt(GetSFXVolume() * 100)}%";
        }
    }

    private float GetMusicVolume()
    {
        if (MusicController.Instance != null)
        {
            return MusicController.Instance.GetMusicVolume();
        }
        return PlayerPrefs.GetFloat("MusicVolume", 0.7f);
    }

    private float GetSFXVolume()
    {
        if (MusicController.Instance != null)
        {
            return MusicController.Instance.GetSFXVolume();
        }
        return PlayerPrefs.GetFloat("SFXVolume", 0.8f);
    }

    private void SaveVolumeSettings()
    {
        PlayerPrefs.SetFloat("MusicVolume", GetMusicVolume());
        PlayerPrefs.SetFloat("SFXVolume", GetSFXVolume());
        PlayerPrefs.Save();

        Debug.Log($"Volume settings saved - Music: {GetMusicVolume():F2}, SFX: {GetSFXVolume():F2}");
    }

    private void LoadVolumeSettings()
    {
        float musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
        float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.8f);

        if (MusicController.Instance != null)
        {
            MusicController.Instance.SetMusicVolume(musicVolume);
            MusicController.Instance.SetSFXVolume(sfxVolume);
        }

        Debug.Log($"Volume settings loaded - Music: {musicVolume:F2}, SFX: {sfxVolume:F2}");
    }

    #endregion

    #region Public Methods

    public bool IsPaused() => isPaused;

    public void SetCanPause(bool canPause)
    {
        this.canPause = canPause;
    }

    public void EnablePause()
    {
        canPause = true;
    }

    public void DisablePause()
    {
        canPause = false;
    }

    #endregion

    void OnDestroy()
    {
        // Save volume settings when destroyed
        SaveVolumeSettings();

        // Clean up DOTween animations
        DOTween.Kill(pauseMenuCanvasGroup);
    }
}