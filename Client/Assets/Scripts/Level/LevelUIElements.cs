using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleParty.Levels
{
    [System.Serializable]
    public class LevelUIElements
    {
        public TextMeshProUGUI movesText;

        [Header("Level Banner")]
        public GameObject levelBanner;
        public TextMeshProUGUI levelBannerText;
        public Transform levelBannerItemsContainer;

        public ParticleSystem correctTileParticlesPrefab;
        public GameObject gameEndOverlay;
        public TextMeshProUGUI gameEndTitle;
        public TextMeshProUGUI gameEndCoinsText;
        public Button restartButton;
        public Button nextLevelButton;

        [Header("Power-Ups")]
        public Button completePuzzleButton;
        public Button slotButton;

        [Header("Streak Icons")]
        public GameObject[] streakIcons; // Array of 3 icon GameObjects on banner

        // Add more UI elements here as needed in the future
        // public TextMeshProUGUI scoreText;
        // public GameObject pauseButton;
        // etc.
    }
}
