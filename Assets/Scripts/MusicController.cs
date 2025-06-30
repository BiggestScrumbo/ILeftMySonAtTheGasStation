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
            sfxSource.PlayOneShot(clip);
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
}