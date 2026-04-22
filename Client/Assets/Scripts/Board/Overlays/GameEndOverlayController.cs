using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using PuzzleParty.EGP;
using PuzzleParty.Board.Effects;

namespace PuzzleParty.Board
{
    public class GameEndOverlayController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject successPanel;
        [SerializeField] private GameObject failPanel;

        [Header("Success UI")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private TMP_Text coinsText;

        [Header("Fail UI")]
        [SerializeField] private TMP_Text titleTextFail;
        [SerializeField] private Button giveUpButton;

        [Header("EGP UI")]
        [SerializeField] private TMP_Text egpOfferText;
        [SerializeField] private TMP_Text egpPriceText;
        [SerializeField] private Button egpContinueButton;

        [Header("Effects")]
        [SerializeField] private ConfettiEffect confettiEffectPrefab;
        [SerializeField] private VictorySparkleEffect victorySparkleEffectPrefab;

        [Header("Victory Text Animation")]
        [SerializeField] private float victoryTextPunchScale = 1.3f;
        [SerializeField] private float victoryTextPunchDuration = 0.4f;

        private ConfettiEffect activeConfettiInstance;

        public void ShowSuccess(int coinsEarned, System.Action onNextLevel)
        {
            gameObject.SetActive(true);
            if (successPanel != null) successPanel.SetActive(true);
            if (failPanel != null) failPanel.SetActive(false);

            if (titleText != null)
            {
                titleText.text = "Victory!";
                StartCoroutine(AnimateVictoryText(titleText));
            }

            if (nextLevelButton != null)
            {
                nextLevelButton.onClick.RemoveAllListeners();
                nextLevelButton.onClick.AddListener(() => onNextLevel?.Invoke());
                StartContinueButtonPulseAnimation();
            }

            if (coinsText != null)
                StartCoroutine(AnimateCoinCounter(coinsEarned));

            StartConfetti();
            ShowOverlayAnimation();
        }

        public void ShowFailure(System.Action onGiveUp)
        {
            gameObject.SetActive(true);
            if (successPanel != null) successPanel.SetActive(false);
            if (failPanel != null) failPanel.SetActive(true);

            StopConfetti();
            HideEGPElements();

            if (titleTextFail != null)
                titleTextFail.text = "Out of Moves!";

            if (giveUpButton != null)
            {
                giveUpButton.onClick.RemoveAllListeners();
                giveUpButton.onClick.AddListener(() => onGiveUp?.Invoke());
            }

            ShowOverlayAnimation();
        }

        public void ShowEGP(EGPRound offer, bool canAfford, System.Action onPurchase, System.Action onGiveUp)
        {
            gameObject.SetActive(true);
            if (successPanel != null) successPanel.SetActive(false);
            if (failPanel != null) failPanel.SetActive(true);

            StopConfetti();

            if (egpOfferText != null)
            {
                egpOfferText.gameObject.SetActive(true);
                egpOfferText.text = $"+{offer.contents.extraMoves} moves";
            }

            if (egpPriceText != null)
            {
                egpPriceText.gameObject.SetActive(true);
                egpPriceText.text = $"{offer.price}";
            }

            if (egpContinueButton != null)
            {
                egpContinueButton.gameObject.SetActive(true);
                egpContinueButton.interactable = canAfford;
                egpContinueButton.onClick.RemoveAllListeners();
                egpContinueButton.onClick.AddListener(() => onPurchase?.Invoke());
            }

            if (giveUpButton != null)
            {
                giveUpButton.onClick.RemoveAllListeners();
                giveUpButton.onClick.AddListener(() => onGiveUp?.Invoke());
            }

            ShowOverlayAnimation();
        }

        public void Hide()
        {
            StopConfetti();
            StopContinueButtonPulseAnimation();
            HideEGPElements();
            gameObject.SetActive(false);
        }

        public void HideEGPElements()
        {
            if (egpContinueButton != null) egpContinueButton.gameObject.SetActive(false);
            if (egpOfferText != null) egpOfferText.gameObject.SetActive(false);
            if (egpPriceText != null) egpPriceText.gameObject.SetActive(false);
        }

        private void ShowOverlayAnimation()
        {
            gameObject.SetActive(true);

            Image overlayImage = GetComponent<Image>();
            if (overlayImage != null)
            {
                Color imageColor = overlayImage.color;
                imageColor.a = 0f;
                overlayImage.color = imageColor;
                overlayImage.DOFade(0.9f, 0.6f).SetEase(Ease.OutQuad);
            }

            transform.localScale = Vector3.zero;
            transform.DOScale(Vector3.one, 0.6f)
                .SetEase(Ease.OutBack)
                .SetDelay(0.2f)
                .SetLink(gameObject);
        }

        private void StartConfetti()
        {
            StopConfetti();
            if (confettiEffectPrefab == null) return;

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("Cannot spawn confetti - missing Canvas");
                return;
            }

            activeConfettiInstance = Instantiate(confettiEffectPrefab);
            activeConfettiInstance.Play(canvas.transform);
        }

