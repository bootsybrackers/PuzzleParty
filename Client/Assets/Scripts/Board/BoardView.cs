using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using PuzzleParty.Levels;

namespace PuzzleParty.Board
{

    public class BoardView : MonoBehaviour
    {
        private Dictionary<string, GameObject> tileObjects = new Dictionary<string, GameObject>();
        private Level level;
        private LevelUIElements uiElements;
        private int currentStreak; // Current streak count for powerup display

        // Sparkle trail settings (tweakable in Inspector)
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

        // Dynamic tile sizing
        private int tilePixelWidth;
        private int tilePixelHeight;
        private const int TEXTURE_WIDTH = 768;
        private const int TEXTURE_HEIGHT = 1344;

        // Desired world-space size for tiles (calculated dynamically)
        private float desiredTileWorldWidth;
        private float desiredTileWorldHeight;

        public void Start()
        {
            Debug.Log("Started!");
            //Setup(null);
        }

        public void Update()
        {

        }

        public void Setup(Level level, BoardTile[][] initialBoard, LevelUIElements uiElements, int currentStreak)
        {
            this.level = level;
            this.uiElements = uiElements;
            this.currentStreak = currentStreak;

            int cols = level.Columns;
            int rows = level.Rows;

            // Calculate dynamic tile size based on board dimensions
            // The texture is always 768x1344, divide it by the grid size
            tilePixelWidth = TEXTURE_WIDTH / cols;
            tilePixelHeight = TEXTURE_HEIGHT / rows;

            // Calculate tile world size to keep board dimensions constant
            // For 4x7: width=4.0/4=1.0, height=7.0/7=1.0 (square tiles)
            // For 3x5: width=4.0/3=1.33, height=7.0/5=1.4 (slightly rectangular)
            const float DESIRED_BOARD_WIDTH = 4.0f;
            const float DESIRED_BOARD_HEIGHT = 7.0f;

            desiredTileWorldWidth = DESIRED_BOARD_WIDTH / cols;
            desiredTileWorldHeight = DESIRED_BOARD_HEIGHT / rows;

            Debug.Log($"Board setup: {cols}x{rows}, tile pixel size: {tilePixelWidth}x{tilePixelHeight}px, tile world size: {desiredTileWorldWidth}x{desiredTileWorldHeight}");

            Texture2D tex = level.LevelSprite.texture;
            Texture2D newTex = ResizeTexture(tex, TEXTURE_WIDTH, TEXTURE_HEIGHT);

            // Create tiles in their initial (correct) positions
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    BoardTile tile = initialBoard[i][j];
                    if (tile != null)
                    {
                        // For initial board: tile is at its solved position
                        // Texture slice: based on tile's identity (which piece of puzzle)
                        // Display position: based on where it is in the board array [i][j]
                        int displayCol = j + 1;  // Convert 0-indexed to 1-indexed
                        int displayRow = i + 1;  // Convert 0-indexed to 1-indexed

                        GameObject tileObj = CreateTile2(newTex, tile.Column + 1, tile.Row + 1, displayCol, displayRow, tilePixelWidth, tilePixelHeight);
                        string key = GetTileKey(tile.Row, tile.Column);
                        tileObjects[key] = tileObj;

                        // Don't add chains during setup - we'll add them after scramble animation
                    }
                }
            }
            Debug.Log($"Setup complete. Total tiles created: {tileObjects.Count}");
        }

        public void StartAnimation(System.Action onComplete)
        {
            StartCoroutine(ShowCompletePuzzle(onComplete));
        }

        private IEnumerator ShowCompletePuzzle(System.Action onComplete)
        {
            // Board stays at full scale from the start - no scaling animation

            // Prepare banner and wait one frame to ensure changes are applied
            if (uiElements?.levelBanner != null)
            {
                // Set level name on banner
                if (uiElements?.levelBannerText != null)
                {
                    uiElements.levelBannerText.text = $"{level.Name}";
                }

                // Set initial alpha to 0 for fade-in effect
                CanvasGroup canvasGroup = uiElements.levelBanner.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                }

                // Get RectTransform and store original position
                RectTransform rectTransform = uiElements.levelBanner.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    Vector2 originalAnchoredPos = rectTransform.anchoredPosition;

                    // Position banner off-screen to the left (1200 pixels to ensure fully off-screen)
                    Vector2 bannerStartAnchoredPos = originalAnchoredPos;
                    bannerStartAnchoredPos.x -= 1200f;
                    rectTransform.anchoredPosition = bannerStartAnchoredPos;

                    // Wait one frame for changes to take effect
                    yield return null;
                }
            }

            // Setup streak icons immediately (visible when banner slides in)
            if (uiElements?.streakIcons != null && uiElements.streakIcons.Length == 3)
            {
                Sprite lockSprite = Resources.Load<Sprite>("Images/Icon_ImageIcon_lock_00");

                for (int i = 0; i < 3; i++)
                {
                    GameObject icon = uiElements.streakIcons[i];
                    if (icon != null)
                    {
                        // Show icon at full scale
                        icon.transform.localScale = Vector3.one;

                        // Tint icon based on active/passive state
                        Image img = icon.GetComponent<Image>();
                        if (img != null)
                        {
                            img.color = (i < currentStreak) ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);
                        }

                        // Ensure CanvasGroup is fully visible
                        CanvasGroup iconCanvasGroup = icon.GetComponent<CanvasGroup>();
                        if (iconCanvasGroup == null)
                        {
                            iconCanvasGroup = icon.AddComponent<CanvasGroup>();
                        }
                        iconCanvasGroup.alpha = 1f;

                        // Remove any existing lock overlay
                        Transform existingLock = icon.transform.Find("LockOverlay");
                        if (existingLock != null)
                        {
                            Destroy(existingLock.gameObject);
                        }

                        // Add lock overlay on inactive icons
                        if (i >= currentStreak && lockSprite != null)
                        {
                            GameObject lockObj = new GameObject("LockOverlay");
                            lockObj.transform.SetParent(icon.transform, false);

                            Image lockImg = lockObj.AddComponent<Image>();
                            lockImg.sprite = lockSprite;
                            lockImg.raycastTarget = false;

                            RectTransform lockRect = lockObj.GetComponent<RectTransform>();
                            lockRect.anchorMin = new Vector2(1f, 0f);
                            lockRect.anchorMax = new Vector2(1f, 0f);
                            lockRect.pivot = new Vector2(1f, 0f);
                            lockRect.anchoredPosition = Vector2.zero;
                            lockRect.sizeDelta = new Vector2(40f, 40f);
                        }
                    }
                }
            }

            // Step 1: Slide in banner from left
            if (uiElements?.levelBanner != null)
            {
                RectTransform rectTransform = uiElements.levelBanner.GetComponent<RectTransform>();
                CanvasGroup canvasGroup = uiElements.levelBanner.GetComponent<CanvasGroup>();

                // Ensure banner is fully visible (no fade)
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                }

                if (rectTransform != null)
                {
                    // Calculate the target position (slide back to original position)
                    Vector2 targetAnchoredPos = rectTransform.anchoredPosition;
                    targetAnchoredPos.x += 1200f; // Move from left to original position

                    // Slide in the banner with smooth easing
                    rectTransform.DOAnchorPos(targetAnchoredPos, 0.6f).SetEase(Ease.OutBack);
                }
            }

            // Wait for banner slide-in to complete
            yield return new WaitForSeconds(0.6f);

            // Keep banner visible for a moment
            yield return new WaitForSeconds(0.5f);

            // Animate powerup unlocks (icon to button/text effects)
            if (currentStreak >= 1 && uiElements.streakIcons != null && uiElements.streakIcons.Length > 0)
            {
                // Painting unlock animation (streak >= 1)
                if (uiElements.completePuzzleButton != null)
                {
                    yield return StartCoroutine(AnimateIconToTarget(uiElements.streakIcons[0], uiElements.completePuzzleButton.transform));
                }
            }

            if (currentStreak >= 3 && uiElements.streakIcons != null && uiElements.streakIcons.Length > 2)
            {
                // Slot unlock animation (streak >= 3)
                if (uiElements.slotButton != null)
                {
                    yield return StartCoroutine(AnimateIconToTarget(uiElements.streakIcons[2], uiElements.slotButton.transform));
                }
            }

            if (currentStreak >= 2 && uiElements.streakIcons != null && uiElements.streakIcons.Length > 1)
            {
                // Moves bonus animation (streak >= 2)
                if (uiElements.movesText != null)
                {
                    yield return StartCoroutine(AnimateIconToTarget(uiElements.streakIcons[1], uiElements.movesText.transform));
                }
            }

            // Step 2: Slide out banner to the left
            if (uiElements?.levelBanner != null)
            {
                RectTransform rectTransform = uiElements.levelBanner.GetComponent<RectTransform>();

                if (rectTransform != null)
                {
                    Vector2 exitPos = rectTransform.anchoredPosition;
                    exitPos.x -= 1200f; // Slide back out to the left (fully off-screen)

                    rectTransform.DOAnchorPos(exitPos, 0.5f).SetEase(Ease.InBack);
                }
            }

            // Wait for banner exit animation to complete
            yield return new WaitForSeconds(0.5f);

            // Brief pause before scrambling
            yield return new WaitForSeconds(0.3f);

            // Signal that we're ready to scramble
            onComplete?.Invoke();
        }

        // Cached sparkle texture (procedurally generated once)
        private static Texture2D _sparkleTexture;
        private static Sprite _sparkleSprite;

        private static Sprite GetSparkleSprite()
        {
            if (_sparkleSprite != null) return _sparkleSprite;

            int size = 32;
            _sparkleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = size / 2f;
            float radius = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float norm = Mathf.Clamp01(dist / radius);
                    // Sharp bright core + soft outer glow
                    float core = Mathf.Clamp01(1f - norm * 3f); // Bright center within ~33% radius
                    float glow = (1f - norm) * (1f - norm); // Soft quadratic falloff
                    float alpha = Mathf.Clamp01(core + glow);
                    _sparkleTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            _sparkleTexture.Apply();

            _sparkleSprite = Sprite.Create(
                _sparkleTexture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                100f
            );

            return _sparkleSprite;
        }

        private IEnumerator AnimateIconToTarget(GameObject sourceIcon, Transform target)
        {
            if (sourceIcon == null || target == null)
            {
                yield break;
            }

            // Get the canvas to parent sparkles to
            Canvas canvas = uiElements.levelBanner.GetComponentInParent<Canvas>();
            Transform sparkleParent = canvas != null ? canvas.transform : uiElements.levelBanner.transform.parent;

            // Get source and target positions
            RectTransform sourceRect = sourceIcon.GetComponent<RectTransform>();
            Vector3 sourcePos = sourceRect != null ? sourceRect.position : sourceIcon.transform.position;

            // Convert target position to the sparkle canvas coordinate space
            // The target may be on a different canvas (Screen Space - Camera) than the sparkles (Screen Space - Overlay)
            Vector3 targetPos;
            Canvas targetCanvas = target.GetComponentInParent<Canvas>();
            if (targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                // Target is on a Camera canvas, sparkles are on Overlay — convert via screen point
                Camera targetCam = targetCanvas.worldCamera ?? Camera.main;
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(targetCam, target.position);
                targetPos = new Vector3(screenPoint.x, screenPoint.y, 0f);
            }
            else
            {
                targetPos = target.position;
            }

            Sprite sparkleSprite = GetSparkleSprite();
            List<GameObject> sparkles = new List<GameObject>();
            float totalDuration = sparkleDuration;
            float staggerInterval = sparkleStaggerTime / sparkleCount;

            // Spawn sparkles with staggered start
            for (int i = 0; i < sparkleCount; i++)
            {
                GameObject sparkle = new GameObject($"Sparkle_{i}");
                Image sparkleImg = sparkle.AddComponent<Image>();
                sparkleImg.sprite = sparkleSprite;
                sparkleImg.raycastTarget = false;

                RectTransform sparkleRect = sparkle.GetComponent<RectTransform>();
                sparkleRect.SetParent(sparkleParent, false);
                sparkleRect.position = sourcePos;

                // Size gradient: first sparkles (head) are larger, trailing ones shrink
                float t01 = (float)i / Mathf.Max(1, sparkleCount - 1);
                float sizeMultiplier = Mathf.Lerp(headToTailRatio, 1f, t01);
                float baseSize = Random.Range(sparkleSize.x, sparkleSize.y) * sizeMultiplier;
                sparkleRect.sizeDelta = new Vector2(baseSize, baseSize);

                // Random initial rotation
                sparkle.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

                // Color: head is brighter/whiter, tail is more saturated
                float hueShift = Random.Range(sparkleHueMin, sparkleHueMax);
                float sat = Mathf.Lerp(sparkleSaturationMin, sparkleSaturationMax, t01);
                sparkleImg.color = Color.HSVToRGB(hueShift, sat, 1f);

                sparkles.Add(sparkle);

                // Calculate delay and animate
                float delay = i * staggerInterval;
                float sparkleLifetime = totalDuration - delay;
                if (sparkleLifetime < 0.15f) sparkleLifetime = 0.15f;

                // Random offset for curved path (bezier-like wobble)
                float yOffset = Random.Range(-pathCurveStrength, pathCurveStrength);
                Vector3 controlPoint = Vector3.Lerp(sourcePos, targetPos, 0.5f) + new Vector3(Random.Range(-pathXSpread, pathXSpread), yOffset, 0);

                // Animate along quadratic bezier curve using DOVirtual
                Vector3 startPos = sourcePos;
                DOVirtual.Float(0f, 1f, sparkleLifetime, t =>
                {
                    if (sparkleRect != null)
                    {
                        // Quadratic bezier: B(t) = (1-t)^2*P0 + 2*(1-t)*t*P1 + t^2*P2
                        float u = 1f - t;
                        Vector3 pos = u * u * startPos + 2f * u * t * controlPoint + t * t * targetPos;
                        sparkleRect.position = pos;
                    }
                }).SetEase(Ease.InQuad).SetDelay(delay);

                // Fade out in the last portion of lifetime
                sparkleImg.DOFade(0f, sparkleLifetime * (1f - sparkleFadeStart))
                    .SetDelay(delay + sparkleLifetime * sparkleFadeStart);

                // Shrink to zero
                sparkleRect.DOScale(0f, sparkleLifetime * (1f - sparkleShrinkStart))
                    .SetDelay(delay + sparkleLifetime * sparkleShrinkStart);

                // Rotate during flight
                sparkle.transform.DORotate(new Vector3(0, 0, Random.Range(180f, 540f)), sparkleLifetime, RotateMode.FastBeyond360)
                    .SetDelay(delay)
                    .SetEase(Ease.Linear);
            }

            // Wait for all sparkles to arrive
            yield return new WaitForSeconds(totalDuration + 0.05f);

            // Impact burst at target: spawn radial sparkles
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

                // Radial direction
                float angle = (360f / burstParticleCount) * i + Random.Range(-15f, 15f);
                float radians = angle * Mathf.Deg2Rad;
                Vector3 direction = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0);
                float dist = Random.Range(burstDistance.x, burstDistance.y);
                Vector3 burstTarget = targetPos + direction * dist;

                burstRect.DOMove(burstTarget, burstDuration).SetEase(Ease.OutQuad);
                burstImg.DOFade(0f, burstDuration).SetEase(Ease.InQuad);
                burstRect.DOScale(0f, burstDuration).SetEase(Ease.InQuad);
            }

            // Pulse target button
            if (target.GetComponent<RectTransform>() != null)
            {
                target.DOScale(pulseScale, pulseDuration).SetEase(Ease.OutBack)
                    .OnComplete(() => target.DOScale(1f, pulseDuration * 0.67f).SetEase(Ease.InQuad));
            }

            yield return new WaitForSeconds(burstDuration + 0.05f);

            // Cleanup all sparkle objects
            foreach (var s in sparkles)
            {
                if (s != null) Destroy(s);
            }
            foreach (var s in burstSparkles)
            {
                if (s != null) Destroy(s);
            }
        }

        public void AnimateScramble(BoardTile[][] scrambledBoard, System.Action onComplete)
        {
            DG.Tweening.Sequence scrambleSequence = DOTween.Sequence();

            // Track which tiles are still on the board
            HashSet<string> tilesInScrambledBoard = new HashSet<string>();

            // Animate each tile to its scrambled position
            for (int i = 0; i < scrambledBoard.Length; i++)
            {
                for (int j = 0; j < scrambledBoard[i].Length; j++)
                {
                    BoardTile tile = scrambledBoard[i][j];
                    if (tile != null)
                    {
                        string key = GetTileKey(tile.Row, tile.Column);
                        tilesInScrambledBoard.Add(key);

                        if (tileObjects.TryGetValue(key, out GameObject tileObj))
                        {
                            // Calculate target position based on scrambled board position (i, j)
                            Vector3 targetPos = GetLocalPos2(j + 1, i + 1, tilePixelWidth, tilePixelHeight);

                            // Add this tile's movement to the sequence (all tiles move in parallel)
                            scrambleSequence.Join(tileObj.transform.DOLocalMove(targetPos, 1f).SetEase(Ease.InOutQuad));
                        }
                    }
                }
            }

            // Destroy tiles that are now holes (not in scrambled board)
            List<string> tilesToRemove = new List<string>();
            foreach (var kvp in tileObjects)
            {
                if (!tilesInScrambledBoard.Contains(kvp.Key))
                {
                    // Fade out and destroy this tile
                    GameObject tileObj = kvp.Value;
                    SpriteRenderer sr = tileObj.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        scrambleSequence.Join(DOTween.ToAlpha(() => sr.color, x => sr.color = x, 0f, 1f));
                    }
                    tilesToRemove.Add(kvp.Key);
                }
            }

            // Call onComplete when animation finishes and clean up destroyed tiles
            scrambleSequence.OnComplete(() => {
                foreach (string key in tilesToRemove)
                {
                    if (tileObjects.TryGetValue(key, out GameObject tileObj))
                    {
                        Destroy(tileObj);
                        tileObjects.Remove(key);
                    }
                }

                // After scramble completes, add locks with animation
                AnimateLocksAppearing(scrambledBoard, onComplete);
            });
        }

        private void AnimateLocksAppearing(BoardTile[][] scrambledBoard, System.Action onComplete)
        {
            Debug.Log("AnimateLocksAppearing called");

            // Find all locked tiles in the scrambled board
            List<GameObject> lockedTileObjects = new List<GameObject>();

            for (int i = 0; i < scrambledBoard.Length; i++)
            {
                for (int j = 0; j < scrambledBoard[i].Length; j++)
                {
                    BoardTile tile = scrambledBoard[i][j];
                    if (tile != null && tile.IsLocked)
                    {
                        string key = GetTileKey(tile.Row, tile.Column);
                        if (tileObjects.TryGetValue(key, out GameObject tileObj))
                        {
                            lockedTileObjects.Add(tileObj);
                            Debug.Log($"Found locked tile at [{tile.Row},{tile.Column}] for lock animation");
                        }
                    }
                }
            }

            if (lockedTileObjects.Count > 0)
            {
                Debug.Log($"Animating {lockedTileObjects.Count} locks appearing");

                // Add chains to all locked tiles with stagger animation
                DG.Tweening.Sequence lockSequence = DOTween.Sequence();

                for (int i = 0; i < lockedTileObjects.Count; i++)
                {
                    GameObject tileObj = lockedTileObjects[i];

                    // Add chain overlay
                    AddChainOverlay(tileObj);

                    // Find the chain overlay we just added
                    Transform chainOverlay = tileObj.transform.Find("ChainOverlay");
                    if (chainOverlay != null)
                    {
                        // Save the target scale (the correct size calculated by AddChainOverlay)
                        Vector3 targetScale = chainOverlay.localScale;

                        // Start with scale 0
                        chainOverlay.localScale = Vector3.zero;

                        // Animate scale up to the target scale with bounce effect, staggered
                        float delay = i * 0.1f; // 0.1 second stagger between each lock
                        lockSequence.Insert(delay, chainOverlay.DOScale(targetScale, 0.5f).SetEase(Ease.OutBack));

                        // Also start with alpha 0 and fade in
                        SpriteRenderer chainSr = chainOverlay.GetComponent<SpriteRenderer>();
                        if (chainSr != null)
                        {
                            Color color = chainSr.color;
                            color.a = 0f;
                            chainSr.color = color;
                            lockSequence.Insert(delay, chainSr.DOFade(1f, 0.5f));
                        }
                    }
                }

                lockSequence.OnComplete(() => onComplete?.Invoke());
            }
            else
            {
                Debug.Log("No locked tiles found, calling onComplete immediately");
                onComplete?.Invoke();
            }
        }

        private string GetTileKey(int row, int col)
        {
            return $"{row}_{col}";
        }

        public BoardTile GetTileAtPosition(Vector3 worldPosition)
        {
            Debug.Log($"GetTileAtPosition called with worldPosition: {worldPosition}");
            Debug.Log($"Total tiles in dictionary: {tileObjects.Count}");

            // Check each tile GameObject to see if it contains the position (ignore z)
            foreach (var kvp in tileObjects)
            {
                GameObject tileObj = kvp.Value;
                SpriteRenderer sr = tileObj.GetComponent<SpriteRenderer>();

                if (sr != null)
                {
                    // Use the tile's actual z position for the check
                    Vector3 checkPosition = new Vector3(worldPosition.x, worldPosition.y, tileObj.transform.position.z);

                    Debug.Log($"Checking tile {kvp.Key} at position {tileObj.transform.position}, bounds: {sr.bounds}, contains: {sr.bounds.Contains(checkPosition)}");

                    if (sr.bounds.Contains(checkPosition))
                    {
                        // Parse the key to get row and column
                        string[] parts = kvp.Key.Split('_');
                        int row = int.Parse(parts[0]);
                        int col = int.Parse(parts[1]);

                        Debug.Log($"Found tile at row {row}, col {col}");
                        return new BoardTile { Row = row, Column = col };
                    }
                }
            }

            Debug.Log("No tile found at position");
            return null;
        }

        public void UpdateTilePositions(BoardTile[][] currentBoard, System.Action onComplete = null)
        {
            // Animate tiles to their new positions
            bool hasAnimations = false;
            DG.Tweening.Sequence moveSequence = DOTween.Sequence();

            for (int i = 0; i < currentBoard.Length; i++)
            {
                for (int j = 0; j < currentBoard[i].Length; j++)
                {
                    BoardTile tile = currentBoard[i][j];
                    if (tile != null)
                    {
                        string key = GetTileKey(tile.Row, tile.Column);
                        if (tileObjects.TryGetValue(key, out GameObject tileObj))
                        {
                            Vector3 targetPos = GetLocalPos2(j + 1, i + 1, tilePixelWidth, tilePixelHeight);
                            moveSequence.Join(tileObj.transform.DOLocalMove(targetPos, 0.1f).SetEase(Ease.OutQuad));
                            hasAnimations = true;

                            // Update chain overlay based on locked state
                            UpdateChainOverlay(tileObj, tile.IsLocked);
                        }
                    }
                }
            }

            // Call onComplete when animations finish
            if (hasAnimations && onComplete != null)
            {
                moveSequence.OnComplete(() => onComplete?.Invoke());
            }
            else if (!hasAnimations && onComplete != null)
            {
                onComplete?.Invoke();
            }
        }

        public void UpdateMovesDisplay(int movesLeft)
        {
            if (uiElements?.movesText != null)
            {
                uiElements.movesText.text = $"{movesLeft}";
            }
        }

        public void ShowCorrectTileEffect(int tileRow, int tileCol)
        {
            string key = GetTileKey(tileRow, tileCol);

            Debug.Log($"ShowCorrectTileEffect called for tile [{tileRow},{tileCol}], key={key}");
            Debug.Log($"uiElements is null: {uiElements == null}");
            if (uiElements != null)
            {
                Debug.Log($"correctTileParticlesPrefab is null: {uiElements.correctTileParticlesPrefab == null}");
            }

            if (tileObjects.TryGetValue(key, out GameObject tileObj))
            {
                Debug.Log($"Found tile object at position: {tileObj.transform.position}");

                // Instantiate particle system if available
                if (uiElements?.correctTileParticlesPrefab != null)
                {
                    Debug.Log("Particle system prefab found, instantiating...");

                    // Instantiate in world space at tile's position
                    Vector3 tileWorldPos = tileObj.transform.position;
                    ParticleSystem particles = Instantiate(
                        uiElements.correctTileParticlesPrefab,
                        tileWorldPos,
                        Quaternion.identity
                    );

                    // Keep particles at tile's XY position, but slightly in front in Z
                    Vector3 particlePos = tileWorldPos;
                    particlePos.z = tileWorldPos.z - 1f; // Slightly in front of the tile
                    particles.transform.position = particlePos;

                    // Don't override scale - use the prefab's scale
                    // particles.transform.localScale stays at prefab default

                    // Ensure particle system doesn't loop
                    var main = particles.main;
                    main.loop = false; // Play once only

                    // Ensure renderer is visible
                    var renderer = particles.GetComponent<ParticleSystemRenderer>();
                    if (renderer != null)
                    {
                        renderer.sortingOrder = 100; // Render on top
                    }

                    // Play the particle system - don't override settings, use prefab config
                    particles.Play();

                    // Destroy the particle system after it finishes playing
                    float duration = main.duration + main.startLifetime.constantMax;
                    Destroy(particles.gameObject, duration);
                }
                else
                {
                    Debug.LogWarning("No particle system prefab assigned! Using fallback effect.");
                    // Fallback: Create a simple scale pulse and glow effect if no particle system
                    SpriteRenderer sr = tileObj.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        // Store original color
                        Color originalColor = sr.color;

                        // Create sequence for the effect
                        DG.Tweening.Sequence effectSequence = DOTween.Sequence();

                        // Scale pulse
                        effectSequence.Append(tileObj.transform.DOScale(tileObj.transform.localScale * 1.2f, 0.2f).SetEase(Ease.OutQuad));
                        effectSequence.Append(tileObj.transform.DOScale(tileObj.transform.localScale, 0.2f).SetEase(Ease.InQuad));

                        // Flash green color
                        effectSequence.Join(DOTween.To(() => sr.color, x => sr.color = x, Color.green, 0.2f));
                        effectSequence.Append(DOTween.To(() => sr.color, x => sr.color = x, originalColor, 0.2f));
                    }
                }
            }
        }

        private IEnumerator CheckParticleCount(ParticleSystem particles)
        {
            yield return null; // Wait one frame
            Debug.Log($"After one frame - particleCount: {particles.particleCount}, isPlaying: {particles.isPlaying}, isEmitting: {particles.isEmitting}");
        }

        public void FillHolesAndShowOverlay(BoardTile[][] completeBoard, bool success, int coinsEarned, System.Action onRestart, System.Action onNextLevel)
        {
            // First, fill in the holes to show complete image
            StartCoroutine(FillHolesAnimation(completeBoard, () => {
                // After holes are filled, show the overlay
                ShowGameEndOverlay(success, coinsEarned, onRestart, onNextLevel);
            }));
        }

        private IEnumerator FillHolesAnimation(BoardTile[][] completeBoard, System.Action onComplete)
        {
            Debug.Log("FillHolesAnimation started");

            // completeBoard has all tiles with no holes
            // We need to find positions that don't have tile objects currently and create them
            for (int i = 0; i < completeBoard.Length; i++)
            {
                for (int j = 0; j < completeBoard[i].Length; j++)
                {
                    BoardTile tile = completeBoard[i][j];

                    // If there's a tile in the complete board at this position
                    if (tile != null)
                    {
                        string key = GetTileKey(tile.Row, tile.Column);

                        // Check if we don't already have a tile object for this position
                        if (!tileObjects.ContainsKey(key))
                        {
                            Debug.Log($"Creating missing tile at position [{i},{j}] for tile [{tile.Row},{tile.Column}]");

                            int displayCol = j + 1;
                            int displayRow = i + 1;

                            Texture2D tex = level.LevelSprite.texture;
                            Texture2D resizedTex = ResizeTexture(tex, TEXTURE_WIDTH, TEXTURE_HEIGHT);

                            GameObject tileObj = CreateTile2(resizedTex, tile.Column + 1, tile.Row + 1, displayCol, displayRow, tilePixelWidth, tilePixelHeight);

                            // Add chain overlay if tile is locked
                            if (tile.IsLocked)
                            {
                                AddChainOverlay(tileObj);
                            }

                            // Start with alpha 0 for fade in effect
                            SpriteRenderer sr = tileObj.GetComponent<SpriteRenderer>();
                            if (sr != null)
                            {
                                Color color = sr.color;
                                color.a = 0f;
                                sr.color = color;

                                // Fade in the tile
                                sr.DOFade(1f, 0.5f).SetEase(Ease.InQuad);
                            }

                            tileObjects[key] = tileObj;
                        }
                    }
                }
            }

            Debug.Log("FillHolesAnimation waiting for fade to complete");

            // Wait for fade in animation to complete
            yield return new WaitForSeconds(0.5f);

            Debug.Log("FillHolesAnimation complete");
            onComplete?.Invoke();
        }

        public void ShowGameEndOverlay(bool success, int coinsEarned, System.Action onRestart, System.Action onNextLevel)
        {
            Debug.Log($"ShowGameEndOverlay called. success: {success}, coinsEarned: {coinsEarned}");
            Debug.Log($"uiElements is null: {uiElements == null}");
            if (uiElements != null)
            {
                Debug.Log($"gameEndOverlay is null: {uiElements.gameEndOverlay == null}");
            }

            if (uiElements?.gameEndOverlay == null)
            {
                Debug.LogError("Cannot show overlay - uiElements or gameEndOverlay is null!");
                return;
            }

            Debug.Log("Showing overlay...");

            // Setup button listeners
            if (uiElements.restartButton != null)
            {
                uiElements.restartButton.onClick.RemoveAllListeners();
                uiElements.restartButton.onClick.AddListener(() => onRestart?.Invoke());
            }

            if (uiElements.nextLevelButton != null)
            {
                uiElements.nextLevelButton.onClick.RemoveAllListeners();
                uiElements.nextLevelButton.onClick.AddListener(() => onNextLevel?.Invoke());
            }

            // Set text and buttons based on success/failure
            if (success)
            {
                if (uiElements.gameEndTitle != null)
                {
                    uiElements.gameEndTitle.text = "Victory!";
                }

                if (uiElements.restartButton != null)
                {
                    uiElements.restartButton.gameObject.SetActive(true);
                }

                if (uiElements.nextLevelButton != null)
                {
                    uiElements.nextLevelButton.gameObject.SetActive(true);
                }

                // Animate coin counter
                if (uiElements.gameEndCoinsText != null)
                {
                    StartCoroutine(AnimateCoinCounter(coinsEarned));
                }
            }
            else
            {
                if (uiElements.gameEndTitle != null)
                {
                    uiElements.gameEndTitle.text = "Out of Moves!";
                }

                if (uiElements.gameEndCoinsText != null)
                {
                    uiElements.gameEndCoinsText.text = "Try Again";
                }

                if (uiElements.nextLevelButton != null)
                {
                    uiElements.nextLevelButton.gameObject.SetActive(false);
                }

                if (uiElements.restartButton != null)
                {
                    uiElements.restartButton.gameObject.SetActive(true);
                }
            }

            // Show overlay with animation
            uiElements.gameEndOverlay.SetActive(true);

            // Get the Image component to fade it in
            UnityEngine.UI.Image overlayImage = uiElements.gameEndOverlay.GetComponent<UnityEngine.UI.Image>();
            if (overlayImage != null)
            {
                Color imageColor = overlayImage.color;
                imageColor.a = 0f;
                overlayImage.color = imageColor;
                overlayImage.DOFade(0.9f, 0.6f).SetEase(Ease.OutQuad);
            }

            // Start with scale 0 and animate up with a slight delay for smoother feel
            uiElements.gameEndOverlay.transform.localScale = Vector3.zero;
            uiElements.gameEndOverlay.transform.DOScale(Vector3.one, 0.6f)
                .SetEase(Ease.OutBack)
                .SetDelay(0.2f);  // Small delay before scaling up
        }

        private IEnumerator AnimateCoinCounter(int coinsEarned)
        {
            // Wait a moment before starting animation
            yield return new WaitForSeconds(0.5f);

            int currentCoins = 0;
            float duration = Mathf.Min(coinsEarned * 0.05f, 2f); // Max 2 seconds
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                currentCoins = Mathf.RoundToInt(Mathf.Lerp(0, coinsEarned, progress));
                uiElements.gameEndCoinsText.text = $"+{currentCoins} coins!";
                yield return null;
            }

            // Ensure final value is exact
            uiElements.gameEndCoinsText.text = $"+{coinsEarned} coins!";
        }

        public void HideGameEndOverlay()
        {
            if (uiElements?.gameEndOverlay != null)
            {
                uiElements.gameEndOverlay.SetActive(false);
            }
        }

        private Texture2D LoadTextureFromFile(string filePath)
        {
            byte[] fileData = File.ReadAllBytes(filePath);
            Texture2D tex = new Texture2D(2, 2); // Size doesn't matter; will be replaced
            tex.LoadImage(fileData);             // Automatically resizes and loads
            return tex;
        }

        private Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
        {
            // Create a new RenderTexture
            RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
            RenderTexture.active = rt;

            // Blit the original texture into the new size
            Graphics.Blit(source, rt);

            // Create a new Texture2D and read pixels from the render texture
            Texture2D newTex = new Texture2D(newWidth, newHeight, source.format, false);
            newTex.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
            newTex.Apply();

            // Clean up
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            return newTex;
        }

        private GameObject CreateTile2(Texture2D source, int tileCol, int tileRow, int displayCol, int displayRow, int pixelWidth, int pixelHeight)
        {
            // Slice the texture based on tile's identity (which piece of the image)
            int startX = (tileCol - 1) * pixelWidth;
            int startY = TEXTURE_HEIGHT - (tileRow * pixelHeight);

            Texture2D part = CopyRegion(source, startX, startY, pixelWidth, pixelHeight);

            // Calculate pixels per unit so sprite size matches desired world size
            // We want the sprite to be exactly desiredTileWorldWidth units wide
            float pixelsPerUnit = pixelWidth / desiredTileWorldWidth;

            Sprite sprite = Sprite.Create(
                part,
                new Rect(0, 0, part.width, part.height),
                new Vector2(0.5f, 0.5f),  // pivot point (center)
                pixelsPerUnit);

            GameObject go = new GameObject($"Tile_{tileRow - 1}_{tileCol - 1}");
            go.AddComponent<SpriteRenderer>();
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 10; // Render in front

            go.transform.SetParent(this.gameObject.transform);

            // Position based on where it should be displayed on the board
            go.transform.localPosition = GetLocalPos2(displayCol, displayRow, pixelWidth, pixelHeight);

            return go;
        }
        private Vector3 GetLocalPos2(int col, int row, int widthPixels, int heightPixels)
        {
            // Calculate the board dimensions based on tile count
            int totalCols = level.Columns;
            int totalRows = level.Rows;

            // Calculate board dimensions in world space
            float boardWidth = totalCols * desiredTileWorldWidth;
            float boardHeight = totalRows * desiredTileWorldHeight;

            // Center position in world space
            float startX = -boardWidth / 2f + desiredTileWorldWidth / 2f;
            float startY = boardHeight / 2f - desiredTileWorldHeight / 2f;

            // Position this specific tile in world space
            float x = startX + ((col - 1) * desiredTileWorldWidth);
            float y = startY - ((row - 1) * desiredTileWorldHeight);

            // Convert world space position to local space by dividing by parent's scale
            Vector3 parentScale = this.transform.lossyScale;
            float localX = x / parentScale.x;
            float localY = y / parentScale.y;

            return new Vector3(localX, localY, -10f);
        }
        private Texture2D CopyRegion(Texture2D source, int x, int y, int width, int height)
        {
            Texture2D newTex = new Texture2D(width, height, source.format, false);
            Color[] pixels = source.GetPixels(x, y, width, height);
            newTex.SetPixels(pixels);
            newTex.Apply();
            return newTex;
        }

        /// <summary>
        /// Shows the complete puzzle image overlay for 3 seconds
        /// </summary>
        public void ShowCompletePuzzleOverlay(Sprite completeImage, System.Action onComplete = null)
        {
            Debug.Log("ShowCompletePuzzleOverlay called");
            Debug.Log($"  completeImage is null: {completeImage == null}");
            Debug.Log($"  transform is null: {transform == null}");

            if (completeImage == null)
            {
                Debug.LogError("Complete image sprite is null!");
                onComplete?.Invoke();
                return;
            }

            // Create overlay GameObject as a screen space overlay (top level)
            GameObject overlayObj = new GameObject("CompletePuzzleOverlay");
            Debug.Log($"  Created overlay object: {overlayObj.name}");

            // Add Canvas for UI overlay
            Canvas canvas = overlayObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // Above everything

            // Add CanvasScaler for proper scaling
            CanvasScaler scaler = overlayObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);

            // Add GraphicRaycaster for interaction blocking
            overlayObj.AddComponent<GraphicRaycaster>();

            // Add semi-transparent background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(overlayObj.transform, false);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.7f); // Semi-transparent black
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            // Add complete puzzle image
            GameObject imageObj = new GameObject("CompletePuzzle");
            imageObj.transform.SetParent(overlayObj.transform, false);
            Image puzzleImage = imageObj.AddComponent<Image>();
            puzzleImage.sprite = completeImage;
            puzzleImage.preserveAspect = true;

            RectTransform imageRect = imageObj.GetComponent<RectTransform>();
            // Center the image and use the actual board dimensions
            imageRect.anchorMin = new Vector2(0.5f, 0.5f);
            imageRect.anchorMax = new Vector2(0.5f, 0.5f);
            imageRect.pivot = new Vector2(0.5f, 0.5f);

            // Calculate size to match board dimensions
            // Board is 4.0 width x 7.0 height in world units
            // Convert to screen pixels based on camera
            float pixelsPerUnit = Screen.height / (Camera.main.orthographicSize * 2);
            float boardWidthPixels = 4.0f * pixelsPerUnit;
            float boardHeightPixels = 7.0f * pixelsPerUnit;

            imageRect.sizeDelta = new Vector2(boardWidthPixels, boardHeightPixels);
            imageRect.anchoredPosition = Vector2.zero;

            // Fade in animation
            CanvasGroup canvasGroup = overlayObj.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;

            Debug.Log("  Starting fade in animation");

            canvasGroup.DOFade(1f, 0.3f)
                .OnComplete(() =>
                {
                    Debug.Log("  Fade in complete, waiting 3 seconds");
                    // Wait 3 seconds
                    DOVirtual.DelayedCall(3f, () =>
                    {
                        Debug.Log("  Starting fade out");
                        // Fade out
                        canvasGroup.DOFade(0f, 0.3f)
                            .OnComplete(() =>
                            {
                                Debug.Log("  Fade out complete, destroying overlay");
                                Destroy(overlayObj);
                                onComplete?.Invoke();
                            });
                    });
                });
        }

        /// <summary>
        /// Updates chain overlay on a tile based on locked state
        /// </summary>
        private void UpdateChainOverlay(GameObject tileObj, bool shouldBeLocked)
        {
            Transform chainOverlay = tileObj.transform.Find("ChainOverlay");
            bool hasChain = chainOverlay != null;

            if (shouldBeLocked && !hasChain)
            {
                // Should be locked but no chain - add it
                AddChainOverlay(tileObj);
            }
            else if (!shouldBeLocked && hasChain)
            {
                // Should not be locked but has chain - remove it immediately (no animation here)
                Destroy(chainOverlay.gameObject);
                Debug.Log($"Removed chain overlay from {tileObj.name} (tile unlocked)");
            }
        }

        /// <summary>
        /// Adds a chain overlay sprite to a locked tile
        /// </summary>
        private void AddChainOverlay(GameObject tileObj)
        {
            Debug.Log($"AddChainOverlay called for tile {tileObj.name}");

            // Load the chains sprite from Resources
            Sprite chainSprite = Resources.Load<Sprite>("Images/chains");

            Debug.Log($"Chain sprite loaded: {chainSprite != null}");

            if (chainSprite == null)
            {
                Debug.LogError("Failed to load chains.png from Resources/Images/");
                Debug.LogError("Make sure chains.png is at: Client/Assets/Resources/Images/chains.png");
                return;
            }

            // Create chain overlay as child of tile
            GameObject chainObj = new GameObject("ChainOverlay");
            chainObj.transform.SetParent(tileObj.transform, false);

            SpriteRenderer chainSr = chainObj.AddComponent<SpriteRenderer>();
            chainSr.sprite = chainSprite;
            chainSr.sortingOrder = 20; // Above the tile

            // Position at tile center
            chainObj.transform.localPosition = Vector3.zero;

            // Get the tile's sprite renderer to match size
            SpriteRenderer tileSr = tileObj.GetComponent<SpriteRenderer>();
            if (tileSr != null && tileSr.sprite != null)
            {
                // Calculate scale to match tile size
                float tileWidth = tileSr.sprite.bounds.size.x;
                float tileHeight = tileSr.sprite.bounds.size.y;
                float chainWidth = chainSprite.bounds.size.x;
                float chainHeight = chainSprite.bounds.size.y;

                // Scale chain to fit tile (cover it completely)
                float scaleX = tileWidth / chainWidth;
                float scaleY = tileHeight / chainHeight;

                chainObj.transform.localScale = new Vector3(scaleX, scaleY, 1f);
                Debug.Log($"Chain scale: {scaleX}x{scaleY}, Tile size: {tileWidth}x{tileHeight}, Chain size: {chainWidth}x{chainHeight}");
            }
            else
            {
                // Fallback to uniform scale
                chainObj.transform.localScale = Vector3.one;
            }

            Debug.Log($"Successfully added chain overlay to tile {tileObj.name}");
        }

        /// <summary>
        /// Animates the unlocking of a tile (removes chain overlay with animation)
        /// </summary>
        public void AnimateUnlock(int row, int col)
        {
            string key = GetTileKey(row, col);
            Debug.Log($"AnimateUnlock called for tile [{row},{col}], key: {key}");

            if (tileObjects.TryGetValue(key, out GameObject tileObj))
            {
                Debug.Log($"Found tile object: {tileObj.name}");

                // Find the chain overlay child
                Transform chainOverlay = tileObj.transform.Find("ChainOverlay");

                Debug.Log($"Chain overlay found: {chainOverlay != null}");

                if (chainOverlay != null)
                {
                    Debug.Log($"Animating unlock for tile [{row},{col}]");

                    // Animate chain breaking - scale up and fade out
                    SpriteRenderer chainSr = chainOverlay.GetComponent<SpriteRenderer>();

                    if (chainSr != null)
                    {
                        // Scale up with punch effect
                        chainOverlay.DOScale(Vector3.one * 1.5f, 0.3f)
                            .SetEase(Ease.OutBack);

                        // Fade out
                        chainSr.DOFade(0f, 0.3f)
                            .OnComplete(() =>
                            {
                                Destroy(chainOverlay.gameObject);
                                Debug.Log($"Chain overlay removed from tile [{row},{col}]");
                            });
                    }
                }
                else
                {
                    Debug.LogWarning($"No chain overlay found on tile [{row},{col}]");
                }
            }
            else
            {
                Debug.LogWarning($"Tile [{row},{col}] not found in tileObjects");
            }
        }
    }
}