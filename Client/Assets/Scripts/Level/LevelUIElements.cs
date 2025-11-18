using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleParty.Levels
{
    [System.Serializable]
    public class LevelUIElements
    {
        public TextMeshProUGUI movesText;
        public TextMeshProUGUI welcomeText;
        public ParticleSystem correctTileParticlesPrefab;
        public GameObject gameEndOverlay;
        public TextMeshProUGUI gameEndTitle;
        public TextMeshProUGUI gameEndCoinsText;
        public Button restartButton;
        public Button nextLevelButton;

        // Add more UI elements here as needed in the future
        // public TextMeshProUGUI scoreText;
        // public GameObject pauseButton;
        // etc.
    }
}
