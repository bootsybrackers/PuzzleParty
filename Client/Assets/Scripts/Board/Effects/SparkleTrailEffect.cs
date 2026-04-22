using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace PuzzleParty.Board.Effects
{
    public class SparkleTrailEffect : MonoBehaviour
    {
        [Header("Sparkle Trail")]
        [SerializeField] private int sparkleCount = 12;
        [SerializeField] private float sparkleDuration = 0.65f;
        [SerializeField] private float sparkleStaggerTime = 0.2f;
        [SerializeField] private Vector2 sparkleSize = new Vector2(28f, 44f);
        [SerializeField] private float pathCurveStrength = 40f;
        [SerializeField] private float pathXSpread = 15f;
        [Tooltip("First sparkles are this much larger than last (comet head effect)")]
        [SerializeField] private float headToTailRatio = 2.0f;

        [Header("Sparkle Color")]
        [SerializeField] private float sparkleHueMin = 0.08f;
        [SerializeField] private float sparkleHueMax = 0.14f;
        [SerializeField] private float sparkleSaturationMin = 0.5f;
        [SerializeField] private float sparkleSaturationMax = 0.85f;

        [Header("Sparkle Fade")]
        [Range(0f, 1f)]
        [SerializeField] private float sparkleFadeStart = 0.75f;
        [Range(0f, 1f)]
        [SerializeField] private float sparkleShrinkStart = 0.7f;

        [Header("Impact Burst")]
        [SerializeField] private int burstParticleCount = 10;
        [SerializeField] private float burstSize = 22f;
        [SerializeField] private Vector2 burstDistance = new Vector2(50f, 100f);
        [SerializeField] private float burstDuration = 0.25f;

        [Header("Target Pulse")]
        [SerializeField] private float pulseScale = 1.2f;
        [SerializeField] private float pulseDuration = 0.18f;

        public void Play(Vector3 sourcePos, Vector3 targetPos, Transform sparkleParent, Transform pulseTarget, System.Action onComplete)
        {
            StartCoroutine(AnimateTrail(sourcePos, targetPos, sparkleParent, pulseTarget, onComplete));
        }

        private IEnumerator AnimateTrail(Vector3 sourcePos, Vector3 targetPos, Transform sparkleParent, Transform pulseTarget, System.Action onComplete)
        {
            Sprite sparkleSprite = Resources.Load<Sprite>("Images/Effects/sparkle_glow");
            if (sparkleSprite == null)
            {
                sparkleSprite = CreateFallbackSparkleSprite();
            }

            List<GameObject> sparkles = new List<GameObject>();
            float totalDuration = sparkleDuration;
            float staggerInterval = sparkleStaggerTime / sparkleCount;

            for (int i = 0; i < sparkleCount; i++)
            {
                GameObject sparkle = new GameObject($"Sparkle_{i}");
                Image sparkleImg = sparkle.AddComponent<Image>();
                sparkleImg.sprite = sparkleSprite;
                sparkleImg.raycastTarget = false;

                RectTransform sparkleRect = sparkle.GetComponent<RectTransform>();
                sparkleRect.SetParent(sparkleParent, false);
                sparkleRect.position = sourcePos;

                float t01 = (float)i / Mathf.Max(1, sparkleCount - 1);
                float sizeMultiplier = Mathf.Lerp(headToTailRatio, 1f, t01);
                float baseSize = Random.Range(sparkleSize.x, sparkleSize.y) * sizeMultiplier;
                sparkleRect.sizeDelta = new Vector2(baseSize, baseSize);

                sparkle.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

                float hueShift = Random.Range(sparkleHueMin, sparkleHueMax);
                float sat = Mathf.Lerp(sparkleSaturationMin, sparkleSaturationMax, t01);
                sparkleImg.color = Color.HSVToRGB(hueShift, sat, 1f);

                sparkles.Add(sparkle);

                float delay = i * staggerInterval;
                float sparkleLifetime = totalDuration - delay;
                if (sparkleLifetime < 0.15f) sparkleLifetime = 0.15f;

                float yOffset = Random.Range(-pathCurveStrength, pathCurveStrength);
                Vector3 controlPoint = Vector3.Lerp(sourcePos, targetPos, 0.5f) + new Vector3(Random.Range(-pathXSpread, pathXSpread), yOffset, 0);

                Vector3 startPos = sourcePos;
                DOVirtual.Float(0f, 1f, sparkleLifetime, t =>
                {
                    if (sparkleRect != null)
                    {
                        float u = 1f - t;
                        Vector3 pos = u * u * startPos + 2f * u * t * controlPoint + t * t * targetPos;
                        sparkleRect.position = pos;
                    }
                }).SetEase(Ease.InQuad).SetDelay(delay).SetLink(sparkle);

                sparkleImg.DOFade(0f, sparkleLifetime * (1f - sparkleFadeStart))
                    .SetDelay(delay + sparkleLifetime * sparkleFadeStart).SetLink(sparkle);

                sparkleRect.DOScale(0f, sparkleLifetime * (1f - sparkleShrinkStart))
                    .SetDelay(delay + sparkleLifetime * sparkleShrinkStart).SetLink(sparkle);

                sparkle.transform.DORotate(new Vector3(0, 0, Random.Range(180f, 540f)), sparkleLifetime, RotateMode.FastBeyond360)
                    .SetDelay(delay).SetEase(Ease.Linear).SetLink(sparkle);
            }

            yield return new WaitForSeconds(totalDuration + 0.05f);

            // Impact burst at target
            List<GameObject> burstSparkles = new List<GameObject>();
            for (int i = 0; i < burstParticleCount; i++)
            {
                GameObject burst = new GameObject($"BurstSparkle_{i}");
                Image burstImg = burst.AddComponent<Image>();
                burstImg.sprite = sparkleSprite;
                burstImg.raycastTarget = false;
                float burstHue = Random.Range(sparkleHueMin, sparkleHueMax);
                burstImg.color = Color.HSVToRGB(burstHue, Random.Range(sparkleSaturationMin, sparkleSaturationMax), 1f);

                RectTransform burstRect = burst.GetComponent<RectTransform>();
                burstRect.SetParent(sparkleParent, false);
                burstRect.position = targetPos;
                burstRect.sizeDelta = new Vector2(burstSize, burstSize);

                burstSparkles.Add(burst);

                float angle = (360f / burstParticleCount) * i + Random.Range(-15f, 15f);
                float radians = angle * Mathf.Deg2Rad;
                Vector3 direction = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0);
                float dist = Random.Range(burstDistance.x, burstDistance.y);
                Vector3 burstTarget = targetPos + direction * dist;

                burstRect.DOMove(burstTarget, burstDuration).SetEase(Ease.OutQuad).SetLink(burst);
                burstImg.DOFade(0f, burstDuration).SetEase(Ease.InQuad).SetLink(burst);
                burstRect.DOScale(0f, burstDuration).SetEase(Ease.InQuad).SetLink(burst);
            }

            // Pulse target
            if (pulseTarget != null && pulseTarget.GetComponent<RectTransform>() != null)
            {
                pulseTarget.DOScale(pulseScale, pulseDuration).SetEase(Ease.OutBack).SetLink(pulseTarget.gameObject)
                    .OnComplete(() => { if (pulseTarget != null) pulseTarget.DOScale(1f, pulseDuration * 0.67f).SetEase(Ease.InQuad).SetLink(pulseTarget.gameObject); });
            }

            yield return new WaitForSeconds(burstDuration + 0.05f);

            // Cleanup
            foreach (var s in sparkles)
            {
                if (s != null) Destroy(s);
            }
            foreach (var s in burstSparkles)
            {
                if (s != null) Destroy(s);
            }

            onComplete?.Invoke();
            Destroy(gameObject);
        }

        private static Sprite CreateFallbackSparkleSprite()
        {
            int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = size / 2f;
            float radius = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float norm = Mathf.Clamp01(dist / radius);
                    float core = Mathf.Clamp01(1f - norm * 3f);
                    float glow = (1f - norm) * (1f - norm);
                    float alpha = Mathf.Clamp01(core + glow);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
