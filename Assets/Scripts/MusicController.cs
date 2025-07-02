using System.Collections;
using UnityEngine;

public class MusicController : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Background Music")]
    [SerializeField] private AudioClip titleScreenMusic;
    [SerializeField] private AudioClip gameplayMusic;
    [SerializeField] private AudioClip winScreenMusic;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip buttonClickSFX;
    [SerializeField] private AudioClip gearChangeSFX;
    [SerializeField] private AudioClip obstacleHitSFX;
    [SerializeField] private AudioClip boostCollectSFX;
    [SerializeField] private AudioClip boostActiveSFX;
    [SerializeField] private AudioClip gameOverSFX;
    [SerializeField] private AudioClip winSFX;

    [Header("Individual SFX Volume Controls")]
    [SerializeField][Range(0f, 2f)] private float obstacleHitVolume = 2f; // Adjustable in Inspector
    [SerializeField][Range(0f, 1f)] private float gearChangeVolume = 1.0f;
    [SerializeField][Range(0f, 1f)] private float boostCollectVolume = 1.0f;
    [SerializeField][Range(0f, 1f)] private float boostActiveVolume = 1.0f;
    // Add more individual volumes as needed

    [Header("Gear Change Pitch Settings")]
    [SerializeField] private float[] gearPitches = { 0.8f, 0.9f, 1.0f, 1.1f, 1.2f }; // Pitch for each gear (1-5)
    [SerializeField] private bool useGearPitchVariation = true; // Toggle for pitch variation

    [Header("Settings")]
    [SerializeField] private float musicVolume = 0.7f;
    [SerializeField] private float sfxVolume = 0.8f;
    [SerializeField] private float fadeTime = 2f;

    // Singleton pattern for easy access
    public static MusicController Instance { get; private set; }

    // Current music state tracking
    private AudioClip currentMusic;
    private bool isFading = false;

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeAudioSources()
    {
        // Create audio sources if they don't exist
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = musicVolume;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.volume = sfxVolume;
        }
    }

    #region Music Control

    public void PlayTitleScreenMusic()
    {
        if (titleScreenMusic != null)
        {
            PlayMusic(titleScreenMusic);
        }
    }

    public void PlayGameplayMusic()
    {
        if (gameplayMusic != null)
        {
            PlayMusic(gameplayMusic);
        }
    }

    public void PlayWinScreenMusic()
    {
        if (winScreenMusic != null)
        {
            PlayMusic(winScreenMusic);
        }
    }

    public void FadeToWinMusic()
    {
        if (winScreenMusic != null && !isFading)
        {
            StartCoroutine(FadeToNewMusic(winScreenMusic));
        }
    }

    private void PlayMusic(AudioClip newClip)
    {
        if (newClip == currentMusic && musicSource.isPlaying)
            return;

        musicSource.Stop();
        musicSource.clip = newClip;
        musicSource.Play();
        currentMusic = newClip;
    }

    private IEnumerator FadeToNewMusic(AudioClip newClip)
    {
        isFading = true;

        // Fade out current music
        float startVolume = musicSource.volume;
        float elapsedTime = 0f;

        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, progress);
            yield return null;
        }

        // Switch to new music
        musicSource.Stop();
        musicSource.clip = newClip;
        musicSource.Play();
        currentMusic = newClip;

        // Fade in new music
        elapsedTime = 0f;
        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeTime;
            musicSource.volume = Mathf.Lerp(0f, musicVolume, progress);
            yield return null;
        }

        musicSource.volume = musicVolume;
        isFading = false;
    }

    public void StopMusic()
    {
        musicSource.Stop();
        currentMusic = null;
    }

    public void PauseMusic()
    {
        musicSource.Pause();
    }

    public void ResumeMusic()
    {
        musicSource.UnPause();
    }

    #endregion

    #region Sound Effects

    public void PlayButtonClickSFX()
    {
        PlaySFX(buttonClickSFX);
    }

    public void PlayGearChangeSFX()
    {
        PlaySFX(gearChangeSFX);
    }

    // NEW: Gear change with specific pitch based on gear number
    public void PlayGearChangeSFX(int gearIndex)
    {
        if (gearChangeSFX != null && sfxSource != null)
        {
            // Calculate pitch based on gear
            float pitch = 1.0f; // Default pitch

            if (useGearPitchVariation && gearIndex >= 0 && gearIndex < gearPitches.Length)
            {
                pitch = gearPitches[gearIndex];
            }

            // Play with specific pitch
            PlaySFXWithPitch(gearChangeSFX, pitch);
        }
    }

    public void PlayObstacleHitSFX()
    {
        PlaySFX(obstacleHitSFX);
    }

    public void PlayBoostCollectSFX()
    {
        PlaySFX(boostCollectSFX);
    }

    public void PlayBoostActiveSFX()
    {
        PlaySFX(boostActiveSFX);
    }

    public void PlayGameOverSFX()
    {
        PlaySFX(gameOverSFX);
    }

    public void PlayWinSFX()
    {
        PlaySFX(winSFX);
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            // Reset pitch to normal before playing
            sfxSource.pitch = 1.0f;
            sfxSource.PlayOneShot(clip);
        }
    }

    // NEW: Play SFX with specific pitch
    private void PlaySFXWithPitch(AudioClip clip, float pitch)
    {
        if (clip != null && sfxSource != null)
        {
            // Set the pitch
            sfxSource.pitch = pitch;
            sfxSource.PlayOneShot(clip);

            // Reset pitch back to normal after playing for other SFX
            StartCoroutine(ResetPitchAfterClip(clip.length));
        }
    }

    // Coroutine to reset pitch after the clip finishes
    private IEnumerator ResetPitchAfterClip(float clipLength)
    {
        yield return new WaitForSeconds(clipLength);
        if (sfxSource != null)
        {
            sfxSource.pitch = 1.0f;
        }
    }

    #endregion

    #region Volume Control

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }
    }

    public float GetMusicVolume() => musicVolume;
    public float GetSFXVolume() => sfxVolume;

    #endregion

    #region Gear Pitch Configuration

    // Method to update gear pitches at runtime if needed
    public void SetGearPitch(int gearIndex, float pitch)
    {
        if (gearIndex >= 0 && gearIndex < gearPitches.Length)
        {
            gearPitches[gearIndex] = Mathf.Clamp(pitch, 0.1f, 3.0f); // Reasonable pitch range
        }
    }

    // Get current gear pitch setting
    public float GetGearPitch(int gearIndex)
    {
        if (gearIndex >= 0 && gearIndex < gearPitches.Length)
        {
            return gearPitches[gearIndex];
        }
        return 1.0f; // Default pitch
    }

    #endregion
}