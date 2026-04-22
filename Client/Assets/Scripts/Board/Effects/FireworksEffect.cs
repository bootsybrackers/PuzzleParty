using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace PuzzleParty.Board.Effects
{
    /// <summary>
    /// Fireworks burst effect played on a UI canvas.
    /// Instantiate from the Resources prefab and call Play(canvasTransform).
    /// </summary>
    public class FireworksEffect : MonoBehaviour
    {
        [Header("Volley")]
        [SerializeField] private int shellsPerVolley = 5;
        [SerializeField] private float volleyInterval = 0.9f;
        [SerializeField] private float shellRiseTime = 0.55f;
        [SerializeField] private float shellRiseMinFraction = 0.30f;
        [SerializeField] private float shellRiseMaxFraction = 0.72f;
        [SerializeField] private float shellXMarginFraction = 0.12f;

        [Header("Burst")]
        [SerializeField] private int burstRingCount = 20;
        [SerializeField] private int burstTrailCount = 7;
        [SerializeField] private float burstRadius = 130f;
        [SerializeField] private float burstDuration = 1.0f;
        [SerializeField] private float particleSize = 16f;

        [Header("Colors")]
        [SerializeField] private Color[] paletteColors = new Color[]
        {
            new Color(1.0f, 0.90f, 0.20f),  // gold
            new Color(1.0f, 0.30f, 0.30f),  // red
            new Color(0.30f, 1.0f, 0.50f),  // green
            new Color(0.40f, 0.75f, 1.0f),  // blue
            new Color(1.0f, 0.45f, 1.0f),   // magenta
            new Color(1.0f, 0.60f, 0.20f),  // orange
            new Color(0.60f, 0.90f, 1.0f),  // cyan
        };

        private Coroutine launchCoroutine;
        private readonly List<GameObject> activeParticles = new List<GameObject>();
        private Transform canvasParent;
        private Sprite circleSprite;

        // ─── Public API ───────────────────────────────────────────────────────

        public void Play(Transform canvasParent)
        {
            this.canvasParent = canvasParent;
            circleSprite = CreateCircleSprite();
            launchCoroutine = StartCoroutine(LaunchLoop());
        }

        public void Stop()
        {
            if (launchCoroutine != null)
            {
                StopCoroutine(launchCoroutine);
                launchCoroutine = null;
            }

            foreach (var p in activeParticles)
                if (p != null) Destroy(p);
            activeParticles.Clear();
        }

        private void OnDestroy() => Stop();

        // ─── Coroutines ───────────────────────────────────────────────────────

        private IEnumerator LaunchLoop()
        {
            // Immediate first volley
            FireVolley();

            while (true)
            {
                yield return new WaitForSeconds(volleyInterval);
                FireVolley();
            }
        }

        private void FireVolley()
        {
            float sw = Screen.width;
            float sh = Screen.height;
            float xMin = sw * shellXMarginFraction;
            float xMax = sw * (1f - shellXMarginFraction);

            for (int i = 0; i < shellsPerVolley; i++)
            {
                float spawnX = Random.Range(xMin, xMax);
                float apexX  = spawnX + Random.Range(-60f, 60f);
                float apexY  = sh * Random.Range(shellRiseMinFraction, shellRiseMaxFraction);
                Color col    = paletteColors[Random.Range(0, paletteColors.Length)];
                float delay  = i * 0.18f;

                StartCoroutine(FireShell(
                    new Vector3(spawnX, -40f, 0f),
                    new Vector3(apexX,  apexY, 0f),
                    col, delay));
            }
        }

        private IEnumerator FireShell(Vector3 start, Vector3 apex, Color color, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);

            // Tiny rising dot
            GameObject shell = SpawnDot(6f, color, start);
            bool burst = false;

            shell.GetComponent<RectTransform>()
                .DOMove(apex, shellRiseTime)
                .SetEase(Ease.OutQuad)
                .SetLink(shell)
                .OnComplete(() =>
                {
                    burst = true;
                    if (shell != null)
                    {
                        activeParticles.Remove(shell);
                        Destroy(shell);
                        Burst(apex, color);
                    }
                });

            yield return new WaitForSeconds(shellRiseTime + 0.05f);

            if (!burst && shell != null)
            {
                activeParticles.Remove(shell);
                Destroy(shell);
            }
        }

        // ─── Burst ────────────────────────────────────────────────────────────

        private void Burst(Vector3 center, Color baseColor)
        {
            // Central flash
            GameObject flash = SpawnDot(burstRadius * 0.6f, baseColor, center);
            var flashRt  = flash.GetComponent<RectTransform>();
            var flashImg = flash.GetComponent<Image>();
            flashImg.DOFade(0f, burstDuration * 0.25f).SetEase(Ease.OutQuad).SetLink(flash);
            flashRt.DOScale(2.2f, burstDuration * 0.25f).SetEase(Ease.OutQuad).SetLink(flash)
                .OnComplete(() => { if (flash != null) { activeParticles.Remove(flash); Destroy(flash); } });

            // Ring burst
            for (int i = 0; i < burstRingCount; i++)
            {
                float angle = (360f / burstRingCount) * i + Random.Range(-8f, 8f);
                float rad   = angle * Mathf.Deg2Rad;
                float dist  = burstRadius * Random.Range(0.75f, 1.30f);
                Vector3 target = center + new Vector3(Mathf.Cos(rad) * dist, Mathf.Sin(rad) * dist, 0f);

                Color pCol = ShiftColor(baseColor, Random.Range(-0.05f, 0.05f));
                float pSize = particleSize * Random.Range(0.7f, 1.4f);
                GameObject p = SpawnDot(pSize, pCol, center);
                var rt  = p.GetComponent<RectTransform>();
                var img = p.GetComponent<Image>();

                rt.DOMove(target, burstDuration).SetEase(Ease.OutQuad).SetLink(p)
                    .OnComplete(() => { if (p != null) { activeParticles.Remove(p); Destroy(p); } });
                img.DOFade(0f, burstDuration * 0.45f).SetDelay(burstDuration * 0.55f).SetLink(p);
                rt.DOScale(0f, burstDuration * 0.35f).SetDelay(burstDuration * 0.65f).SetLink(p);
            }

            // Falling sparkle trails
            for (int i = 0; i < burstTrailCount; i++)
            {
                float angle = Random.Range(210f, 330f) * Mathf.Deg2Rad;
                float initDist = Random.Range(20f, burstRadius * 0.6f);
                Vector3 sp = center + new Vector3(Mathf.Cos(angle) * initDist, Mathf.Sin(angle) * initDist, 0f);
                Vector3 ep = sp + new Vector3(Random.Range(-25f, 25f), -Random.Range(90f, 190f), 0f);

                Color tCol = ShiftColor(baseColor, Random.Range(-0.04f, 0.04f));
                GameObject t = SpawnDot(particleSize * 0.55f, tCol, sp);
                var rt  = t.GetComponent<RectTransform>();
                var img = t.GetComponent<Image>();
                float dur   = Random.Range(0.45f, 0.95f);
                float dly   = Random.Range(0.08f, 0.35f);

                rt.DOMove(ep, dur).SetEase(Ease.InQuad).SetDelay(dly).SetLink(t)
                    .OnComplete(() => { if (t != null) { activeParticles.Remove(t); Destroy(t); } });
                img.DOFade(0f, dur * 0.4f).SetDelay(dly + dur * 0.6f).SetLink(t);
            }
        }

        // ─── Helpers ──────────────────────────────────────────────────────────

        private GameObject SpawnDot(float size, Color color, Vector3 worldPos)
        {
            var go  = new GameObject("FW");
            go.transform.SetParent(canvasParent, false);
            var img = go.AddComponent<Image>();
            img.sprite = circleSprite;
            img.color  = color;
            img.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size, size);
            rt.position  = worldPos;
            activeParticles.Add(go);
            return go;
        }

        private static Color ShiftColor(Color col, float hueShift)
        {
            Color.RGBToHSV(col, out float h, out float s, out float v);
            return Color.HSVToRGB((h + hueShift + 1f) % 1f, s, v);
        }

        private static Sprite CreateCircleSprite()
        {
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float c = size * 0.5f - 0.5f;
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                    // Soft glow: bright core + wider glow
                    float core = Mathf.Clamp01(1f - d / (r * 0.45f));
                    float glow = Mathf.Pow(Mathf.Clamp01(1f - d / r), 2f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(core + glow * 0.6f)));
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
