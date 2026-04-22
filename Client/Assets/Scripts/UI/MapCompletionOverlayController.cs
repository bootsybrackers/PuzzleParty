using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using PuzzleParty.Board.Effects;
using PuzzleParty.Service;
using PuzzleParty.Progressions;

namespace PuzzleParty.UI
{
    /// <summary>
    /// Full-screen overlay shown when the player beats the last level of a map.
    /// Awards MAP_COMPLETION_COINS coins, plays fireworks, and presents a Next Map button.
    /// Sits in the scene under MainMenuCanvas/MapCompletionOverlay (inactive by default).
    /// </summary>
    public class MapCompletionOverlayController : MonoBehaviour
    {
        private const string FIREWORKS_PREFAB_PATH = "Prefabs/Effects/FireworksEffect";
        private const int MAP_COMPLETION_COINS = 1000;

        [Header("Overlay Root")]
        [SerializeField] private GameObject overlayRoot;

        [Header("Dynamic Text")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI mapNameText;
        [SerializeField] private TextMeshProUGUI coinCounterText;

        [Header("Button")]
        [SerializeField] private Button nextMapButton;
        [SerializeField] private TextMeshProUGUI buttonLabel;

        private IProgressionService progressionService;
        private FireworksEffect activeFireworks;
        private System.Action pendingOnNextMap;

        void Start()
        {
            progressionService = ServiceLocator.GetInstance().Get<ProgressionService>();

            if (nextMapButton != null)
                nextMapButton.onClick.AddListener(OnNextMapButtonClicked);
        }

        // ─── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Show the overlay.
        /// completedMapName  – name of the map the player just finished.
        /// nextMapName       – name of the next map, or null/empty if none exists.
        /// onNextMap         – called after the overlay finishes dismissing.
        /// </summary>
        public void Show(string completedMapName, string nextMapName, System.Action onNextMap)
        {
            // Award coins immediately so the counter reflects reality
            Progression prog = progressionService.GetProgression();
            prog.coins += MAP_COMPLETION_COINS;
            progressionService.SaveProgression(prog);

            pendingOnNextMap = onNextMap;

            // Populate dynamic fields
            if (mapNameText != null)
                mapNameText.text = completedMapName;

            if (coinCounterText != null)
                coinCounterText.text = "+0 coins";

            bool hasNextMap = !string.IsNullOrEmpty(nextMapName);
            if (buttonLabel != null)
                buttonLabel.text = hasNextMap ? $"Next Map\n► {nextMapName}" : "More content\ncoming soon!";

            if (nextMapButton != null)
            {
                var img = nextMapButton.GetComponent<Image>();
                if (img != null)
                    img.color = hasNextMap ? new Color(0.15f, 0.55f, 0.22f) : new Color(0.28f, 0.28f, 0.48f);
            }

            // Show the root
            if (overlayRoot != null)
                overlayRoot.SetActive(true);

            // Launch fireworks
            StartFireworks();

            // Entrance animation
            if (overlayRoot != null)
            {
                overlayRoot.transform.localScale = Vector3.zero;
                overlayRoot.transform
                    .DOScale(Vector3.one, 0.55f)
                    .SetEase(Ease.OutBack)
                    .SetLink(overlayRoot);
            }

            // Animate title punch and coin counter
            if (titleText != null)
                StartCoroutine(AnimateTitle(titleText));

            if (coinCounterText != null)
                StartCoroutine(AnimateCoinCounter(coinCounterText, MAP_COMPLETION_COINS));

            // Pulse button
            if (nextMapButton != null)
            {
                nextMapButton.transform.DOKill();
                nextMapButton.transform.DOScale(1.04f, 0.75f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetDelay(0.5f)
                    .SetLink(nextMapButton.gameObject);
            }
        }

        public void Hide(System.Action onHidden = null)
        {
            StopFireworks();

            if (overlayRoot != null)
            {
                overlayRoot.transform.DOKill();
                overlayRoot.transform
                    .DOScale(Vector3.zero, 0.35f)
                    .SetEase(Ease.InBack)
                    .SetLink(overlayRoot)
                    .OnComplete(() =>
                    {
                        if (overlayRoot != null) overlayRoot.SetActive(false);
                        onHidden?.Invoke();
                    });
            }
            else
            {
                onHidden?.Invoke();
            }
        }

        private void OnNextMapButtonClicked()
        {
            Hide(() => pendingOnNextMap?.Invoke());
        }

        // ─── Animations ───────────────────────────────────────────────────────

        private IEnumerator AnimateTitle(TextMeshProUGUI text)
        {
            yield return new WaitForSeconds(0.65f);
            if (text == null) yield break;
            text.transform.DOPunchScale(Vector3.one * 0.28f, 0.55f, 5, 0.8f).SetLink(text.gameObject);
        }

        private IEnumerator AnimateCoinCounter(TextMeshProUGUI text, int target)
        {
            yield return new WaitForSeconds(0.9f);

            float duration = Mathf.Min(target * 0.001f, 1.8f);
            float elapsed  = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                int current = Mathf.RoundToInt(Mathf.Lerp(0, target, elapsed / duration));
                if (text != null) text.text = $"+{current} coins";
                yield return null;
            }

            if (text != null)
            {
                text.text = $"+{target} coins";
                text.transform.DOPunchScale(Vector3.one * 0.22f, 0.45f, 5, 0.8f).SetLink(text.gameObject);
            }
        }

        // ─── Fireworks ────────────────────────────────────────────────────────

        private void StartFireworks()
        {
            var prefab = Resources.Load<FireworksEffect>(FIREWORKS_PREFAB_PATH);
            if (prefab != null)
            {
                activeFireworks = Instantiate(prefab);
            }
            else
            {
                var go = new GameObject("FireworksEffect");
                activeFireworks = go.AddComponent<FireworksEffect>();
            }

            Canvas canvas = overlayRoot != null
                ? overlayRoot.GetComponentInParent<Canvas>()
                : GetComponentInParent<Canvas>();

            activeFireworks.Play(canvas != null ? canvas.transform : transform);
        }

        private void StopFireworks()
        {
            if (activeFireworks != null)
            {
                activeFireworks.Stop();
                Destroy(activeFireworks.gameObject);
                activeFireworks = null;
            }
        }

        void OnDestroy()
        {
            if (nextMapButton != null)
                nextMapButton.onClick.RemoveListener(OnNextMapButtonClicked);

            StopFireworks();
        }
    }
}
