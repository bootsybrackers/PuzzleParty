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

        [Header("Game End - Success")]
        public GameObject gameEndSuccessPanel;
        public TextMeshProUGUI gameEndTitle;
        public TextMeshProUGUI gameEndCoinsText;
        public Button nextLevelButton;

        [Header("Game End - Fail")]
        public GameObject gameEndFailPanel;
        public TextMeshProUGUI gameEndTitleFail;
        public Button giveUpButton;

        [Header("Power-Ups")]
        public Button completePuzzleButton;
        public Button slotButton;

        [Header("Streak Icons")]
        public GameObject[] streakIcons; // Array of 3 icon GameObjects on banner

        [Header("EGP (End Game Purchase)")]
        public Button egpContinueButton;
        public TextMeshProUGUI egpPriceText;
        public TextMeshProUGUI egpOfferText;
    }
}
