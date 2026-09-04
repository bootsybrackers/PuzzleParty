using System.Collections;
using UnityEngine;
using DG.Tweening;
using TMPro;

namespace PuzzleParty.Board.Effects
{
    /// <summary>
    /// Shared "victory text" animation: a punch-scale entrance, a burst of sparkles,
    /// a gold/white color pulse, then a slow idle breathing loop.
    /// Used by both the level-completed and map-completed overlays so their
    /// title text reads consistently.
    /// </summary>
    public static class VictoryTextEffect
    {
        private const string SPARKLE_PREFAB_PATH = "Prefabs/Effects/VictorySparkleEffect";

        public static IEnumerator Animate(
            TMP_Text text,
            float startDelay = 0.65f,
            float punchScale = 1.3f,
            float punchDuration = 0.4f,
            VictorySparkleEffect sparkleEffectPrefab = null)
        {
            if (text == null) yield break;

            RectTransform textRect = text.GetComponent<RectTransform>();
            if (textRect == null) yield break;

            Vector3 originalScale = textRect.localScale;
            textRect.localScale = Vector3.zero;

            yield return new WaitForSeconds(startDelay);

            Canvas canvas = text.GetComponentInParent<Canvas>();
            if (canvas == null) yield break;

            textRect.DOScale(originalScale * punchScale, punchDuration * 0.6f)
                .SetEase(Ease.OutBack)
                .SetLink(textRect.gameObject)
                .OnComplete(() => {
                    if (textRect != null) textRect.DOScale(originalScale, punchDuration * 0.4f).SetEase(Ease.InOutQuad).SetLink(textRect.gameObject);
                });

            VictorySparkleEffect sparklePrefab = sparkleEffectPrefab != null
                ? sparkleEffectPrefab
                : Resources.Load<VictorySparkleEffect>(SPARKLE_PREFAB_PATH);

            if (sparklePrefab != null)
            {
                VictorySparkleEffect sparkles = Object.Instantiate(sparklePrefab);
                sparkles.Play(textRect, canvas.transform);
            }

            DG.Tweening.Sequence colorSeq = DOTween.Sequence();
            Color goldColor = new Color(1f, 0.85f, 0.3f);
            Color brightGold = new Color(1f, 0.95f, 0.6f);
            colorSeq.Append(text.DOColor(brightGold, 0.3f));
            colorSeq.Append(text.DOColor(goldColor, 0.3f));
            colorSeq.Append(text.DOColor(Color.white, 0.3f));
            colorSeq.Append(text.DOColor(goldColor, 0.3f));
            colorSeq.SetLoops(3);
            colorSeq.SetLink(textRect.gameObject);
            colorSeq.OnComplete(() => { if (text != null) text.color = goldColor; });

            yield return new WaitForSeconds(1.5f);

            textRect.DOScale(originalScale * 1.05f, 0.8f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(textRect.gameObject);
        }
    }
}
