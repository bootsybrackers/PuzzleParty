using UnityEngine;
using DG.Tweening;

namespace PuzzleParty.Board.Effects
{
    /// <summary>
    /// Glowing frame around the board, used when the tile-swap power-up is active.
    /// Layers a bright crisp border with a soft radial-glow halo running along each edge and
    /// accent blobs at the corners, so the board reads as genuinely glowing rather than just
    /// having a thin pulsing outline.
    /// </summary>
    public class BoardShineEffect : MonoBehaviour
    {
        [Header("Core border (crisp line)")]
        [SerializeField] private Sprite borderSprite;
        [SerializeField] private Color borderColor = new Color(1f, 0.85f, 0.2f, 1f);
        [SerializeField] private float borderThickness = 0.2f;
        [SerializeField] private float corePulseMin = 0.55f;
        [SerializeField] private float corePulseMax = 1f;

        [Header("Soft outer halo")]
        [SerializeField] private Color glowColor = new Color(1f, 0.95f, 0.55f, 1f);
        [SerializeField] private float glowBlobSize = 1.1f;
        [SerializeField] private int glowBlobsPerLongSide = 7;
        [SerializeField] private int glowBlobsPerShortSide = 5;
        [SerializeField] private float glowPulseMin = 0.15f;
        [SerializeField] private float glowPulseMax = 0.5f;

        [Header("Corner accents")]
        [SerializeField] private float cornerBlobSize = 1.6f;

        [Header("Timing")]
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float fadeOutDuration = 0.25f;
        [SerializeField] private float pulseDuration = 0.75f;
        [SerializeField] private float colorPulseDuration = 0.9f;

        private const float BOARD_W = 4f;
        private const float BOARD_H = 6f;
        private const string GLOW_SPRITE_PATH = "Images/Effects/sparkle_glow";

        private SpriteRenderer[] coreBorders;
        private SpriteRenderer[] glowBlobs;
        private SpriteRenderer[] cornerBlobs;
        private Tween[] allTweens;

        private void Awake()
        {
            Sprite glowSprite = Resources.Load<Sprite>(GLOW_SPRITE_PATH);
            if (glowSprite == null) glowSprite = CreateFallbackGlowSprite();

            BuildCoreBorder();
            BuildEdgeGlow(glowSprite);
            BuildCornerAccents(glowSprite);
        }

        private void BuildCoreBorder()
        {
            float half = borderThickness * 0.5f;
            float hw = BOARD_W * 0.5f;
            float hh = BOARD_H * 0.5f;

            // (localPos, scaleXY) for top, bottom, left, right
            (Vector3 pos, Vector3 scale)[] layout = {
                (new Vector3(0f,  hh + half, 0f), new Vector3(BOARD_W + borderThickness * 2f, borderThickness, 1f)),
                (new Vector3(0f, -hh - half, 0f), new Vector3(BOARD_W + borderThickness * 2f, borderThickness, 1f)),
                (new Vector3(-hw - half, 0f, 0f), new Vector3(borderThickness, BOARD_H, 1f)),
                (new Vector3( hw + half, 0f, 0f), new Vector3(borderThickness, BOARD_H, 1f)),
            };

            coreBorders = new SpriteRenderer[4];
            for (int i = 0; i < 4; i++)
            {
                var go = new GameObject($"CoreBorder_{i}");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = layout[i].pos;
                go.transform.localScale    = layout[i].scale;

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite       = borderSprite;
                sr.color        = new Color(borderColor.r, borderColor.g, borderColor.b, 0f);
                sr.sortingOrder = 16;
                coreBorders[i] = sr;
            }
        }

        private void BuildEdgeGlow(Sprite glowSprite)
        {
            float hw = BOARD_W * 0.5f;
            float hh = BOARD_H * 0.5f;

            var blobs = new System.Collections.Generic.List<SpriteRenderer>();

            blobs.AddRange(CreateBlobRow(glowSprite, new Vector3(-hw, hh, 0f), new Vector3(hw, hh, 0f), glowBlobsPerLongSide));
            blobs.AddRange(CreateBlobRow(glowSprite, new Vector3(-hw, -hh, 0f), new Vector3(hw, -hh, 0f), glowBlobsPerLongSide));
            blobs.AddRange(CreateBlobRow(glowSprite, new Vector3(-hw, -hh, 0f), new Vector3(-hw, hh, 0f), glowBlobsPerShortSide));
            blobs.AddRange(CreateBlobRow(glowSprite, new Vector3(hw, -hh, 0f), new Vector3(hw, hh, 0f), glowBlobsPerShortSide));

            glowBlobs = blobs.ToArray();
        }

