using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

namespace PuzzleParty.UI
{
    /// <summary>
    /// Handles visual presentation and animations for the main menu (atlas visualization)
    /// </summary>
    public class MainMenuView : MonoBehaviour
    {
        [Header("Atlas Components")]
        [SerializeField]
        private RectTransform atlasContainer;

        [SerializeField]
        private Image mapImage;

        [SerializeField]
        private Transform levelMarkersContainer;

        [SerializeField]
        private TextMeshProUGUI mapNameText;

        [Header("Level Marker Prefab")]
        [SerializeField]
        private GameObject levelMarkerPrefab;

        [Header("Animation Settings")]
        [SerializeField]
        private float openDuration = 0.8f;

        [SerializeField]
        private float levelMarkerStaggerDelay = 0.1f;

        [SerializeField]
        private Ease openEase = Ease.OutBack;

        private bool isOpen = false;

        void Awake()
        {
            // Start hidden
            if (atlasContainer != null)
            {
                atlasContainer.localScale = Vector3.zero;
            }

            // Hide map name text initially
            if (mapNameText != null)
            {
                CanvasGroup canvasGroup = mapNameText.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = mapNameText.gameObject.AddComponent<CanvasGroup>();
                }
                canvasGroup.alpha = 0f;
            }
        }

        /// <summary>
        /// Animates the atlas opening with a book-opening effect
        /// </summary>
        public void AnimateOpen(System.Action onComplete = null)
        {
            if (isOpen)
            {
                onComplete?.Invoke();
                return;
            }

            isOpen = true;

            // Animate atlas container scaling up
            atlasContainer.DOScale(Vector3.one, openDuration)
                .SetEase(openEase)
                .OnComplete(() =>
                {
                    // After atlas opens, animate level markers
                    StartCoroutine(AnimateLevelMarkersIn(onComplete));
                });

            // Animate map name text fading in at the same time
            if (mapNameText != null)
            {
                CanvasGroup canvasGroup = mapNameText.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = mapNameText.gameObject.AddComponent<CanvasGroup>();
                }

                canvasGroup.DOFade(1f, openDuration)
                    .SetEase(Ease.InOutQuad);
            }
        }

        /// <summary>
        /// Animates level markers appearing one by one
        /// </summary>
        private IEnumerator AnimateLevelMarkersIn(System.Action onComplete = null)
        {
            if (levelMarkersContainer == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            for (int i = 0; i < levelMarkersContainer.childCount; i++)
            {
                Transform marker = levelMarkersContainer.GetChild(i);
                LevelMarkerView markerView = marker.GetComponent<LevelMarkerView>();

                if (markerView != null)
                {
                    markerView.AnimateIn();
                    yield return new WaitForSeconds(levelMarkerStaggerDelay);
                }
            }

            // All markers finished animating in
            onComplete?.Invoke();
        }

        /// <summary>
        /// Animates a new level marker appearing (when player completes a level)
        /// </summary>
        public void AnimateNewLevelCompletion(int levelIndex)
        {
            if (levelMarkersContainer == null)
            {
                Debug.LogWarning("levelMarkersContainer is null - cannot animate level completion");
                return;
            }

            if (levelIndex < 0 || levelIndex >= levelMarkersContainer.childCount)
            {
                Debug.LogWarning($"Level index {levelIndex} out of bounds (total markers: {levelMarkersContainer.childCount})");
                return;
            }

            Transform marker = levelMarkersContainer.GetChild(levelIndex);
            LevelMarkerView markerView = marker.GetComponent<LevelMarkerView>();

            if (markerView != null)
            {
                Debug.Log($"Animating completion for level marker at index {levelIndex}");
                markerView.AnimateComplete();
            }
            else
            {
                Debug.LogWarning($"LevelMarkerView not found on child at index {levelIndex}");
            }
        }

        /// <summary>
        /// Sets the map sprite for the right page
        /// </summary>
        public void SetMapSprite(Sprite sprite)
        {
            if (mapImage != null)
            {
                mapImage.sprite = sprite;
            }
        }

        /// <summary>
        /// Sets the map name text
        /// </summary>
        public void SetMapName(string mapName)
        {
            if (mapNameText != null)
            {
                mapNameText.text = mapName;
            }
        }

        /// <summary>
        /// Clears all level markers (for when switching maps)
        /// </summary>
        public void ClearLevelMarkers()
        {
            if (levelMarkersContainer == null) return;

            foreach (Transform child in levelMarkersContainer)
            {
                Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// Adds a level marker to the container
        /// </summary>
        public void AddLevelMarker(int levelId, string levelName, bool isCompleted)
        {
            if (levelMarkersContainer == null || levelMarkerPrefab == null)
            {
                Debug.LogError("levelMarkersContainer or levelMarkerPrefab is NULL!");
                return;
            }

            GameObject markerObj = Instantiate(levelMarkerPrefab, levelMarkersContainer);
            LevelMarkerView markerView = markerObj.GetComponent<LevelMarkerView>();

            if (markerView != null)
            {
                markerView.Setup(levelId, levelName, isCompleted);
            }
        }
    }
}