        private void StopConfetti()
        {
            if (activeConfettiInstance != null)
            {
                activeConfettiInstance.Stop();
                Destroy(activeConfettiInstance.gameObject);
                activeConfettiInstance = null;
            }
        }

        private void StartContinueButtonPulseAnimation()
        {
            if (nextLevelButton == null) return;

            nextLevelButton.transform.DOKill();
            nextLevelButton.transform.localScale = Vector3.one;
            nextLevelButton.transform.DOScale(1.05f, 0.8f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetDelay(0.9f)
                .SetLink(nextLevelButton.gameObject);
        }

        private void StopContinueButtonPulseAnimation()
        {
            if (nextLevelButton == null) return;

            nextLevelButton.transform.DOKill();
            nextLevelButton.transform.localScale = Vector3.one;
        }

        private IEnumerator AnimateCoinCounter(int coinsEarned)
        {
            yield return new WaitForSeconds(0.5f);

            int currentCoins = 0;
            float duration = Mathf.Min(coinsEarned * 0.05f, 2f);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                currentCoins = Mathf.RoundToInt(Mathf.Lerp(0, coinsEarned, progress));
                coinsText.text = $"+{currentCoins} coins!";
                yield return null;
            }

            coinsText.text = $"+{coinsEarned} coins!";
        }

        private IEnumerator AnimateVictoryText(TMP_Text victoryText)
        {
            RectTransform textRect = victoryText.GetComponent<RectTransform>();
            if (textRect == null) yield break;

            Vector3 originalScale = textRect.localScale;
            textRect.localScale = Vector3.zero;

            yield return new WaitForSeconds(0.9f);

            Canvas canvas = victoryText.GetComponentInParent<Canvas>();
            if (canvas == null) yield break;

            textRect.DOScale(originalScale * victoryTextPunchScale, victoryTextPunchDuration * 0.6f)
                .SetEase(Ease.OutBack)
                .SetLink(textRect.gameObject)
                .OnComplete(() => {
                    if (textRect != null) textRect.DOScale(originalScale, victoryTextPunchDuration * 0.4f).SetEase(Ease.InOutQuad).SetLink(textRect.gameObject);
                });

            if (victorySparkleEffectPrefab != null)
            {
                VictorySparkleEffect sparkles = Instantiate(victorySparkleEffectPrefab);
                sparkles.Play(textRect, canvas.transform);
            }

            DG.Tweening.Sequence colorSeq = DOTween.Sequence();
            Color goldColor = new Color(1f, 0.85f, 0.3f);
            Color brightGold = new Color(1f, 0.95f, 0.6f);
            colorSeq.Append(victoryText.DOColor(brightGold, 0.3f));
            colorSeq.Append(victoryText.DOColor(goldColor, 0.3f));
            colorSeq.Append(victoryText.DOColor(Color.white, 0.3f));
            colorSeq.Append(victoryText.DOColor(goldColor, 0.3f));
            colorSeq.SetLoops(3);
            colorSeq.OnComplete(() => victoryText.color = goldColor);

            yield return new WaitForSeconds(1.5f);

            textRect.DOScale(originalScale * 1.05f, 0.8f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(textRect.gameObject);
        }
    }
}
