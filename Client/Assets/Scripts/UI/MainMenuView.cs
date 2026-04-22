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
        private Transform leftLevelMarkersContainer;

        [SerializeField]
        private Transform rightLevelMarkersContainer;

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
        /// Animates level markers appearing one by one across both pages
        /// </summary>
        private IEnumerator AnimateLevelMarkersIn(System.Action onComplete = null)
        {
            var allMarkers = new List<LevelMarkerView>();

            if (leftLevelMarkersContainer != null)
                foreach (Transform t in leftLevelMarkersContainer)
                {
                    var v = t.GetComponent<LevelMarkerView>();
                    if (v != null) allMarkers.Add(v);
                }

            if (rightLevelMarkersContainer != null)
                foreach (Transform t in rightLevelMarkersContainer)
                {
                    var v = t.GetComponent<LevelMarkerView>();
                    if (v != null) allMarkers.Add(v);
                }

            foreach (var markerView in allMarkers)
            {
                markerView.AnimateIn();
                yield return new WaitForSeconds(levelMarkerStaggerDelay);
            }

            onComplete?.Invoke();
        }

        /// <summary>
        /// Animates a new level marker appearing (when player completes a level)
        /// </summary>
        public void AnimateNewLevelCompletion(int levelIndex)
        {
            LevelMarkerView markerView = GetMarkerAtIndex(levelIndex);
            if (markerView != null)
            {
                Debug.Log($"Animating completion for level marker at index {levelIndex}");
                markerView.AnimateComplete();
            }
            else
            {
                Debug.LogWarning($"LevelMarkerView not found at index {levelIndex}");
            }
        }

        private LevelMarkerView GetMarkerAtIndex(int index)
        {
            int leftCount = leftLevelMarkersContainer != null ? leftLevelMarkersContainer.childCount : 0;

            if (index < leftCount)
            {
                return leftLevelMarkersContainer.GetChild(index).GetComponent<LevelMarkerView>();
            }

            int rightIndex = index - leftCount;
            if (rightLevelMarkersContainer != null && rightIndex < rightLevelMarkersContainer.childCount)
            {
                return rightLevelMarkersContainer.GetChild(rightIndex).GetComponent<LevelMarkerView>();
            }

            return null;
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
            if (leftLevelMarkersContainer != null)
                foreach (Transform child in leftLevelMarkersContainer)
                    Destroy(child.gameObject);

            if (rightLevelMarkersContainer != null)
                foreach (Transform child in rightLevelMarkersContainer)
                    Destroy(child.gameObject);
        }

        /// <summary>
        /// Adds a level marker. page=0 goes to the left page, page=1 to the right page.
        /// </summary>
        public void AddLevelMarker(int levelId, string levelName, bool isCompleted, int page = 0)
        {
            if (levelMarkerPrefab == null)
            {
                Debug.LogError("levelMarkerPrefab is NULL!");
                return;
            }

            Transform target = page == 0 ? leftLevelMarkersContainer : rightLevelMarkersContainer;

            if (target == null)
            {
                Debug.LogError($"Level marker container for page {page} is not assigned!");
                return;
            }

            GameObject markerObj = Instantiate(levelMarkerPrefab, target);
            LevelMarkerView markerView = markerObj.GetComponent<LevelMarkerView>();
            if (markerView != null)
            {
                markerView.Setup(levelId, levelName, isCompleted);
            }
        }
    }
}