        private System.Collections.Generic.List<SpriteRenderer> CreateBlobRow(Sprite sprite, Vector3 from, Vector3 to, int count)
        {
            var result = new System.Collections.Generic.List<SpriteRenderer>();
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0.5f : i / (float)(count - 1);
                Vector3 pos = Vector3.Lerp(from, to, t);

                var go = new GameObject($"EdgeGlow_{transform.childCount}");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = pos;
                go.transform.localScale = Vector3.one * glowBlobSize;

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0f);
                sr.sortingOrder = 14;
                result.Add(sr);
            }
            return result;
        }

        private void BuildCornerAccents(Sprite glowSprite)
        {
            float hw = BOARD_W * 0.5f;
            float hh = BOARD_H * 0.5f;

            Vector3[] corners = {
                new Vector3(-hw, hh, 0f), new Vector3(hw, hh, 0f),
                new Vector3(-hw, -hh, 0f), new Vector3(hw, -hh, 0f),
            };

            cornerBlobs = new SpriteRenderer[corners.Length];
            for (int i = 0; i < corners.Length; i++)
            {
                var go = new GameObject($"CornerGlow_{i}");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = corners[i];
                go.transform.localScale = Vector3.one * cornerBlobSize;

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = glowSprite;
                sr.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0f);
                sr.sortingOrder = 17;
                cornerBlobs[i] = sr;
            }
        }

        public void Play()
        {
            gameObject.SetActive(true);

            var tweens = new System.Collections.Generic.List<Tween>();

            foreach (var sr in coreBorders)
                tweens.Add(FadeThenPulse(sr, corePulseMax, corePulseMin, pulseDuration));

            foreach (var sr in glowBlobs)
                tweens.Add(FadeThenPulse(sr, glowPulseMax, glowPulseMin, pulseDuration * 1.2f));

            foreach (var sr in cornerBlobs)
                tweens.Add(FadeThenPulse(sr, glowPulseMax * 1.1f, glowPulseMin * 0.6f, pulseDuration * 0.9f));

            // Gentle warm <-> bright gold color life on the core border, echoing the gold
            // "power-up" language used elsewhere (victory text, map completion, etc). Alpha
            // keeps being driven separately by the fade/pulse tweens above, so only the RGB
            // is touched here.
            Color bright = new Color(1f, 0.98f, 0.75f, 1f);
            foreach (var sr in coreBorders)
            {
                var colorTween = DOTween.To(() => sr.color, c => { float a = sr.color.a; sr.color = new Color(c.r, c.g, c.b, a); }, bright, colorPulseDuration)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine)
                    .SetLink(gameObject);
                tweens.Add(colorTween);
            }

            allTweens = tweens.ToArray();
        }

        private Tween FadeThenPulse(SpriteRenderer sr, float max, float min, float duration)
        {
            return sr.DOFade(max, fadeInDuration)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    sr.DOFade(min, duration)
                        .SetLoops(-1, LoopType.Yoyo)
                        .SetEase(Ease.InOutSine)
                        .SetLink(gameObject);
                });
        }

        public void Stop(System.Action onComplete = null)
        {
            KillTweens();

            var allRenderers = new System.Collections.Generic.List<SpriteRenderer>();
            allRenderers.AddRange(coreBorders);
            allRenderers.AddRange(glowBlobs);
            allRenderers.AddRange(cornerBlobs);

            int remaining = allRenderers.Count;
            if (remaining == 0)
            {
                gameObject.SetActive(false);
                onComplete?.Invoke();
                return;
            }

            foreach (var sr in allRenderers)
            {
                sr.DOFade(0f, fadeOutDuration)
                    .SetLink(gameObject)
                    .OnComplete(() =>
                    {
                        remaining--;
                        if (remaining == 0)
                        {
                            gameObject.SetActive(false);
                            onComplete?.Invoke();
                        }
                    });
            }
        }

        private void KillTweens()
        {
            if (allTweens == null) return;
            foreach (var t in allTweens) t?.Kill();
        }

        private void OnDestroy()
        {
            KillTweens();
        }

        private static Sprite CreateFallbackGlowSprite()
        {
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float c = size * 0.5f - 0.5f;
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                    float alpha = Mathf.Clamp01(1f - Mathf.Pow(d / r, 2f));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
