using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class BoostIndicator : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject boostIndicatorPanel; // Main panel containing all boost UI elements
    public Image spacebarImage; // Your spacebar graphic
    public TMP_Text promptText; // Text like "Press SPACE to BOOST!"
    public Image boostMeterFill; // Optional: visual meter showing boost duration
    public TMP_Text chargesText; // Text showing boost charges (e.g., "3/3")

    [Header("Animation Settings")]
    public float pulseScale = 1.2f; // How much to scale during pulse
    public float pulseDuration = 0.8f; // Duration of one pulse cycle
    public Color availableColor = Color.green; // Color when boost is available
    public Color activeColor = Color.cyan; // Color during boost
    public Color unavailableColor = Color.gray; // Color when no charges

    [Header("Boost Meter Settings")]
    public bool showBoostMeter = true; // Whether to show duration meter during boost
    public Color meterFillColor = Color.yellow; // Color of the meter fill

    private GameController gameController;
    private bool isVisible = false;
    private Sequence pulseSequence;

    void Start()
    {
        // Find the GameController
        gameController = FindObjectOfType<GameController>();

        if (gameController == null)
        {
            Debug.LogError("BoostIndicator: GameController not found!");
            return;
        }

        // Initialize UI state
        SetupInitialState();

        Debug.Log("BoostIndicator: Initialized successfully!");
    }

    void Update()
    {
        if (gameController == null) return;

        // Update the indicator based on boost state
        UpdateBoostIndicator();
    }

    void SetupInitialState()
    {
        // Hide the panel initially
        if (boostIndicatorPanel != null)
            boostIndicatorPanel.SetActive(false);

        // Setup boost meter if available
        if (boostMeterFill != null)
        {
            boostMeterFill.fillAmount = 0f;
            boostMeterFill.color = meterFillColor;
        }

        isVisible = false;
    }

    void UpdateBoostIndicator()
    {
        bool hasCharges = gameController.GetBoostCharges() > 0;
        bool isBoostActive = gameController.IsBoostActive();

        // Determine if indicator should be visible
        bool shouldShow = hasCharges || isBoostActive;

        if (shouldShow != isVisible)
        {
            if (shouldShow)
                ShowIndicator();
            else
                HideIndicator();
        }

        // Update indicator appearance based on state
        if (isVisible)
        {
            UpdateIndicatorAppearance(hasCharges, isBoostActive);
        }
    }

    void ShowIndicator()
    {
        if (boostIndicatorPanel == null) return;

        isVisible = true;
        boostIndicatorPanel.SetActive(true);

        // Animate the panel sliding in or scaling up
        boostIndicatorPanel.transform.localScale = Vector3.zero;
        boostIndicatorPanel.transform.DOScale(Vector3.one, 0.3f)
            .SetEase(Ease.OutBack);

        Debug.Log("BoostIndicator: Showing boost indicator");
    }

    void HideIndicator()
    {
        if (boostIndicatorPanel == null) return;

        isVisible = false;

        // Stop any ongoing pulse animation
        StopPulseAnimation();

        // Animate the panel scaling down
        boostIndicatorPanel.transform.DOScale(Vector3.zero, 0.2f)
            .SetEase(Ease.InBack)
            .OnComplete(() => {
                boostIndicatorPanel.SetActive(false);
            });

        Debug.Log("BoostIndicator: Hiding boost indicator");
    }

    void UpdateIndicatorAppearance(bool hasCharges, bool isBoostActive)
    {
        // Update charges text
        if (chargesText != null)
        {
            int currentCharges = gameController.GetBoostCharges();
            int maxCharges = gameController.GetMaxBoostCharges();
            chargesText.text = $"{currentCharges}/{maxCharges}";
        }

        if (isBoostActive)
        {
            // During boost: show duration meter, change colors, stop pulsing
            ShowBoostActiveState();
        }
        else if (hasCharges)
        {
            // Has charges: pulse spacebar, show prompt, green colors
            ShowBoostAvailableState();
        }
    }

    void ShowBoostActiveState()
    {
        // Stop pulsing
        StopPulseAnimation();

        // Change colors to active state
        if (spacebarImage != null)
            spacebarImage.color = activeColor;

        if (promptText != null)
        {
            promptText.text = "BOOST ACTIVE!";
            promptText.color = activeColor;
        }

        if (chargesText != null)
            chargesText.color = activeColor;

        // Update boost meter if enabled
        if (showBoostMeter && boostMeterFill != null)
        {
            float remainingTime = gameController.GetBoostTimeRemaining();
            float totalTime = gameController.GetBoostDuration();
            boostMeterFill.fillAmount = remainingTime / totalTime;

            // Make meter visible during boost
            boostMeterFill.gameObject.SetActive(true);
        }
    }

    void ShowBoostAvailableState()
    {
        // Change colors to available state
        if (spacebarImage != null)
            spacebarImage.color = availableColor;

        if (promptText != null)
        {
            promptText.text = "Press SPACE to BOOST!";
            promptText.color = availableColor;
        }

        if (chargesText != null)
            chargesText.color = availableColor;

        // Hide boost meter when not boosting
        if (boostMeterFill != null)
            boostMeterFill.gameObject.SetActive(false);

        // Start pulsing animation
        StartPulseAnimation();
    }

    void StartPulseAnimation()
    {
        if (pulseSequence != null && pulseSequence.IsActive())
            return; // Already pulsing

        if (spacebarImage == null) return;

        // Create pulsing sequence
        pulseSequence = DOTween.Sequence();
        pulseSequence.Append(spacebarImage.transform.DOScale(pulseScale, pulseDuration * 0.5f))
                    .Append(spacebarImage.transform.DOScale(1f, pulseDuration * 0.5f))
                    .SetLoops(-1, LoopType.Restart)
                    .SetEase(Ease.InOutSine);
    }

    void StopPulseAnimation()
    {
        if (pulseSequence != null && pulseSequence.IsActive())
        {
            pulseSequence.Kill();

            // Reset scale
            if (spacebarImage != null)
                spacebarImage.transform.localScale = Vector3.one;
        }
    }

    void OnDestroy()
    {
        // Clean up DOTween sequences
        StopPulseAnimation();
    }
}