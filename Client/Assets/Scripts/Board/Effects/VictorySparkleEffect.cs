using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace PuzzleParty.Board.Effects
{
    public class VictorySparkleEffect : MonoBehaviour
    {
        [Header("Victory Sparkles")]
        [SerializeField] private int victorySparkleCount = 16;
        [SerializeField] private float victorySparkleRadius = 120f;
        [SerializeField] private float victorySparkleDuration = 0.8f;

        public void Play(RectTransform centerRect, Transform parent)
        {
            StartCoroutine(SpawnVictorySparkles(centerRect, parent));
        }

        private IEnumerator SpawnVictorySparkles(RectTransform centerRect, Transform parent)
        {
            Sprite sparkleSprite = Resources.Load<Sprite>("Images/Effects/sparkle_glow");
            if (sparkleSprite == null)
            {
                sparkleSprite = CreateFallbackSparkleSprite();
            }

            Vector3 centerPos = centerRect.position;

            // First wave
            for (int i = 0; i < victorySparkleCount; i++)
            {
                GameObject sparkle = new GameObject($"VictorySparkle_{i}");
                sparkle.transform.SetParent(parent, false);

                Image sparkleImg = sparkle.AddComponent<Image>();
                sparkleImg.sprite = sparkleSprite;
                sparkleImg.raycastTarget = false;

                Color[] sparkleColors = new Color[]
                {
                    new Color(1f, 0.95f, 0.6f),
                    new Color(1f, 0.85f, 0.3f),
                    Color.white,
                    new Color(1f, 0.9f, 0.5f),
                };
                sparkleImg.color = sparkleColors[Random.Range(0, sparkleColors.Length)];

                RectTransform sparkleRect = sparkle.GetComponent<RectTransform>();

                float size = Random.Range(15f, 35f);
                sparkleRect.sizeDelta = new Vector2(size, size);
                sparkleRect.position = centerPos;

                float angle = (i / (float)victorySparkleCount) * 360f + Random.Range(-20f, 20f);
                float distance = victorySparkleRadius * Random.Range(0.7f, 1.3f);
                Vector3 targetPos = centerPos + new Vector3(
                    Mathf.Cos(angle * Mathf.Deg2Rad) * distance,
                    Mathf.Sin(angle * Mathf.Deg2Rad) * distance,
                    0f
                );

                float delay = (i / (float)victorySparkleCount) * 0.15f;

                sparkleRect.DOMove(targetPos, victorySparkleDuration)
                    .SetDelay(delay).SetEase(Ease.OutQuad).SetLink(sparkle);

                sparkleRect.DOScale(0f, victorySparkleDuration * 0.4f)
                    .SetDelay(delay + victorySparkleDuration * 0.6f).SetEase(Ease.InQuad).SetLink(sparkle);

                sparkleImg.DOFade(0f, victorySparkleDuration * 0.3f)
                    .SetDelay(delay + victorySparkleDuration * 0.7f).SetLink(sparkle);

                sparkle.transform.DORotate(new Vector3(0, 0, Random.Range(180f, 360f)), victorySparkleDuration, RotateMode.FastBeyond360)
                    .SetDelay(delay).SetEase(Ease.Linear).SetLink(sparkle);

                Destroy(sparkle, victorySparkleDuration + delay + 0.1f);
            }

            // Second wave
            yield return new WaitForSeconds(0.4f);

            for (int i = 0; i < victorySparkleCount / 2; i++)
            {
                GameObject sparkle = new GameObject($"VictorySparkle2_{i}");
                sparkle.transform.SetParent(parent, false);

                Image sparkleImg = sparkle.AddComponent<Image>();
                sparkleImg.sprite = sparkleSprite;
                sparkleImg.raycastTarget = false;

                sparkleImg.color = new Color(1f, 0.9f, 0.4f, 0.9f);

                RectTransform sparkleRect = sparkle.GetComponent<RectTransform>();
                float size = Random.Range(12f, 28f);
                sparkleRect.sizeDelta = new Vector2(size, size);
                sparkleRect.position = centerPos;

                float angle = Random.Range(0f, 360f);
                float distance = victorySparkleRadius * Random.Range(0.5f, 1.0f);
                Vector3 targetPos = centerPos + new Vector3(
                    Mathf.Cos(angle * Mathf.Deg2Rad) * distance,
                    Mathf.Sin(angle * Mathf.Deg2Rad) * distance,
                    0f
                );

                float delay = Random.Range(0f, 0.1f);

                sparkleRect.DOMove(targetPos, victorySparkleDuration * 0.8f)
                    .SetDelay(delay).SetEase(Ease.OutQuad).SetLink(sparkle);

                sparkleImg.DOFade(0f, victorySparkleDuration * 0.5f)
                    .SetDelay(delay + victorySparkleDuration * 0.3f).SetLink(sparkle);

                sparkleRect.DOScale(0f, victorySparkleDuration * 0.4f)
                    .SetDelay(delay + victorySparkleDuration * 0.4f).SetLink(sparkle);

                Destroy(sparkle, victorySparkleDuration + delay + 0.2f);
            }

            // Auto-destroy after all sparkles finish
            float totalLifetime = victorySparkleDuration + 0.4f + 0.2f + 0.5f;
            Destroy(gameObject, totalLifetime);
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
