using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using PuzzleParty.EGP;
using PuzzleParty.Levels;
using PuzzleParty.Board.Effects;

namespace PuzzleParty.Board
{

    public class BoardView : MonoBehaviour
    {
        private Dictionary<string, GameObject> tileObjects = new Dictionary<string, GameObject>();
        private Level level;
        private LevelUIElements uiElements;
        private int currentStreak; // Current streak count for powerup display

        [Header("Effect Prefabs")]
        [SerializeField] private SparkleTrailEffect sparkleTrailEffectPrefab;

        [Header("Overlays")]
        [SerializeField] private GameEndOverlayController gameEndOverlay;
        [SerializeField] private ChainSystem chainSystem;

        // Dynamic tile sizing
        private int tilePixelWidth;
        private int tilePixelHeight;
        private float desiredTileWorldWidth;
        private float desiredTileWorldHeight;

        public void Setup(Level level, BoardTile[][] initialBoard, LevelUIElements uiElements, int currentStreak)
        {
            this.level = level;
            this.uiElements = uiElements;
            this.currentStreak = currentStreak;

            chainSystem.Initialize();
            chainSystem.SetTileObjects(tileObjects);

            int cols = level.Columns;
            int rows = level.Rows;

            // Calculate dynamic tile size based on board dimensions
            // The texture is always 768x1344, divide it by the grid size
            tilePixelWidth = TileFactory.TEXTURE_WIDTH / cols;
            tilePixelHeight = TileFactory.TEXTURE_HEIGHT / rows;

            // Calculate tile world size to keep board dimensions constant
            // For 4x7: width=4.0/4=1.0, height=7.0/7=1.0 (square tiles)
            // For 3x5: width=4.0/3=1.33, height=7.0/5=1.4 (slightly rectangular)
            const float DESIRED_BOARD_WIDTH = 4.0f;
            const float DESIRED_BOARD_HEIGHT = 7.0f;

            desiredTileWorldWidth = DESIRED_BOARD_WIDTH / cols;
            desiredTileWorldHeight = DESIRED_BOARD_HEIGHT / rows;

            Texture2D tex = level.LevelSprite.texture;
            Texture2D newTex = TileFactory.ResizeTexture(tex, TileFactory.TEXTURE_WIDTH, TileFactory.TEXTURE_HEIGHT);

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

                        GameObject tileObj = TileFactory.CreateTile(newTex, tile.Column + 1, tile.Row + 1, displayCol, displayRow, tilePixelWidth, tilePixelHeight, desiredTileWorldWidth, desiredTileWorldHeight, cols, rows, transform);
                        string key = TileFactory.GetTileKey(tile.Row, tile.Column);
                        tileObjects[key] = tileObj;

                        // Don't add chains during setup - we'll add them after scramble animation
                    }
                }
            }
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
                            // Center the lock on the icon
                            lockRect.anchorMin = new Vector2(0.5f, 0.5f);
                            lockRect.anchorMax = new Vector2(0.5f, 0.5f);
                            lockRect.pivot = new Vector2(0.5f, 0.5f);
                            lockRect.anchoredPosition = Vector2.zero;
                            lockRect.sizeDelta = new Vector2(35f, 35f);
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
            Vector3 targetPos;
            Canvas targetCanvas = target.GetComponentInParent<Canvas>();
            if (targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                Camera targetCam = targetCanvas.worldCamera ?? Camera.main;
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(targetCam, target.position);
                targetPos = new Vector3(screenPoint.x, screenPoint.y, 0f);
            }
            else
            {
                targetPos = target.position;
            }

            // Instantiate sparkle trail effect
            bool trailComplete = false;

            if (sparkleTrailEffectPrefab != null)
            {
                SparkleTrailEffect trail = Instantiate(sparkleTrailEffectPrefab);
                trail.Play(sourcePos, targetPos, sparkleParent, target, () => { trailComplete = true; });
            }
            else
            {
                Debug.LogWarning("SparkleTrailEffect prefab not assigned, skipping sparkle trail");
                trailComplete = true;
            }

            // Wait for the trail to complete
            while (!trailComplete)
            {
                yield return null;
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
                        string key = TileFactory.GetTileKey(tile.Row, tile.Column);
                        tilesInScrambledBoard.Add(key);

                        if (tileObjects.TryGetValue(key, out GameObject tileObj))
                        {
                            // Calculate target position based on scrambled board position (i, j)
                            Vector3 targetPos = TileFactory.GetTileLocalPosition(j + 1, i + 1, level.Columns, level.Rows, desiredTileWorldWidth, desiredTileWorldHeight, transform.lossyScale);

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
            // Calculate how many tiles are correctly placed after scramble
            int correctCount = 0;
            for (int i = 0; i < scrambledBoard.Length; i++)
            {
                for (int j = 0; j < scrambledBoard[i].Length; j++)
                {
                    BoardTile tile = scrambledBoard[i][j];
                    if (tile != null && tile.Row == i && tile.Column == j)
                    {
                        correctCount++;
                    }
                }
            }
            int tilesRemaining = Mathf.Max(1, 4 - correctCount); // At least 1 to show chains
            chainSystem.SetInitialState(tilesRemaining);

            // Find all locked tiles in the scrambled board
            List<GameObject> lockedTileObjects = new List<GameObject>();

            for (int i = 0; i < scrambledBoard.Length; i++)
            {
                for (int j = 0; j < scrambledBoard[i].Length; j++)
                {
                    BoardTile tile = scrambledBoard[i][j];
                    if (tile != null && tile.IsLocked)
                    {
                        string key = TileFactory.GetTileKey(tile.Row, tile.Column);
                        if (tileObjects.TryGetValue(key, out GameObject tileObj))
                        {
                            lockedTileObjects.Add(tileObj);
                        }
                    }
                }
            }

            if (lockedTileObjects.Count > 0)
            {

                // Add chains to all locked tiles with stagger animation
                DG.Tweening.Sequence lockSequence = DOTween.Sequence();

                for (int i = 0; i < lockedTileObjects.Count; i++)
                {
                    GameObject tileObj = lockedTileObjects[i];

                    // Add chain overlay with correct initial state
                    chainSystem.AddChainOverlay(tileObj, tilesRemaining);

                    float delay = i * 0.1f; // 0.1 second stagger between each lock

                    // Find the dark overlay and animate it
                    ChainOverlayController controller = tileObj.GetComponentInChildren<ChainOverlayController>();
                    if (controller != null)
                    {
                        SpriteRenderer darkSr = controller.DarkOverlayRenderer;
                        if (darkSr != null)
                        {
                            Color darkColor = darkSr.color;
                            float targetAlpha = darkColor.a;
                            darkColor.a = 0f;
                            darkSr.color = darkColor;
                            lockSequence.Insert(delay, darkSr.DOFade(targetAlpha, 0.5f));
                        }

                        SpriteRenderer chainSr = controller.ChainRenderer;
                        if (chainSr != null)
                        {
                            // Save the target scale
                            Vector3 targetScale = controller.transform.localScale;

                            // Start with scale 0
                            controller.transform.localScale = Vector3.zero;

                            // Animate scale up with bounce
                            lockSequence.Insert(delay, controller.transform.DOScale(targetScale, 0.5f).SetEase(Ease.OutBack));

                            // Fade in chain sprite
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
                onComplete?.Invoke();
            }
        }

        public BoardTile GetTileAtPosition(Vector3 worldPosition)
        {
            foreach (var kvp in tileObjects)
            {
                GameObject tileObj = kvp.Value;
                SpriteRenderer sr = tileObj.GetComponent<SpriteRenderer>();

                if (sr != null)
                {
                    Vector3 checkPosition = new Vector3(worldPosition.x, worldPosition.y, tileObj.transform.position.z);

                    if (sr.bounds.Contains(checkPosition))
                    {
                        string[] parts = kvp.Key.Split('_');
                        int row = int.Parse(parts[0]);
                        int col = int.Parse(parts[1]);
                        return new BoardTile { Row = row, Column = col };
                    }
                }
            }

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
                        string key = TileFactory.GetTileKey(tile.Row, tile.Column);
                        if (tileObjects.TryGetValue(key, out GameObject tileObj))
                        {
                            Vector3 targetPos = TileFactory.GetTileLocalPosition(j + 1, i + 1, level.Columns, level.Rows, desiredTileWorldWidth, desiredTileWorldHeight, transform.lossyScale);
                            moveSequence.Join(tileObj.transform.DOLocalMove(targetPos, 0.1f).SetEase(Ease.OutQuad));
                            hasAnimations = true;

                            // Update chain overlay based on locked state
                            chainSystem.UpdateChainOverlay(tileObj, tile.IsLocked);
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
            string key = TileFactory.GetTileKey(tileRow, tileCol);

            if (tileObjects.TryGetValue(key, out GameObject tileObj))
            {
                if (uiElements?.correctTileParticlesPrefab != null)
                {

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

        public void FillHolesAndShowOverlay(BoardTile[][] completeBoard, bool success, int coinsEarned, System.Action onGiveUp, System.Action onNextLevel)
        {
            // First, fill in the holes to show complete image
            StartCoroutine(FillHolesAnimation(completeBoard, () => {
                // After holes are filled, show the overlay
                ShowGameEndOverlay(success, coinsEarned, onGiveUp, onNextLevel);
            }));
        }

        private IEnumerator FillHolesAnimation(BoardTile[][] completeBoard, System.Action onComplete)
        {
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
                        string key = TileFactory.GetTileKey(tile.Row, tile.Column);

                        // Check if we don't already have a tile object for this position
                        if (!tileObjects.ContainsKey(key))
                        {

                            int displayCol = j + 1;
                            int displayRow = i + 1;

                            Texture2D tex = level.LevelSprite.texture;
                            Texture2D resizedTex = TileFactory.ResizeTexture(tex, TileFactory.TEXTURE_WIDTH, TileFactory.TEXTURE_HEIGHT);

                            GameObject tileObj = TileFactory.CreateTile(resizedTex, tile.Column + 1, tile.Row + 1, displayCol, displayRow, tilePixelWidth, tilePixelHeight, desiredTileWorldWidth, desiredTileWorldHeight, level.Columns, level.Rows, transform);

                            // Add chain overlay if tile is locked
                            if (tile.IsLocked)
                            {
                                chainSystem.AddChainOverlay(tileObj);
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

            yield return new WaitForSeconds(0.5f);
            onComplete?.Invoke();
        }

        public void ShowGameEndOverlay(bool success, int coinsEarned, System.Action onGiveUp, System.Action onNextLevel)
        {
            if (success)
                gameEndOverlay.ShowSuccess(coinsEarned, onNextLevel);
            else
                gameEndOverlay.ShowFailure(onGiveUp);
        }

        public void ShowEGPOverlay(EGPRound offer, bool canAfford, System.Action onPurchase, System.Action onGiveUp)
        {
            gameEndOverlay.ShowEGP(offer, canAfford, onPurchase, onGiveUp);
        }

        public void HideEGPElements()
        {
            gameEndOverlay.HideEGPElements();
        }

        public void HideGameEndOverlay()
        {
            gameEndOverlay.Hide();
        }

        /// <summary>
        /// Shows the complete puzzle image overlay for 3 seconds
        /// </summary>
        public void ShowCompletePuzzleOverlay(Sprite completeImage, System.Action onComplete = null)
        {
            if (completeImage == null)
            {
                Debug.LogError("Complete image sprite is null!");
                onComplete?.Invoke();
                return;
            }

            GameObject overlayObj = new GameObject("CompletePuzzleOverlay");

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

            canvasGroup.DOFade(1f, 0.3f)
                .OnComplete(() =>
                {
                    DOVirtual.DelayedCall(3f, () =>
                    {
                        canvasGroup.DOFade(0f, 0.3f)
                            .OnComplete(() =>
                            {
                                Destroy(overlayObj);
                                onComplete?.Invoke();
                            });
                    });
                });
        }

        public void UpdateChainProgress(int correctlyPlacedCount)
            => chainSystem.UpdateChainProgress(correctlyPlacedCount);

        public void AnimateUnlock(int row, int col)
            => chainSystem.AnimateUnlock(row, col);
    }
}
