using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PuzzleParty.EGP;
using PuzzleParty.Levels;
using PuzzleParty.Maps;
using PuzzleParty.Progressions;
using PuzzleParty.Service;

namespace PuzzleParty.Board
{
    public enum MoveDirection
    {
        LEFT,
        RIGHT,
        UP,
        DOWN
    }

    public class BoardController : MonoBehaviour
    {
        [SerializeField]
        private LevelUIElements uiElements;

        [SerializeField] private InputHandler inputHandler;

        private ILevelService levelService;
        private IProgressionService progressionService;
        private ISceneLoader sceneLoader;
        private ITransitionService transitionService;
        private IEGPService egpService;
        private BoardView boardView;
        private BoardManager boardManager;
        private Level currentLevel;
        private HashSet<string> tilesMarkedCorrect = new HashSet<string>();
        private bool isFirstCheck = true;

        // Power-up tracking
        private bool hasUsedCompletePuzzle = false;

        void Start()
        {
            inputHandler.OnSwipe += OnSwipe;
            levelService = ServiceLocator.GetInstance().Get<LevelService>();
            progressionService = ServiceLocator.GetInstance().Get<ProgressionService>();
            sceneLoader = ServiceLocator.GetInstance().Get<SceneLoader>();
            transitionService = ServiceLocator.GetInstance().Get<TransitionService>();
            egpService = ServiceLocator.GetInstance().Get<EGPService>();
            SetupLevel();

            // Fade in from black when scene starts
            transitionService.FadeIn();
        }

