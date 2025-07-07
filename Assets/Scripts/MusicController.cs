using System.Collections;
using UnityEngine;

public class MusicController : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource; // Will also handle ambiance
    [SerializeField] private AudioSource engineSource; // NEW: Dedicated source for engine sound

    [Header("Background Music")]
    [SerializeField] private AudioClip titleScreenMusic;
    [SerializeField] private AudioClip gameplayMusic;
    [SerializeField] private AudioClip winScreenMusic;

    [Header("Ambiance")]
    [SerializeField] private AudioClip busyStreetAmbiance;
    [SerializeField] private AudioClip titleScreenAmbiance; // Optional: Different ambiance for title screen
    [SerializeField] private AudioClip winScreenAmbiance; // Optional: Different ambiance for win screen
    [SerializeField] private bool playAmbianceDuringTitle = true;
    [SerializeField] private bool playAmbianceDuringGameplay = true;
    [SerializeField] private bool playAmbianceDuringWin = true;

    [Header("Engine Sound")]
    [SerializeField] private AudioClip engineIdleSound; // NEW: Engine idle sound
    [SerializeField] private AudioClip engineRunningSound; // NEW: Engine running sound
    [SerializeField] private bool playEngineDuringTitle = false; // NEW: Engine sound during title screen
    [SerializeField] private bool playEngineDuringGameplay = true; // NEW: Engine sound during gameplay
    [SerializeField] private bool playEngineDuringWin = false; // NEW: Engine sound during win screen
    [SerializeField][Range(0f, 1f)] private float engineVolumeMultiplier = 0.6f; // NEW: Engine volume control
    [SerializeField] private bool enableEngineRPMVariation = true; // NEW: Enable RPM-based pitch variation
    [SerializeField][Range(0.5f, 2.0f)] private float minEnginePitch = 0.8f; // NEW: Minimum engine pitch
    [SerializeField][Range(0.5f, 2.0f)] private float maxEnginePitch = 1.4f; // NEW: Maximum engine pitch

    [Header("Sound Effects")]
    [SerializeField] private AudioClip buttonClickSFX;
    [SerializeField] private AudioClip gearChangeSFX;
    [SerializeField] private AudioClip obstacleHitSFX;
    [SerializeField] private AudioClip boostCollectSFX;
    [SerializeField] private AudioClip boostActiveSFX;
    [SerializeField] private AudioClip gameOverSFX;
    [SerializeField] private AudioClip winSFX;
    [SerializeField] private AudioClip boostCollisionSFX; // NEW: Special sound for boost collisions

    [Header("Countdown SFX")] // Countdown sound effects section
    [SerializeField] private AudioClip countdownFullSFX; // NEW: Single audio file with "3", "2", "1", "GO!"
    [SerializeField] private bool useCountdownSFX = true; // Toggle countdown sounds on/off

    // Remove the old individual countdown clips:
    // [SerializeField] private AudioClip countdownNumberSFX; // REMOVED
    // [SerializeField] private AudioClip countdownGoSFX; // REMOVED

    [Header("Individual SFX Volume Controls")]
    [SerializeField][Range(0f, 2f)] private float obstacleHitVolume = 1.2f; // Reduced from 2f
    [SerializeField][Range(0f, 1f)] private float gearChangeVolume = 0.6f; // Reduced from 1.0f
    [SerializeField][Range(0f, 1f)] private float boostCollectVolume = 0.7f; // Reduced from 1.0f
    [SerializeField][Range(0f, 1f)] private float boostActiveVolume = 0.8f; // Reduced from 1.0f
    [SerializeField][Range(0f, 1f)] private float buttonClickVolume = 0.5f; // Add button click volume
    [SerializeField][Range(0f, 1f)] private float gameOverVolume = 1.0f; // Add game over volume
    [SerializeField][Range(0f, 1f)] private float winVolume = 1.0f; // Add win volume
    [SerializeField][Range(0f, 1f)] private float countdownVolume = 1.0f; // Countdown volume control
    [SerializeField][Range(0f, 1f)] private float boostCollisionVolume = 1.5f; // NEW: Boost collision volume (louder for impact)
    [SerializeField][Range(0f, 1f)] private float ambianceVolumeMultiplier = 0.8f; // Increased from 0.5f

    [Header("Ambiance Ducking")]
    [SerializeField] private bool enableAmbianceDucking = true; // NEW: Enable ducking
    [SerializeField][Range(0f, 1f)] private float duckingAmount = 0.3f; // NEW: How much to reduce ambiance during SFX
    [SerializeField] private float duckingFadeTime = 0.1f; // NEW: How quickly to duck/unduck

    [Header("Gear Change Pitch Settings")]
    [SerializeField] private float[] gearPitches = { 0.8f, 0.9f, 1.0f, 1.1f, 1.2f }; // Pitch for each gear (1-5)
    [SerializeField] private bool useGearPitchVariation = true; // Toggle for pitch variation

    [Header("Settings")]
    [SerializeField] private float musicVolume = 0.7f;
    [SerializeField] private float sfxVolume = 0.8f;
    [SerializeField] private float fadeTime = 2f;

    // Singleton pattern for easy access
    public static MusicController Instance { get; private set; }

    // Current music and ambiance state tracking
    private AudioClip currentMusic;
    private AudioClip currentAmbiance; // Track current ambiance clip
    private AudioClip currentEngineSound; // NEW: Track current engine sound
    private bool isFading = false;
    private bool isAmbiancePlaying = false;
    private bool isEnginePlaying = false; // NEW: Track engine sound state
    private bool isAmbianceDucked = false; // NEW: Track ducking state
    private bool isEngineDucked = false; // NEW: Track engine ducking state
    private float originalAmbianceVolume; // NEW: Store original ambiance volume
    private float originalEngineVolume; // NEW: Store original engine volume
    private float currentEngineRPM = 0f; // NEW: Current engine RPM for pitch variation

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
            sfxSource.loop = false; // Will be set to true for ambiance
            sfxSource.playOnAwake = false;
            sfxSource.volume = sfxVolume;
        }

        // NEW: Initialize engine audio source
        if (engineSource == null)
        {
            engineSource = gameObject.AddComponent<AudioSource>();
            engineSource.loop = true;
            engineSource.playOnAwake = false;
            engineSource.volume = sfxVolume * engineVolumeMultiplier;
            engineSource.pitch = 1.0f;
        }
    }

    #region Music Control

    public void PlayTitleScreenMusic()
    {
        if (titleScreenMusic != null)
        {
            PlayMusic(titleScreenMusic);
        }

        // Play title screen ambiance
        if (playAmbianceDuringTitle)
        {
            AudioClip ambianceToPlay = titleScreenAmbiance != null ? titleScreenAmbiance : busyStreetAmbiance;
            PlayAmbiance(ambianceToPlay);
        }

        // NEW: Play engine sound during title screen
        if (playEngineDuringTitle)
        {
            AudioClip engineToPlay = engineIdleSound != null ? engineIdleSound : engineRunningSound;
            PlayEngineSound(engineToPlay);
        }
        else
        {
            StopEngineSound();
        }
    }

    public void PlayGameplayMusic()
    {
        if (gameplayMusic != null)
        {
            PlayMusic(gameplayMusic);
        }

        // Play gameplay ambiance
        if (playAmbianceDuringGameplay)
        {
            PlayAmbiance(busyStreetAmbiance);
        }

        // NEW: Play engine sound during gameplay
        if (playEngineDuringGameplay)
        {
            AudioClip engineToPlay = engineRunningSound != null ? engineRunningSound : engineIdleSound;
            PlayEngineSound(engineToPlay);
        }
        else
        {
            StopEngineSound();
        }
    }

    public void PlayWinScreenMusic()
    {
        if (winScreenMusic != null)
        {
            PlayMusic(winScreenMusic);
        }

        // Play win screen ambiance
        if (playAmbianceDuringWin)
        {
            AudioClip ambianceToPlay = winScreenAmbiance != null ? winScreenAmbiance : busyStreetAmbiance;
            PlayAmbiance(ambianceToPlay);
        }

        // NEW: Play engine sound during win screen
        if (playEngineDuringWin)
        {
            AudioClip engineToPlay = engineIdleSound != null ? engineIdleSound : engineRunningSound;
            PlayEngineSound(engineToPlay);
        }
        else
        {
            StopEngineSound();
        }
    }

    public void FadeToWinMusic()
    {
        if (winScreenMusic != null && !isFading)
        {
            StartCoroutine(FadeToNewMusic(winScreenMusic));
        }

        // Transition ambiance for win screen
        if (playAmbianceDuringWin)
        {
            AudioClip ambianceToPlay = winScreenAmbiance != null ? winScreenAmbiance : busyStreetAmbiance;
            PlayAmbiance(ambianceToPlay);
        }

        // NEW: Transition engine sound for win screen
        if (playEngineDuringWin)
        {
            AudioClip engineToPlay = engineIdleSound != null ? engineIdleSound : engineRunningSound;
            PlayEngineSound(engineToPlay);
        }
        else
        {
            StopEngineSound();
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

    #region Engine Sound Control

    // NEW: Play engine sound using dedicated engine source
    private void PlayEngineSound(AudioClip newClip)
    {
        if (newClip == null) return;

        // If same engine sound is already playing, don't restart
        if (newClip == currentEngineSound && isEnginePlaying && engineSource.isPlaying) return;

        // Stop current engine sound if playing
        if (isEnginePlaying)
        {
            StopEngineSound();
        }

        // Setup engine source for playback
        engineSource.clip = newClip;
        engineSource.loop = true;
        originalEngineVolume = sfxVolume * engineVolumeMultiplier;
        engineSource.volume = originalEngineVolume;
        engineSource.pitch = 1.0f;
        engineSource.Play();

        currentEngineSound = newClip;
        isEnginePlaying = true;
        isEngineDucked = false;

        Debug.Log($"Playing engine sound: {newClip.name} at volume {originalEngineVolume:F2}");
    }

    // NEW: Stop engine sound
    public void StopEngineSound()
    {
        if (isEnginePlaying)
        {
            engineSource.Stop();
            engineSource.volume = sfxVolume * engineVolumeMultiplier;
            currentEngineSound = null;
            isEnginePlaying = false;
            isEngineDucked = false;
            currentEngineRPM = 0f;
            Debug.Log("Engine sound stopped");
        }
    }

    // NEW: Pause engine sound
    public void PauseEngineSound()
    {
        if (isEnginePlaying)
        {
            engineSource.Pause();
        }
    }

    // NEW: Resume engine sound
    public void ResumeEngineSound()
    {
        if (isEnginePlaying)
        {
            engineSource.UnPause();
        }
    }

    // NEW: Set engine RPM for pitch variation (0-100 range typically)
    public void SetEngineRPM(float rpm)
    {
        currentEngineRPM = Mathf.Clamp(rpm, 0f, 100f);

        if (isEnginePlaying && enableEngineRPMVariation)
        {
            // Map RPM to pitch range
            float normalizedRPM = currentEngineRPM / 100f;
            float targetPitch = Mathf.Lerp(minEnginePitch, maxEnginePitch, normalizedRPM);
            engineSource.pitch = targetPitch;
        }
    }

    // NEW: Duck engine sound volume for SFX
    private void DuckEngineSound()
    {
        if (isEnginePlaying && enableAmbianceDucking && !isEngineDucked)
        {
            isEngineDucked = true;
            float targetVolume = originalEngineVolume * duckingAmount;
            StartCoroutine(FadeEngineVolume(targetVolume, duckingFadeTime));
        }
    }

    // NEW: Restore engine sound volume after SFX
    private void RestoreEngineSound()
    {
        if (isEnginePlaying && enableAmbianceDucking && isEngineDucked)
        {
            isEngineDucked = false;
            StartCoroutine(FadeEngineVolume(originalEngineVolume, duckingFadeTime));
        }
    }

    // NEW: Fade engine volume smoothly
    private IEnumerator FadeEngineVolume(float targetVolume, float duration)
    {
        float startVolume = engineSource.volume;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            engineSource.volume = Mathf.Lerp(startVolume, targetVolume, progress);
            yield return null;
        }

        engineSource.volume = targetVolume;
    }

    #endregion

    #region Ambiance Control

    // Play ambiance using the SFX source
    // In the PlayAmbiance method, add some additional safety checks:
    private void PlayAmbiance(AudioClip newClip)
    {
        if (newClip == null) return;

        // If same ambiance is already playing, don't restart
        if (newClip == currentAmbiance && isAmbiancePlaying && sfxSource.isPlaying) return;

        // Stop current ambiance if playing
        if (isAmbiancePlaying)
        {
            StopAmbiance();
        }

        // Setup SFX source for ambiance playback
        sfxSource.clip = newClip;
        sfxSource.loop = true; // Enable looping for ambiance
        originalAmbianceVolume = sfxVolume * ambianceVolumeMultiplier;
        sfxSource.volume = originalAmbianceVolume;
        sfxSource.pitch = 1.0f; // Reset pitch for ambiance

        // Additional safety: ensure the clip itself is set to loop in Unity
        if (newClip != null)
        {
            // Force loop setting (this ensures the AudioSource respects the loop setting)
            sfxSource.loop = true;
        }

        sfxSource.Play();

        currentAmbiance = newClip;
        isAmbiancePlaying = true;
        isAmbianceDucked = false;

        Debug.Log($"Playing ambiance: {newClip.name} at volume {originalAmbianceVolume:F2} - Loop enabled: {sfxSource.loop}");
    }

    // Stop ambiance
    public void StopAmbiance()
    {
        if (isAmbiancePlaying)
        {
            sfxSource.Stop();
            sfxSource.loop = false; // Reset loop for future SFX
            sfxSource.volume = sfxVolume; // Reset volume for future SFX
            currentAmbiance = null;
            isAmbiancePlaying = false;
            isAmbianceDucked = false;
            Debug.Log("Ambiance stopped");
        }
    }

    // Pause ambiance
    public void PauseAmbiance()
    {
        if (isAmbiancePlaying)
        {
            sfxSource.Pause();
        }
    }

    // Resume ambiance
    public void ResumeAmbiance()
    {
        if (isAmbiancePlaying)
        {
            sfxSource.UnPause();
        }
    }

    // NEW: Duck ambiance volume for SFX
    private void DuckAmbiance()
    {
        if (isAmbiancePlaying && enableAmbianceDucking && !isAmbianceDucked)
        {
            isAmbianceDucked = true;
            float targetVolume = originalAmbianceVolume * duckingAmount;
            StartCoroutine(FadeAmbianceVolume(targetVolume, duckingFadeTime));
        }
    }

    // NEW: Restore ambiance volume after SFX
    private void RestoreAmbiance()
    {
        if (isAmbiancePlaying && enableAmbianceDucking && isAmbianceDucked)
        {
            isAmbianceDucked = false;
            StartCoroutine(FadeAmbianceVolume(originalAmbianceVolume, duckingFadeTime));
        }
    }

    // NEW: Fade ambiance volume smoothly
    private IEnumerator FadeAmbianceVolume(float targetVolume, float duration)
    {
        float startVolume = sfxSource.volume;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            sfxSource.volume = Mathf.Lerp(startVolume, targetVolume, progress);
            yield return null;
        }

        sfxSource.volume = targetVolume;
    }

    // Play busy street ambiance directly
    public void PlayBusyStreetAmbiance()
    {
        PlayAmbiance(busyStreetAmbiance);
    }

    // Fade ambiance volume (useful for special moments)

    private IEnumerator FadeAmbianceCoroutine(float targetVolumeMultiplier, float duration)
    {
        float startVolume = sfxSource.volume;
        float targetVolume = sfxVolume * targetVolumeMultiplier;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            sfxSource.volume = Mathf.Lerp(startVolume, targetVolume, progress);
            yield return null;
        }

        sfxSource.volume = targetVolume;
    }

    #endregion

    #region Sound Effects

    // Modified SFX methods to use individual volume controls and ducking
    public void PlayButtonClickSFX()
    {
        PlaySFXWithVolume(buttonClickSFX, buttonClickVolume);
    }

    public void PlayGearChangeSFX()
    {
        PlaySFXWithVolume(gearChangeSFX, gearChangeVolume);
    }

    public void PlayGearChangeSFX(int gearIndex)
    {
        if (gearChangeSFX != null)
        {
            // Calculate pitch based on gear
            float pitch = 1.0f;

            if (useGearPitchVariation && gearIndex >= 0 && gearIndex < gearPitches.Length)
            {
                pitch = gearPitches[gearIndex];
            }

            // Play with specific pitch and volume
            PlaySFXWithPitchAndVolume(gearChangeSFX, pitch, gearChangeVolume);
        }
    }

    public void PlayObstacleHitSFX()
    {
        PlaySFXWithVolume(obstacleHitSFX, obstacleHitVolume);
    }

    public void PlayBoostCollectSFX()
    {
        PlaySFXWithVolume(boostCollectSFX, boostCollectVolume);
    }

    public void PlayBoostActiveSFX()
    {
        PlaySFXWithVolume(boostActiveSFX, boostActiveVolume);
    }

    public void PlayGameOverSFX()
    {
        PlaySFXWithVolume(gameOverSFX, gameOverVolume);
    }

    public void PlayWinSFX()
    {
        PlaySFXWithVolume(winSFX, winVolume);
    }

    // NEW: Play the full countdown sequence SFX (replaces the old individual methods)
    public void PlayCountdownFullSFX()
    {
        if (useCountdownSFX && countdownFullSFX != null)
        {
            PlaySFXWithVolume(countdownFullSFX, countdownVolume);
            Debug.Log("Playing full countdown SFX sequence");
        }
    }

    // DEPRECATED: Keep these methods for backward compatibility but make them do nothing
    public void PlayCountdownNumberSFX()
    {
        // This method is now deprecated - use PlayCountdownFullSFX() instead
        Debug.Log("PlayCountdownNumberSFX called but deprecated - use PlayCountdownFullSFX() instead");
    }

    public void PlayCountdownGoSFX()
    {
        // This method is now deprecated - use PlayCountdownFullSFX() instead
        Debug.Log("PlayCountdownGoSFX called but deprecated - use PlayCountdownFullSFX() instead");
    }

    // NEW: Play SFX with individual volume control (now also ducks engine sound)
    private void PlaySFXWithVolume(AudioClip clip, float volumeMultiplier)
    {
        if (clip != null && sfxSource != null)
        {
            // Duck ambiance and engine sound if enabled
            if (isAmbiancePlaying)
            {
                DuckAmbiance();
            }
            if (isEnginePlaying)
            {
                DuckEngineSound();
            }

            // Create a temporary AudioSource for the SFX to avoid conflicts
            GameObject tempSFX = new GameObject("TempSFX");
            AudioSource tempSource = tempSFX.AddComponent<AudioSource>();
            tempSource.clip = clip;
            tempSource.volume = sfxVolume * volumeMultiplier;
            tempSource.pitch = 1.0f;
            tempSource.Play();

            // Schedule cleanup and ambiance restoration
            StartCoroutine(CleanupTempSFX(tempSFX, clip.length));
        }
    }

    // NEW: Play SFX with pitch and volume control (now also ducks engine sound)
    private void PlaySFXWithPitchAndVolume(AudioClip clip, float pitch, float volumeMultiplier)
    {
        if (clip != null && sfxSource != null)
        {
            // Duck ambiance and engine sound if enabled
            if (isAmbiancePlaying)
            {
                DuckAmbiance();
            }
            if (isEnginePlaying)
            {
                DuckEngineSound();
            }

            // Create a temporary AudioSource for the SFX to avoid conflicts
            GameObject tempSFX = new GameObject("TempSFX");
            AudioSource tempSource = tempSFX.AddComponent<AudioSource>();
            tempSource.clip = clip;
            tempSource.volume = sfxVolume * volumeMultiplier;
            tempSource.pitch = pitch;
            tempSource.Play();

            // Schedule cleanup and ambiance restoration
            StartCoroutine(CleanupTempSFX(tempSFX, clip.length));
        }
    }

    // NEW: Cleanup temporary SFX and restore ambiance and engine sound
    private IEnumerator CleanupTempSFX(GameObject tempSFX, float clipLength)
    {
        // Wait for the SFX to finish
        yield return new WaitForSeconds(clipLength);

        // Restore ambiance and engine sound volume
        if (isAmbiancePlaying)
        {
            RestoreAmbiance();
        }
        if (isEnginePlaying)
        {
            RestoreEngineSound();
        }

        // Cleanup the temporary AudioSource
        if (tempSFX != null)
        {
            Destroy(tempSFX);
        }
    }

    // Legacy methods for backward compatibility
    private void PlaySFX(AudioClip clip)
    {
        PlaySFXWithVolume(clip, 1.0f);
    }

    private void PlaySFXWithPitch(AudioClip clip, float pitch)
    {
        PlaySFXWithPitchAndVolume(clip, pitch, 1.0f);
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

        // Update ambiance volume if playing
        if (isAmbiancePlaying)
        {
            originalAmbianceVolume = sfxVolume * ambianceVolumeMultiplier;
            if (!isAmbianceDucked)
            {
                sfxSource.volume = originalAmbianceVolume;
            }
        }

        // NEW: Update engine volume if playing
        if (isEnginePlaying)
        {
            originalEngineVolume = sfxVolume * engineVolumeMultiplier;
            if (!isEngineDucked)
            {
                engineSource.volume = originalEngineVolume;
            }
        }
    }

    // Set ambiance volume multiplier
    public void SetAmbianceVolumeMultiplier(float multiplier)
    {
        ambianceVolumeMultiplier = Mathf.Clamp01(multiplier);

        // Update current ambiance volume if playing
        if (isAmbiancePlaying)
        {
            originalAmbianceVolume = sfxVolume * ambianceVolumeMultiplier;
            if (!isAmbianceDucked)
            {
                sfxSource.volume = originalAmbianceVolume;
            }
        }
    }

    // NEW: Set engine volume multiplier
    public void SetEngineVolumeMultiplier(float multiplier)
    {
        engineVolumeMultiplier = Mathf.Clamp01(multiplier);

        // Update current engine volume if playing
        if (isEnginePlaying)
        {
            originalEngineVolume = sfxVolume * engineVolumeMultiplier;
            if (!isEngineDucked)
            {
                engineSource.volume = originalEngineVolume;
            }
        }
    }

    public float GetMusicVolume() => musicVolume;
    public float GetSFXVolume() => sfxVolume;
    public float GetAmbianceVolumeMultiplier() => ambianceVolumeMultiplier;
    public float GetEngineVolumeMultiplier() => engineVolumeMultiplier; // NEW: Getter for engine volume

    // Helper method to get effective ambiance volume
    public float GetAmbianceVolume() => sfxVolume * ambianceVolumeMultiplier;

    // NEW: Helper method to get effective engine volume
    public float GetEngineVolume() => sfxVolume * engineVolumeMultiplier;

    #endregion

    #region Gear Pitch Configuration

    // Method to update gear pitches at runtime if needed
    public void SetGearPitch(int gearIndex, float pitch)
    {
        if (gearIndex >= 0 && gearIndex < gearPitches.Length)
        {
            gearPitches[gearIndex] = Mathf.Clamp(pitch, 0.1f, 3.0f);
        }
    }

    // Get current gear pitch setting
    public float GetGearPitch(int gearIndex)
    {
        if (gearIndex >= 0 && gearIndex < gearPitches.Length)
        {
            return gearPitches[gearIndex];
        }
        return 1.0f;
    }

    // NEW: Boost collision SFX method
    public void PlayBoostCollisionSFX()
    {
        if (boostCollisionSFX != null)
        {
            PlaySFXWithVolume(boostCollisionSFX, boostCollisionVolume);
        }
    }

    #endregion

    #region Public Helper Methods

    // Check if ambiance is currently playing
    public bool IsAmbiancePlaying() => isAmbiancePlaying;

    // Get current ambiance clip
    public AudioClip GetCurrentAmbiance() => currentAmbiance;

    // NEW: Check if engine sound is currently playing
    public bool IsEnginePlaying() => isEnginePlaying;

    // NEW: Get current engine sound clip
    public AudioClip GetCurrentEngineSound() => currentEngineSound;

    // NEW: Get current engine RPM
    public float GetCurrentEngineRPM() => currentEngineRPM;

    // NEW: Control ducking settings
    public void SetDuckingEnabled(bool enabled)
    {
        enableAmbianceDucking = enabled;
    }

    public void SetDuckingAmount(float amount)
    {
        duckingAmount = Mathf.Clamp01(amount);
    }

    // NEW: Control engine RPM variation
    public void SetEngineRPMVariationEnabled(bool enabled)
    {
        enableEngineRPMVariation = enabled;

        // Reset pitch if disabled
        if (!enabled && isEnginePlaying)
        {
            engineSource.pitch = 1.0f;
        }
    }

    // NEW: Set engine pitch range
    public void SetEnginePitchRange(float minPitch, float maxPitch)
    {
        minEnginePitch = Mathf.Clamp(minPitch, 0.5f, 2.0f);
        maxEnginePitch = Mathf.Clamp(maxPitch, minEnginePitch, 2.0f);
    }

    #endregion
}