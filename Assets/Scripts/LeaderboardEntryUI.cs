using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Dan.Main;

namespace LeaderboardCreatorDemo
{
    /// <summary>
    /// Component for individual leaderboard entry prefabs
    /// Attach this to your leaderboard entry prefab to help organize the UI elements
    /// </summary>
    public class LeaderboardEntryUI : MonoBehaviour
    {
        [Header("Text Components")]
        [SerializeField] private TMP_Text entryText; // Single text component for all info
        
        [Header("Visual Components")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image medalImage;
        
        [Header("Display Settings")]
        [SerializeField] private bool showMedals = true;
        [SerializeField] private bool highlightPlayerEntry = true;
        
        /// <summary>
        /// Sets the entry data with rank, username, and formatted score/time
        /// </summary>
        /// <param name="rank">Player's rank position</param>
        /// <param name="username">Player's username</param>
        /// <param name="scoreText">Formatted score or time string</param>
        /// <param name="isPlayerEntry">Whether this is the current player's entry</param>
        public void SetEntryData(int rank, string username, string scoreText, bool isPlayerEntry = false)
        {
            // Format everything into one string for single text display
            if (entryText != null)
            {
                entryText.text = $"{rank}. {username} - {scoreText}";
            }
            
            // Highlight player's entry
            if (isPlayerEntry && highlightPlayerEntry && backgroundImage != null)
            {
                backgroundImage.color = new Color(1f, 1f, 0f, 0.3f); // Light yellow
            }
            else if (backgroundImage != null)
            {
                backgroundImage.color = Color.white; // Reset to default
            }
            
            // Set medal colors for top 3
            if (showMedals && medalImage != null)
            {
                SetMedalVisual(rank);
            }
        }
        
        /// <summary>
        /// Alternative method for setting entry data with integer score
        /// </summary>
        /// <param name="rank">Player's rank position</param>
        /// <param name="username">Player's username</param>
        /// <param name="score">Raw score value</param>
        /// <param name="isPlayerEntry">Whether this is the current player's entry</param>
        public void SetEntryData(int rank, string username, int score, bool isPlayerEntry = false)
        {
            SetEntryData(rank, username, score.ToString(), isPlayerEntry);
        }
        
        /// <summary>
        /// Sets the medal visual based on rank
        /// </summary>
        /// <param name="rank">Player's rank position</param>
        private void SetMedalVisual(int rank)
        {
            if (medalImage == null) return;
            
            switch (rank)
            {
                case 1:
                    medalImage.color = new Color(1f, 0.84f, 0f); // Gold
                    medalImage.gameObject.SetActive(true);
                    break;
                case 2:
                    medalImage.color = new Color(0.75f, 0.75f, 0.75f); // Silver
                    medalImage.gameObject.SetActive(true);
                    break;
                case 3:
                    medalImage.color = new Color(0.8f, 0.5f, 0.2f); // Bronze
                    medalImage.gameObject.SetActive(true);
                    break;
                default:
                    medalImage.gameObject.SetActive(false);
                    break;
            }
        }
        
        /// <summary>
        /// Updates just the text content without changing visual styling
        /// </summary>
        /// <param name="rank">Player's rank position</param>
        /// <param name="username">Player's username</param>
        /// <param name="scoreText">Formatted score or time string</param>
        public void UpdateTextOnly(int rank, string username, string scoreText)
        {
            if (entryText != null)
            {
                entryText.text = $"{rank}. {username} - {scoreText}";
            }
        }
        
        /// <summary>
        /// Gets the formatted text currently displayed
        /// </summary>
        /// <returns>Current entry text or empty string if no text component</returns>
        public string GetDisplayText()
        {
            return entryText != null ? entryText.text : "";
        }
    }
}