        void Update()
        {
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.W))
                OnPuzzleSolved();
#endif
        }

        void OnSwipe(Vector3 startPos, MoveDirection direction)
        {
            BoardTile clickedTile = boardView.GetTileAtPosition(startPos);
            if (clickedTile == null) return;

            // Get the tile from the board with its current locked state
            BoardTile[][] currentBoard = boardManager.GetCurrentBoard();
            BoardTile actualTile = null;
            for (int i = 0; i < currentBoard.Length; i++)
            {
                for (int j = 0; j < currentBoard[i].Length; j++)
                {
                    BoardTile tile = currentBoard[i][j];
                    if (tile != null && tile.Row == clickedTile.Row && tile.Column == clickedTile.Column)
                    {
                        actualTile = tile;
                        break;
                    }
                }
                if (actualTile != null) break;
            }

            if (actualTile == null) return;

            if (boardManager.CanMoveTile(actualTile, direction))
            {
                boardManager.MoveTile(actualTile, direction);

                boardView.UpdateTilePositions(boardManager.GetCurrentBoard(), () => {
                    CheckForCorrectlyPlacedTiles();
                    CheckAndUnlockTiles();
                    CheckAndBreakIce();

                    if (boardManager.IsSolved)
                        OnPuzzleSolved();
                    else if (boardManager.MovesLeft <= 0)
                        OnOutOfMoves();
                });

                boardView.UpdateMovesDisplay(boardManager.MovesLeft);
            }
        }

        void SetupLevel()
        {
            egpService.ResetRounds();
            currentLevel = levelService.GetNextLevel();
            boardManager = new BoardManager(currentLevel);
            boardManager.Init();

            // Get current streak from progression
            Progression progression = progressionService.GetProgression();
            int currentStreak = progression.streak;

            boardView = GetComponent<BoardView>();
            boardView.Setup(currentLevel, boardManager.GetInitialBoard(), uiElements, currentStreak);

            // Initialize moves display
            boardView.UpdateMovesDisplay(boardManager.MovesLeft);

            // Hide game end overlay initially
            boardView.HideGameEndOverlay();

            // Reset tracking state
            tilesMarkedCorrect.Clear();
            isFirstCheck = true;
            inputHandler.DisableInput();

            // Reset power-up usage for this level
            hasUsedCompletePuzzle = false;
            SetupPowerUpButton();

            // Enable powerup buttons based on streak
            if (uiElements.completePuzzleButton != null)
            {
                uiElements.completePuzzleButton.gameObject.SetActive(currentStreak >= 1);
            }
            if (uiElements.slotButton != null)
            {
                uiElements.slotButton.gameObject.SetActive(currentStreak >= 3);
            }

            // Apply moves bonus if streak >= 2
            if (currentStreak >= 2)
            {
                boardManager.AddMoves(20);
                boardView.UpdateMovesDisplay(boardManager.MovesLeft);
            }

            // Start the animation sequence with welcome text and scaling
            boardView.StartAnimation(() => {
                // After showing complete puzzle, animate the scramble
                boardView.AnimateScramble(boardManager.GetCurrentBoard(), () => {
                    // Do the first check to mark initially correct tiles without showing particles
                    CheckForCorrectlyPlacedTiles();

                    // Initialize chain state based on initial correct count (no animation)
                    int initialCorrectCount = boardManager.GetCorrectlyPlacedTilesCount();
                    boardView.UpdateChainProgress(initialCorrectCount);

                    // Now disable first check flag so future checks will show particles
                    isFirstCheck = false;

                    // Enable input after animations complete
                    inputHandler.EnableInput();
                });
            });
        }

        void CheckForCorrectlyPlacedTiles()
        {
            BoardTile[][] currentBoard = boardManager.GetCurrentBoard();

            for (int i = 0; i < currentBoard.Length; i++)
            {
                for (int j = 0; j < currentBoard[i].Length; j++)
                {
                    BoardTile tile = currentBoard[i][j];

                    // Check if tile is in correct position
                    if (tile != null && tile.Row == i && tile.Column == j)
                    {
                        string tileKey = $"{tile.Row}_{tile.Column}";

                        // If this tile hasn't been marked correct before
                        if (!tilesMarkedCorrect.Contains(tileKey))
                        {
                            tilesMarkedCorrect.Add(tileKey);

                            if (!isFirstCheck)
                            {
                                boardView.ShowCorrectTileEffect(tile.Row, tile.Column);
                            }
                        }
                    }
                    else
                    {
                        // If tile is not in correct position, remove it from the marked set
                        // so it can trigger particles again when placed correctly
                        if (tile != null)
                        {
                            string tileKey = $"{tile.Row}_{tile.Column}";
                            tilesMarkedCorrect.Remove(tileKey);
                        }
                    }
                }
            }

            // Update chain progress visualization (only after initial setup)
            if (!isFirstCheck)
            {
                int correctCount = boardManager.GetCorrectlyPlacedTilesCount();
                boardView.UpdateChainProgress(correctCount);
            }
        }

        void OnPuzzleSolved()
        {
            inputHandler.DisableInput();

            // Calculate coins earned (1 coin per remaining move)
            int coinsEarned = boardManager.MovesLeft;

            // Save progression
            Progression progression = progressionService.GetProgression();
            progression.coins += coinsEarned;

            // Update last beaten level if this is the next level
            progression.lastBeatenLevel = currentLevel.Id;

            progressionService.SaveProgression(progression);

            // Increment streak on win
            progressionService.IncrementStreak();

            // Fill holes to show complete image, then show success overlay
            boardView.FillHolesAndShowOverlay(
                boardManager.GetInitialBoard(),
                success: true,
                coinsEarned: coinsEarned,
                onGiveUp: GiveUp,
                onNextLevel: LoadNextLevel
            );
        }

        void OnOutOfMoves()
        {
            inputHandler.DisableInput();

            // Check if EGP offer is available
            EGPRound offer = egpService.GetCurrentOffer();
            if (offer != null)
            {
                bool canAfford = egpService.CanAfford(progressionService);
                boardView.ShowEGPOverlay(
                    offer,
                    canAfford,
                    onPurchase: OnEGPPurchase,
                    onGiveUp: GiveUp
                );
            }
            else
            {
                progressionService.ResetStreak();

                // Show failure overlay
                boardView.ShowGameEndOverlay(
                    success: false,
                    coinsEarned: 0,
                    onGiveUp: GiveUp,
                    onNextLevel: LoadNextLevel
                );
            }
        }

        void OnEGPPurchase()
        {
            EGPContents contents = egpService.Purchase(progressionService);
            if (contents == null)
            {
                Debug.LogWarning("EGP purchase failed");
                return;
            }

            // Add extra moves
            boardManager.AddMoves(contents.extraMoves);
            boardView.UpdateMovesDisplay(boardManager.MovesLeft);

            // Hide overlay and resume gameplay
            boardView.HideGameEndOverlay();
            inputHandler.EnableInput();
        }

        void RestartLevel()
        {
            // Reset streak on manual restart
            progressionService.ResetStreak();

            // Clear all existing tile objects
            boardView.HideGameEndOverlay();

            // Destroy all tile GameObjects
            foreach (Transform child in boardView.transform)
            {
                Destroy(child.gameObject);
            }

            // Reset and setup level again
            SetupLevel();
        }

        void GiveUp()
        {
            progressionService.ResetStreak();
            sceneLoader.LoadMainMenu();
        }

        void LoadNextLevel()
        {
            PlayerPrefs.SetInt("JustCompletedLevel", 1);

            // Check if this was the last level of a map
            IMapService mapService = ServiceLocator.GetInstance().Get<MapService>();
            foreach (Map map in mapService.GetAllMaps())
            {
                if (map.endLevel == currentLevel.Id)
                {
                    PlayerPrefs.SetInt("JustCompletedMap", 1);
                    PlayerPrefs.SetInt("CompletedMapId", map.id);
                    break;
                }
            }

            PlayerPrefs.Save();
            sceneLoader.LoadMainMenu();
        }

        void SetupPowerUpButton()
        {
            if (uiElements.completePuzzleButton != null)
            {
                // Remove existing listeners
                uiElements.completePuzzleButton.onClick.RemoveAllListeners();

                // Add listener
                uiElements.completePuzzleButton.onClick.AddListener(OnCompletePuzzleButtonClicked);

                // Enable button (will be disabled after first use)
                uiElements.completePuzzleButton.interactable = true;
            }
            else
            {
                Debug.LogWarning("Complete Puzzle button is not assigned in UI Elements");
            }
        }

        void OnCompletePuzzleButtonClicked()
        {
            if (hasUsedCompletePuzzle)
            {
                Debug.LogWarning("Complete Puzzle power-up already used this level");
                return;
            }

            // Mark as used
            hasUsedCompletePuzzle = true;

            // Disable button
            if (uiElements.completePuzzleButton != null)
            {
                uiElements.completePuzzleButton.interactable = false;
            }

            // Disable input during power-up
            inputHandler.DisableInput();

            // Show complete puzzle overlay for 3 seconds
            boardView.ShowCompletePuzzleOverlay(currentLevel.LevelSprite, () =>
            {
                inputHandler.EnableInput();
            });
        }

        void CheckAndUnlockTiles()
        {
            List<(int row, int col)> unlockedTiles = boardManager.CheckAndUnlockTiles();

            if (unlockedTiles.Count > 0)
            {
                // Trigger unlock animation for each tile
                foreach (var tile in unlockedTiles)
                {
                    boardView.AnimateUnlock(tile.row, tile.col);
                }

                // Force update tile positions to remove chains immediately if animation doesn't work
                // This will call UpdateChainOverlay for all tiles with the current locked state
                boardView.UpdateTilePositions(boardManager.GetCurrentBoard(), null);
            }
        }

        void CheckAndBreakIce()
        {
            List<(int row, int col)> brokeIce = boardManager.CheckAndBreakIce();

            if (brokeIce.Count > 0)
            {
                inputHandler.DisableInput();
                boardView.AnimateIceBreak(brokeIce, () => {
                    boardView.UpdateTilePositions(boardManager.GetCurrentBoard(), null);
                    inputHandler.EnableInput();
                });
            }
        }

    }
}