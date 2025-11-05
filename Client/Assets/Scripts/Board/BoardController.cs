using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
        // Start is called before the first frame update

        [SerializeField]
        private LevelUIElements uiElements;

        private ILevelService levelService;
        private IProgressionService progressionService;
        private ISceneLoader sceneLoader;
        private BoardView boardView;
        private BoardManager boardManager;
        private Level currentLevel;
        private bool isInputEnabled = false;
        private HashSet<string> tilesMarkedCorrect = new HashSet<string>();
        private bool isFirstCheck = true; // Track if this is the first check after scramble

        private Vector3 mouseDownPosition;
        private bool isDragging = false;
        private float dragThreshold = 0.2f;

        public BoardController()
        {
        }

        void Start()
        {
            //Camera camera = GetComponent<Camera>();
            float pixelsPerUnitOnScreen = Screen.height / (Camera.main.orthographicSize * 2);
            Debug.Log("PPU:" + pixelsPerUnitOnScreen);
            this.AddComponent<BoardView>();
            levelService = ServiceLocator.GetInstance().Get<LevelService>();
            progressionService = ServiceLocator.GetInstance().Get<ProgressionService>();
            sceneLoader = ServiceLocator.GetInstance().Get<SceneLoader>();
            SetupLevel();
        }

        // Update is called once per frame
        void Update()
        {
            if (!isInputEnabled) return;

            HandleInput();
        }

        void HandleInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                mouseDownPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mouseDownPosition.z = 0; // Ensure we're on the same z-plane as tiles
                Debug.Log($"Mouse down at screen: {Input.mousePosition}, world: {mouseDownPosition}");
                isDragging = true;
            }

            if (Input.GetMouseButtonUp(0) && isDragging)
            {
                Vector3 mouseUpPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mouseUpPosition.z = 0; // Ensure we're on the same z-plane as tiles
                Debug.Log($"Mouse up at screen: {Input.mousePosition}, world: {mouseUpPosition}");
                ProcessSwipe(mouseDownPosition, mouseUpPosition);
                isDragging = false;
            }
        }

        void ProcessSwipe(Vector3 startPos, Vector3 endPos)
        {
            // Find which tile was clicked
            BoardTile clickedTile = boardView.GetTileAtPosition(startPos);

            if (clickedTile == null)
            {
                Debug.Log("No tile clicked");
                return;
            }

            // Calculate swipe direction
            Vector3 swipeVector = endPos - startPos;
            float swipeDistance = swipeVector.magnitude;

            if (swipeDistance < dragThreshold)
            {
                Debug.Log("Swipe too short");
                return;
            }

            // Determine primary direction
            MoveDirection direction = GetSwipeDirection(swipeVector);

            // Ask BoardManager if move is allowed
            if (boardManager.CanMoveTile(clickedTile, direction))
            {
                Debug.Log($"Moving tile [{clickedTile.Row},{clickedTile.Column}] in direction {direction}");
                boardManager.MoveTile(clickedTile, direction);

                // Update the view based on new board state
                // Check for correct tiles AFTER animation completes
                boardView.UpdateTilePositions(boardManager.GetCurrentBoard(), () => {
                    // Animation complete, now check for correctly placed tiles
                    CheckForCorrectlyPlacedTiles();

                    // Check if puzzle is solved
                    if (boardManager.IsSolved)
                    {
                        OnPuzzleSolved();
                    }
                    // Check if out of moves
                    else if (boardManager.MovesLeft <= 0)
                    {
                        OnOutOfMoves();
                    }
                });

                boardView.UpdateMovesDisplay(boardManager.MovesLeft);
            }
            else
            {
                Debug.Log($"Cannot move tile [{clickedTile.Row},{clickedTile.Column}] in direction {direction}");
            }
        }

        MoveDirection GetSwipeDirection(Vector3 swipeVector)
        {
            float absX = Mathf.Abs(swipeVector.x);
            float absY = Mathf.Abs(swipeVector.y);

            if (absX > absY)
            {
                // Horizontal swipe
                return swipeVector.x > 0 ? MoveDirection.RIGHT : MoveDirection.LEFT;
            }
            else
            {
                // Vertical swipe
                return swipeVector.y > 0 ? MoveDirection.UP : MoveDirection.DOWN;
            }
        }

        void SetupLevel()
        {
            currentLevel = levelService.GetNextLevel();
            boardManager = new BoardManager(currentLevel);
            boardManager.Init();

            boardView = GetComponent<BoardView>();
            boardView.Setup(currentLevel, boardManager.GetInitialBoard(), uiElements);

            // Initialize moves display
            boardView.UpdateMovesDisplay(boardManager.MovesLeft);

            // Hide game end overlay initially
            boardView.HideGameEndOverlay();

            // Reset tracking state
            tilesMarkedCorrect.Clear();
            isFirstCheck = true;
            isInputEnabled = false;

            // Start the animation sequence with welcome text and scaling
            boardView.StartAnimation(() => {
                // After showing complete puzzle, animate the scramble
                boardView.AnimateScramble(boardManager.GetCurrentBoard(), () => {
                    Debug.Log("Animation sequence complete - game ready to play!");

                    // Do the first check to mark initially correct tiles without showing particles
                    CheckForCorrectlyPlacedTiles();

                    // Now disable first check flag so future checks will show particles
                    isFirstCheck = false;

                    // Enable input after animations complete
                    isInputEnabled = true;
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

                            // Only show particle effect if this is not the first check
                            if (!isFirstCheck)
                            {
                                boardView.ShowCorrectTileEffect(tile.Row, tile.Column);
                                Debug.Log($"Tile [{tile.Row},{tile.Column}] placed correctly for the first time!");
                            }
                            else
                            {
                                Debug.Log($"Tile [{tile.Row},{tile.Column}] already correct after scramble");
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
        }

        void OnPuzzleSolved()
        {
            // Disable input
            isInputEnabled = false;

            Debug.Log("Puzzle solved! Congratulations!");

            // Calculate coins earned (1 coin per remaining move)
            int coinsEarned = boardManager.MovesLeft;

            // Save progression
            Progression progression = progressionService.GetProgression();
            progression.coins += coinsEarned;

            // Update last beaten level if this is the next level
            progression.lastBeatenLevel = currentLevel.Id;

            progressionService.SaveProgression(progression);

            Debug.Log($"Earned {coinsEarned} coins! Total coins: {progression.coins}");
            Debug.Log($"About to show overlay. boardView is null: {boardView == null}");

            // Fill holes to show complete image, then show success overlay
            boardView.FillHolesAndShowOverlay(
                boardManager.GetInitialBoard(),
                success: true,
                coinsEarned: coinsEarned,
                onRestart: RestartLevel,
                onNextLevel: LoadNextLevel
            );

            Debug.Log("FillHolesAndShowOverlay called");
        }

        void OnOutOfMoves()
        {
            // Disable input
            isInputEnabled = false;

            Debug.Log("Out of moves! Game Over!");

            // Show failure overlay
            boardView.ShowGameEndOverlay(
                success: false,
                coinsEarned: 0,
                onRestart: RestartLevel,
                onNextLevel: LoadNextLevel
            );
        }

        void RestartLevel()
        {
            Debug.Log("Restarting level...");

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

        void LoadNextLevel()
        {
            Debug.Log("Loading next level...");

            // Go back to main menu to select next level
            // (You can change this to load the next level directly if you prefer)
            sceneLoader.LoadMainMenu();
        }

    }
}