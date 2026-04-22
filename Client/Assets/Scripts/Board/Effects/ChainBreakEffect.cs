using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace PuzzleParty.Board.Effects
{
    public class ChainBreakEffect : MonoBehaviour
    {
        [Header("Chain Break Particles")]
        [SerializeField] private int chainParticleCount = 12;
        [SerializeField] private float chainParticleDuration = 0.4f;
        [SerializeField] private float chainParticleSpread = 60f;
        [SerializeField] private Vector2 chainParticleSize = new Vector2(8f, 16f);

        public void Play(Vector3 worldPosition)
        {
            StartCoroutine(SpawnChainBreakParticles(worldPosition));
        }

        private IEnumerator SpawnChainBreakParticles(Vector3 worldPos)
        {
            Sprite sparkleSprite = Resources.Load<Sprite>("Images/Effects/sparkle_glow");
            if (sparkleSprite == null)
            {
                sparkleSprite = CreateFallbackSparkleSprite();
            }

            float maxDuration = 0f;

            for (int i = 0; i < chainParticleCount; i++)
            {
                GameObject particle = new GameObject("ChainParticle");
                particle.transform.position = worldPos;

                SpriteRenderer sr = particle.AddComponent<SpriteRenderer>();
                sr.sprite = sparkleSprite;
                sr.sortingOrder = 25;

                Color[] metalColors = new Color[]
                {
                    new Color(0.8f, 0.8f, 0.85f),
                    new Color(1f, 0.85f, 0.4f),
                    new Color(0.85f, 0.55f, 0.3f),
                    new Color(0.6f, 0.6f, 0.65f),
                };
                sr.color = metalColors[Random.Range(0, metalColors.Length)];

                float size = Random.Range(chainParticleSize.x, chainParticleSize.y) * 0.01f;
                particle.transform.localScale = new Vector3(size, size, 1f);

                float angle = (i / (float)chainParticleCount) * 360f + Random.Range(-15f, 15f);
                float distance = Random.Range(chainParticleSpread * 0.5f, chainParticleSpread) * 0.01f;
                Vector3 targetPos = worldPos + new Vector3(
                    Mathf.Cos(angle * Mathf.Deg2Rad) * distance,
                    Mathf.Sin(angle * Mathf.Deg2Rad) * distance,
                    0f
                );

                float duration = chainParticleDuration * Random.Range(0.8f, 1.2f);
                if (duration > maxDuration) maxDuration = duration;

                particle.transform.DOMove(targetPos, duration).SetEase(Ease.OutQuad).SetLink(particle);
                particle.transform.DOScale(Vector3.zero, duration).SetEase(Ease.InQuad).SetLink(particle);
                sr.DOFade(0f, duration * 0.8f).SetDelay(duration * 0.2f).SetLink(particle);

                Destroy(particle, duration + 0.1f);
            }

            yield return null;

            // Auto-destroy this effect after all particles finish
            Destroy(gameObject, maxDuration + 0.2f);
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
