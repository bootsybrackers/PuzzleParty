using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace PuzzleParty.UI
{
    /// <summary>
    /// Represents a single level marker on the atlas left page
    /// Shows level icon/checkmark and level name with different states (locked/unlocked/completed)
    /// </summary>
    public class LevelMarkerView : MonoBehaviour
    {
        [SerializeField]
        private Image iconImage;

        [SerializeField]
        private TextMeshProUGUI levelNameText;

        [SerializeField]
        private Sprite uncompletedSprite;

        [SerializeField]
        private Sprite completedSprite;

        [Header("Animation Settings")]
        [SerializeField]
        private float animateInDuration = 0.3f;

        [SerializeField]
        private float animateCompleteDuration = 0.5f;

        [SerializeField]
        private Ease animateInEase = Ease.OutBack;

        private int levelId;
        private bool isCompleted;

        void Awake()
        {
            // Start hidden for initial animation
            transform.localScale = Vector3.zero;
        }

        /// <summary>
        /// Sets up the level marker with data
        /// </summary>
        public void Setup(int levelId, string levelName, bool isCompleted)
        {
            this.levelId = levelId;
            this.isCompleted = isCompleted;

            Debug.Log($"LevelMarkerView.Setup - LevelId: {levelId}, Name: {levelName}, Completed: {isCompleted}");
            Debug.Log($"  levelNameText is null: {levelNameText == null}");
            Debug.Log($"  iconImage is null: {iconImage == null}");

            if (levelNameText != null)
            {
                levelNameText.enabled = true; // Ensure text component is enabled
                levelNameText.text = levelName;
                Debug.Log($"  Set text to: {levelNameText.text}");
                Debug.Log($"  Text GameObject active: {levelNameText.gameObject.activeInHierarchy}");
                Debug.Log($"  Text enabled: {levelNameText.enabled}");
            }
            else
            {
                Debug.LogWarning($"  levelNameText is NULL for level {levelId}!");
            }

            UpdateVisuals();

            // If already completed when setup is called, skip the initial scale-to-zero
            // (they'll appear at full scale immediately)
            if (isCompleted)
            {
                transform.localScale = Vector3.one;
            }
        }

        /// <summary>
        /// Updates the visual state based on completion status
        /// </summary>
        private void UpdateVisuals()
        {
            if (iconImage != null)
            {
                iconImage.sprite = isCompleted ? completedSprite : uncompletedSprite;
            }

            // Set text color based on completion status
            if (levelNameText != null)
            {
                levelNameText.color = isCompleted ? Color.green : new Color(1f, 1f, 1f, 0.6f);
            }
        }

        /// <summary>
        /// Animates the marker appearing (used when atlas first opens)
        /// </summary>
        public void AnimateIn()
        {
            transform.DOScale(Vector3.one, animateInDuration)
                .SetEase(animateInEase);
        }

        /// <summary>
        /// Animates the marker changing to completed state (used when returning from game)
        /// </summary>
        public void AnimateComplete()
        {
            isCompleted = true;
            UpdateVisuals();

            // Pop animation with overshoot
            transform.DOPunchScale(Vector3.one * 0.3f, animateCompleteDuration, 5, 1f);

            // Optional: particle effect or checkmark animation could go here
        }
    }
}
