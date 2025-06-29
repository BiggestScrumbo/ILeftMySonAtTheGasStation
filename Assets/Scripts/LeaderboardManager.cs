using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// NOTE: Make sure to include the following namespace wherever you want to access Leaderboard Creator methods
using Dan.Main;

namespace LeaderboardCreatorDemo
{
    public class LeaderboardManager : MonoBehaviour
    {
        [Header("Static Leaderboard Display")]
        [SerializeField] private TMP_Text[] _entryTextObjects;
        [SerializeField] private TMP_InputField _usernameInputField;

        [Header("Gas Station UI")]
        [SerializeField] private GameObject gasStationLeaderboardPanel;
        [SerializeField] private Button showLeaderboardButton;
        [SerializeField] private Button hideLeaderboardButton;

        // Make changes to this section according to how you're storing the player's score:
        // ------------------------------------------------------------
        [SerializeField] private GameController game;

        private float time => game.gameTimer;
        
        // Convert float time to integer for leaderboard (centiseconds for 2 decimal precision)
        private int timeAsScore => Mathf.RoundToInt(time * 100);
        // ------------------------------------------------------------

        private bool isLeaderboardVisible = false; // Track leaderboard visibility state

        private void Start()
        {
            SetupGasStationLeaderboard();
            // Don't load entries immediately - wait until after win animation
        }

        private void SetupGasStationLeaderboard()
        {
            // Setup button listeners
            if (showLeaderboardButton != null)
                showLeaderboardButton.onClick.AddListener(ShowGasStationLeaderboard);
            
            if (hideLeaderboardButton != null)
                hideLeaderboardButton.onClick.AddListener(HideGasStationLeaderboard);

            // Initially hide ALL leaderboard components during normal gameplay
            HideAllLeaderboardComponents();
        }

        private void HideAllLeaderboardComponents()
        {
            // Hide static leaderboard text elements during gameplay
            if (_entryTextObjects != null)
            {
                foreach (var textObj in _entryTextObjects)
                {
                    if (textObj != null)
                        textObj.gameObject.SetActive(false);
                }
            }

            // Hide the gas station leaderboard panel
            if (gasStationLeaderboardPanel != null)
                gasStationLeaderboardPanel.SetActive(false);

            isLeaderboardVisible = false;
            Debug.Log("Static leaderboard components hidden for gameplay");
        }

        private void ShowAllLeaderboardComponents()
        {
            // Show static leaderboard text elements
            if (_entryTextObjects != null)
            {
                foreach (var textObj in _entryTextObjects)
                {
                    if (textObj != null)
                        textObj.gameObject.SetActive(true);
                }
            }

            // Show the gas station leaderboard panel
            if (gasStationLeaderboardPanel != null)
                gasStationLeaderboardPanel.SetActive(true);

            isLeaderboardVisible = true;
            Debug.Log("Static leaderboard components shown after win animation");
        }

        private void LoadEntries()
        {
            // Only load entries if leaderboard should be visible
            if (!isLeaderboardVisible)
            {
                Debug.Log("Leaderboard not visible, skipping entry loading");
                return;
            }

            // Q: How do I reference my own leaderboard?
            // A: Leaderboards.<NameOfTheLeaderboard>

            // For racing games, we want fastest times first (ascending order)
            Leaderboards.ILeftMySonAtTheGasStation.GetEntries(true, entries =>
            {
                // Only update if leaderboard is still supposed to be visible
                if (!isLeaderboardVisible) return;

                // Update static display using the text objects array
                if (_entryTextObjects != null)
                {
                    // Clear all text objects first
                    foreach (var t in _entryTextObjects)
                        t.text = "";

                    // Fill in the available entries
                    var length = Mathf.Min(_entryTextObjects.Length, entries.Length);
                    for (int i = 0; i < length; i++)
                    {
                        // Convert score back to time format for display
                        float displayTime = entries[i].Score / 100f;
                        string timeString = FormatTime(displayTime);
                        
                        // Format the leaderboard entry text
                        string entryText = $"{entries[i].Rank}. {entries[i].Username} - {timeString}";
                        
                        // Highlight the current player's entry
                        if (entries[i].IsMine())
                        {
                            entryText = $"<color=yellow>{entryText}</color>";
                        }
                        
                        _entryTextObjects[i].text = entryText;
                    }
                    
                    Debug.Log($"Loaded {length} entries into static leaderboard display");
                }
            });
        }

        public void ShowGasStationLeaderboard()
        {
            // Show static leaderboard components
            ShowAllLeaderboardComponents();
            
            // Load and refresh the leaderboard data
            LoadEntries();
            
            Debug.Log("Gas station static leaderboard shown");
        }

        public void HideGasStationLeaderboard()
        {
            // Hide static leaderboard components
            HideAllLeaderboardComponents();
            
            Debug.Log("Gas station static leaderboard hidden");
        }

        public void UploadEntry()
        {
            Leaderboards.ILeftMySonAtTheGasStation.UploadNewEntry(_usernameInputField.text, timeAsScore, isSuccessful =>
            {
                if (isSuccessful && isLeaderboardVisible)
                    LoadEntries();
            });
        }

        public void UploadEntryWithUsername(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                Debug.LogWarning("Username cannot be empty!");
                return;
            }

            Leaderboards.ILeftMySonAtTheGasStation.UploadNewEntry(username, timeAsScore, isSuccessful =>
            {
                if (isSuccessful)
                {
                    Debug.Log($"Successfully uploaded time: {FormatTime(time)} for user: {username}");
                    if (isLeaderboardVisible)
                        LoadEntries();
                    // Automatically show leaderboard after successful upload
                    ShowGasStationLeaderboard();
                }
                else
                {
                    Debug.LogError("Failed to upload leaderboard entry!");
                }
            });
        }
        
        /// <summary>
        /// Formats time in seconds to a readable string (MM:SS.ff format)
        /// </summary>
        /// <param name="timeInSeconds">Time in seconds</param>
        /// <returns>Formatted time string</returns>
        private string FormatTime(float timeInSeconds)
        {
            int minutes = Mathf.FloorToInt(timeInSeconds / 60);
            int seconds = Mathf.FloorToInt(timeInSeconds % 60);
            int centiseconds = Mathf.FloorToInt((timeInSeconds * 100) % 100);
            
            return $"{minutes:D2}:{seconds:D2}.{centiseconds:D2}";
        }

        // Public method to refresh leaderboard (can be called from other scripts)
        public void RefreshLeaderboard()
        {
            if (isLeaderboardVisible)
                LoadEntries();
        }

        private void OnDestroy()
        {
            // Clean up any remaining components if needed
        }
    }
}