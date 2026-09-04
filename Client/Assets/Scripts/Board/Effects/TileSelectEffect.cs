using UnityEngine;
using DG.Tweening;

namespace PuzzleParty.Board.Effects
{
    /// <summary>
    /// Shown on the first tile picked for the swap power-up: a soft ambient glow behind the
    /// tile plus a crisp 4-sided outline hugging its edges, with a punch-in on selection, so
    /// it's unmistakable exactly which tile is currently selected.
    /// </summary>
    public class TileSelectEffect : MonoBehaviour
    {
        [Header("Ambient glow (existing blob behind the tile)")]
        [SerializeField] private SpriteRenderer ringRenderer;
        [SerializeField] private float pulseScale = 1.12f;
        [SerializeField] private float pulseDuration = 0.5f;

        [Header("Edge outline")]
        [SerializeField] private Sprite borderSprite;
        [SerializeField] private Color borderColor = new Color(1f, 0.95f, 0.35f, 1f);
        [SerializeField] private float borderThicknessFraction = 0.09f; // relative to tile size
        [SerializeField] private float borderPulseMin = 0.55f;
        [SerializeField] private float borderPulseMax = 1f;

        [Header("Selection punch")]
        [SerializeField] private float punchScale = 0.35f;
        [SerializeField] private float punchDuration = 0.35f;

        private Tween scaleTween;
        private Tween[] borderPulseTweens;
        private SpriteRenderer[] borders;
        private Vector3 baseScale;
        private Transform borderRoot;

        public void Play(Vector3 worldPosition, float tileWidth, float tileHeight)
        {
            transform.position = worldPosition;

            // Ambient glow blob sizing (existing behavior)
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
                ringRenderer.DOFade(0.6f, 0.15f).SetLink(gameObject);
            }

            scaleTween = transform.DOScale(baseScale * pulseScale, pulseDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetLink(gameObject);

            BuildBorder(tileWidth, tileHeight);
            PlayBorder();

            // Punch the whole selection effect on entry to really announce it, then settle
            // into the steady pulse started above.
            transform.DOPunchScale(baseScale * punchScale, punchDuration, 6, 0.9f)
                .SetLink(gameObject);
        }

        private void BuildBorder(float tileWidth, float tileHeight)
        {
            Sprite sprite = borderSprite != null ? borderSprite : CreateFallbackBorderSprite();
            float thickness = Mathf.Min(tileWidth, tileHeight) * borderThicknessFraction;
            float half = thickness * 0.5f;
            float hw = tileWidth * 0.5f;
            float hh = tileHeight * 0.5f;

            // Built as a sibling (not a child of this transform) since this GameObject's own
            // transform gets scaled to cover the tile for the ambient glow blob - a child here
            // would inherit that scale and distort the outline's thickness/size.
            var rootGO = new GameObject("SelectBorder");
            rootGO.transform.SetParent(transform.parent, false);
            rootGO.transform.position = transform.position;
            borderRoot = rootGO.transform;

            (Vector3 pos, Vector3 scale)[] layout = {
                (new Vector3(0f,  hh + half, 0f), new Vector3(tileWidth + thickness * 2f, thickness, 1f)),
                (new Vector3(0f, -hh - half, 0f), new Vector3(tileWidth + thickness * 2f, thickness, 1f)),
                (new Vector3(-hw - half, 0f, 0f), new Vector3(thickness, tileHeight, 1f)),
                (new Vector3( hw + half, 0f, 0f), new Vector3(thickness, tileHeight, 1f)),
            };

            borders = new SpriteRenderer[4];
            borderPulseTweens = new Tween[4];

            for (int i = 0; i < 4; i++)
            {
                var go = new GameObject($"Side_{i}");
                go.transform.SetParent(borderRoot, false);
                go.transform.localPosition = layout[i].pos;
                go.transform.localScale = layout[i].scale;

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = new Color(borderColor.r, borderColor.g, borderColor.b, 0f);
                sr.sortingOrder = 20;
                borders[i] = sr;
            }
        }

        private void PlayBorder()
        {
            for (int i = 0; i < borders.Length; i++)
            {
                int idx = i;
                borders[i].DOFade(borderPulseMax, 0.15f).SetLink(borderRoot.gameObject)
                    .OnComplete(() =>
                    {
                        borderPulseTweens[idx] = borders[idx].DOFade(borderPulseMin, pulseDuration)
                            .SetLoops(-1, LoopType.Yoyo)
                            .SetEase(Ease.InOutSine)
                            .SetLink(borderRoot.gameObject);
                    });
            }
        }

        public void Stop()
        {
            scaleTween?.Kill();
            KillBorderTweens();
            if (borderRoot != null) Destroy(borderRoot.gameObject);
            Destroy(gameObject);
        }

        private void KillBorderTweens()
        {
            if (borderPulseTweens == null) return;
            foreach (var t in borderPulseTweens) t?.Kill();
        }

        private void OnDestroy()
        {
            scaleTween?.Kill();
            KillBorderTweens();
        }

        private static Sprite CreateFallbackBorderSprite()
        {
            const int size = 4;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
