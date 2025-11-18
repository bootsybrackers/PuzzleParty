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

        public void Setup(Level level, BoardTile[][] initialBoard, LevelUIElements uiElements)
        {
            this.level = level;
            this.uiElements = uiElements;

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
            // Set board to 80% scale at the start
            Vector3 originalScale = this.transform.localScale;
            this.transform.localScale = originalScale * 0.8f;

            // Prepare welcome text and wait one frame to ensure changes are applied
            if (uiElements?.welcomeText != null)
            {
                uiElements.welcomeText.text = $"Welcome to {level.Name}";

                // Set initial alpha to 0
                Color textColor = uiElements.welcomeText.color;
                textColor.a = 0f;
                uiElements.welcomeText.color = textColor;

                // Get RectTransform and store original position
                RectTransform rectTransform = uiElements.welcomeText.rectTransform;
                Vector2 originalAnchoredPos = rectTransform.anchoredPosition;

                // Position text off-screen to the left (500 pixels)
                Vector2 textStartAnchoredPos = originalAnchoredPos;
                textStartAnchoredPos.x -= 500f;
                rectTransform.anchoredPosition = textStartAnchoredPos;

                // Wait one frame for changes to take effect
                yield return null;
            }

            // Step 1: Slide in and fade in welcome text
            if (uiElements?.welcomeText != null)
            {
                // Get RectTransform for UI element (TextMeshProUGUI always has RectTransform)
                RectTransform rectTransform = uiElements.welcomeText.rectTransform;

                // Calculate the target position (current position is already off-screen)
                Vector2 targetAnchoredPos = rectTransform.anchoredPosition;
                targetAnchoredPos.x += 500f; // Move back to original position

                // Slide in and fade in the text with bouncy feel
                DG.Tweening.Sequence textSequence = DOTween.Sequence();
                textSequence.Append(rectTransform.DOAnchorPos(targetAnchoredPos, 0.5f).SetEase(Ease.OutBack));
                textSequence.Join(DOTween.ToAlpha(() => uiElements.welcomeText.color, x => uiElements.welcomeText.color = x, 1f, 0.5f));
            }

            // Wait for text slide-in to complete
            yield return new WaitForSeconds(0.5f);

            // Keep text visible for a moment
            yield return new WaitForSeconds(1f);

            // Step 2: Fade out welcome text AND scale board at the same time
            if (uiElements?.welcomeText != null)
            {
                DOTween.ToAlpha(() => uiElements.welcomeText.color, x => uiElements.welcomeText.color = x, 0f, 0.5f);
            }

            // Start board scaling at the same time
            this.transform.DOScale(originalScale, 0.6f).SetEase(Ease.OutBack, 2f);

            // Wait for board scaling to complete (longer duration)
            yield return new WaitForSeconds(0.6f);

            // Long pause before scrambling
            yield return new WaitForSeconds(0.5f);

            // Signal that we're ready to scramble
            onComplete?.Invoke();
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
                onComplete?.Invoke();
            });
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
            Debug.Log($"Setting gameEndOverlay active. Current state: {uiElements.gameEndOverlay.activeSelf}");
            uiElements.gameEndOverlay.SetActive(true);
            Debug.Log($"After SetActive. New state: {uiElements.gameEndOverlay.activeSelf}");

            // Get the Image component to fade it in
            UnityEngine.UI.Image overlayImage = uiElements.gameEndOverlay.GetComponent<UnityEngine.UI.Image>();
            Debug.Log($"overlayImage is null: {overlayImage == null}");
            if (overlayImage != null)
            {
                Color imageColor = overlayImage.color;
                Debug.Log($"Original image color: {imageColor}");
                imageColor.a = 0f;
                overlayImage.color = imageColor;
                Debug.Log($"Set image alpha to 0, starting fade to 1");
                overlayImage.DOFade(1f, 0.5f).SetEase(Ease.OutQuad);
            }

            // Scale up animation
            Debug.Log($"Current scale: {uiElements.gameEndOverlay.transform.localScale}");
            uiElements.gameEndOverlay.transform.localScale = Vector3.zero;
            uiElements.gameEndOverlay.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
            Debug.Log("Overlay animation setup complete");
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
    }
}