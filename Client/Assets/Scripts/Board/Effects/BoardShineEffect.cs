using UnityEngine;
using DG.Tweening;

namespace PuzzleParty.Board.Effects
{
    /// <summary>
    /// Glowing frame around the board, used when the tile-swap power-up is active.
    /// Creates four border strips dynamically. Assign a solid white sprite to borderSprite.
    /// </summary>
    public class BoardShineEffect : MonoBehaviour
    {
        [SerializeField] private Sprite borderSprite;
        [SerializeField] private Color borderColor = new Color(1f, 0.85f, 0.2f, 1f);
        [SerializeField] private float borderThickness = 0.12f;
        [SerializeField] private float fadeInDuration = 0.25f;
        [SerializeField] private float fadeOutDuration = 0.2f;
        [SerializeField] private float pulseMin = 0.35f;
        [SerializeField] private float pulseMax = 0.85f;
        [SerializeField] private float pulseDuration = 0.65f;

        private const float BOARD_W = 4f;
        private const float BOARD_H = 6f;

        private SpriteRenderer[] borders;
        private Tween[] pulseTweens;

        private void Awake()
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

            borders = new SpriteRenderer[4];
            pulseTweens = new Tween[4];

            for (int i = 0; i < 4; i++)
            {
                var go = new GameObject($"Border_{i}");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = layout[i].pos;
                go.transform.localScale    = layout[i].scale;

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite       = borderSprite;
                sr.color        = new Color(borderColor.r, borderColor.g, borderColor.b, 0f);
                sr.sortingOrder = 15;
                borders[i] = sr;
            }
        }

        public void Play()
        {
            gameObject.SetActive(true);
            for (int i = 0; i < borders.Length; i++)
            {
                int idx = i;
                borders[i].DOFade(pulseMax, fadeInDuration)
                    .SetLink(gameObject)
                    .OnComplete(() =>
                    {
                        pulseTweens[idx] = borders[idx].DOFade(pulseMin, pulseDuration)
                            .SetLoops(-1, LoopType.Yoyo)
                            .SetEase(Ease.InOutSine)
                            .SetLink(gameObject);
                    });
            }
        }

        public void Stop(System.Action onComplete = null)
        {
            for (int i = 0; i < pulseTweens.Length; i++)
                pulseTweens[i]?.Kill();

            int remaining = borders.Length;
            for (int i = 0; i < borders.Length; i++)
            {
                borders[i].DOFade(0f, fadeOutDuration)
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

        private void OnDestroy()
        {
            if (pulseTweens != null)
                foreach (var t in pulseTweens) t?.Kill();
        }
    }
}
