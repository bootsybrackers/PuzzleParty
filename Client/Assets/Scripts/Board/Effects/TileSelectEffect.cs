using UnityEngine;
using DG.Tweening;

namespace PuzzleParty.Board.Effects
{
    /// <summary>
    /// Soft glow shown behind a tile when selected for the swap power-up.
    /// Assign a radial glow sprite (e.g. sparkle_glow.png) to the SpriteRenderer.
    /// </summary>
    public class TileSelectEffect : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer ringRenderer;
        [SerializeField] private float pulseScale = 1.12f;
        [SerializeField] private float pulseDuration = 0.5f;

        private Tween scaleTween;
        private Vector3 baseScale;

        public void Play(Vector3 worldPosition, float tileWidth, float tileHeight)
        {
            transform.position = worldPosition;

            // Scale so the sprite exactly covers the tile
            if (ringRenderer != null && ringRenderer.sprite != null)
            {
                Vector2 spriteSize = ringRenderer.sprite.bounds.size;
                float sx = spriteSize.x > 0 ? tileWidth  / spriteSize.x : 1f;
                float sy = spriteSize.y > 0 ? tileHeight / spriteSize.y : 1f;
                baseScale = new Vector3(sx, sy, 1f);
            }
            else
            {
                baseScale = new Vector3(tileWidth, tileHeight, 1f);
            }

            transform.localScale = baseScale;
            gameObject.SetActive(true);

            if (ringRenderer != null)
            {
                Color c = ringRenderer.color;
                c.a = 0f;
                ringRenderer.color = c;
                ringRenderer.DOFade(0.8f, 0.15f).SetLink(gameObject);
            }

            scaleTween = transform.DOScale(baseScale * pulseScale, pulseDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetLink(gameObject);
        }

        public void Stop()
        {
            scaleTween?.Kill();
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            scaleTween?.Kill();
        }
    }
}
