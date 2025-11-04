
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using PuzzleParty.Board;

namespace PuzzleParty.Board
{

    public class BoardManager
    {
        private Level level;

        private BoardTile[][] board;
        private BoardTile[][] initialBoard; // Board before scrambling
        private int movesLeft;
        private int totalMoves;

        public BoardManager(Level level)
        {
            this.level = level;
            this.totalMoves = level.Moves;
            this.movesLeft = level.Moves;
        }

        public int MovesLeft => movesLeft;
        public int TotalMoves => totalMoves;
        public bool IsSolved => CheckIfSolved();

        public void Init()
        {
            int cols = level.Columns;
            int rows = level.Rows;

            // Create initial board with ALL tiles (no holes) to show complete image
            board = new BoardTile[rows][];

            StringBuilder debug = new StringBuilder();
            for (int i = 0; i < rows; i++)
            {
                BoardTile[] col = new BoardTile[cols];
                for (int j = 0; j < cols; j++)
                {
                    BoardTile tile = new BoardTile();
                    tile.Column = j;
                    tile.Row = i;
                    col[j] = tile;
                    debug.Append("[" + i + ":" + j + "]");
                }
                board[i] = col;
                debug.AppendLine();
            }

            Debug.Log(debug);

            // Store initial board state (complete image, no holes)
            initialBoard = CloneBoard(board);

            // Scramble and add holes
            ScrambleBoard();
            PrintBoardSetup();



        }

        public BoardTile[][] GetCurrentBoard()
        {
            return (BoardTile[][])this.board.Clone();
        }

        public BoardTile[][] GetInitialBoard()
        {
            return (BoardTile[][])this.initialBoard.Clone();
        }

        private void ScrambleBoard()
        {
            // First, set up the board with holes at the end positions
            int totalPositions = level.Rows * level.Columns;

            // Identify where holes should be (last N positions)
            for (int i = 0; i < level.Rows; i++)
            {
                for (int j = 0; j < level.Columns; j++)
                {
                    int currentPosition = i * level.Columns + j;

                    // If this is one of the last positions, make it a hole
                    if (currentPosition >= totalPositions - level.Holes)
                    {
                        board[i][j] = null;
                    }
                }
            }

            // Try scrambling until we have less than 2 tiles in correct positions
            int maxAttempts = 10;
            int attempt = 0;
            System.Random rnd = new System.Random();

            while (attempt < maxAttempts)
            {
                // Make a copy of the current board to potentially restore
                BoardTile[][] boardCopy = CloneBoard(board);

                // Scramble with random valid moves
                int scrambleMoves = CalculateScrambleMoves();

                Debug.Log($"Scrambling attempt {attempt + 1} with {scrambleMoves} random moves");

                for (int moveCount = 0; moveCount < scrambleMoves; moveCount++)
                {
                    // Find all holes
                    List<(int row, int col)> holes = new List<(int row, int col)>();
                    for (int i = 0; i < level.Rows; i++)
                    {
                        for (int j = 0; j < level.Columns; j++)
                        {
                            if (board[i][j] == null)
                            {
                                holes.Add((i, j));
                            }
                        }
                    }

                    // For each hole, find movable tiles (tiles adjacent to the hole)
                    List<(int row, int col, MoveDirection direction)> possibleMoves = new List<(int row, int col, MoveDirection direction)>();

                    foreach (var hole in holes)
                    {
                        int holeRow = hole.row;
                        int holeCol = hole.col;

                        // Check all four directions for tiles that can move into this hole
                        // UP: tile below the hole can move up
                        if (holeRow + 1 < level.Rows && board[holeRow + 1][holeCol] != null)
                        {
                            possibleMoves.Add((holeRow + 1, holeCol, MoveDirection.UP));
                        }
                        // DOWN: tile above the hole can move down
                        if (holeRow - 1 >= 0 && board[holeRow - 1][holeCol] != null)
                        {
                            possibleMoves.Add((holeRow - 1, holeCol, MoveDirection.DOWN));
                        }
                        // LEFT: tile to the right of the hole can move left
                        if (holeCol + 1 < level.Columns && board[holeRow][holeCol + 1] != null)
                        {
                            possibleMoves.Add((holeRow, holeCol + 1, MoveDirection.LEFT));
                        }
                        // RIGHT: tile to the left of the hole can move right
                        if (holeCol - 1 >= 0 && board[holeRow][holeCol - 1] != null)
                        {
                            possibleMoves.Add((holeRow, holeCol - 1, MoveDirection.RIGHT));
                        }
                    }

                    // Pick a random valid move
                    if (possibleMoves.Count > 0)
                    {
                        int randomIndex = rnd.Next(possibleMoves.Count);
                        var move = possibleMoves[randomIndex];

                        // Execute the move (swap tile with hole)
                        BoardTile tileToMove = board[move.row][move.col];

                        int newRow = move.row;
                        int newCol = move.col;

                        switch (move.direction)
                        {
                            case MoveDirection.UP:
                                newRow--;
                                break;
                            case MoveDirection.DOWN:
                                newRow++;
                                break;
                            case MoveDirection.LEFT:
                                newCol--;
                                break;
                            case MoveDirection.RIGHT:
                                newCol++;
                                break;
                        }

                        // Swap tile with hole
                        board[newRow][newCol] = tileToMove;
                        board[move.row][move.col] = null;
                    }
                }

                // Check how many tiles are in correct positions
                int correctTiles = GetCorrectlyPlacedTilesCount();
                Debug.Log($"After scrambling: {correctTiles} tiles in correct position");

                if (correctTiles < 2)
                {
                    // Good scramble! Less than 2 tiles in correct positions
                    Debug.Log("Scramble successful!");
                    break;
                }

                // Not good enough, restore and try again
                board = boardCopy;
                attempt++;
            }

            if (attempt >= maxAttempts)
            {
                Debug.LogWarning($"Could not achieve scramble with less than 2 correct tiles after {maxAttempts} attempts. Using best attempt.");
            }
        }

        private int CalculateScrambleMoves()
        {
            // Calculate number of scramble moves based on board size and difficulty
            // Smaller boards need fewer moves, larger boards need more
            int totalTiles = (level.Rows * level.Columns) - level.Holes;

            // Base moves: 3-5 times the number of tiles
            // This ensures good scrambling without making it too chaotic
            int minMoves = totalTiles * 3;
            int maxMoves = totalTiles * 5;

            System.Random rnd = new System.Random();
            return rnd.Next(minMoves, maxMoves + 1);
        }

        private void PrintBoardSetup()
        {
            StringBuilder debug = new StringBuilder("BoardSetup:");
            debug.AppendLine();

            for (int i = 0; i < board.Length; i++)
            {
                BoardTile[] col = board[i];

                for (int j = 0; j < col.Length; j++)
                {

                    BoardTile tile = col[j];

                    if (tile != null)
                    {
                        debug.Append("[" + tile.Row + ":" + tile.Column + "]");
                    }
                    else
                    {
                        debug.Append("[null]");
                    }

                }
                debug.AppendLine();
            }

            Debug.Log(debug);
        }

        private List<BoardTile> getTileList()
        {
            List<BoardTile> boardTiles = new List<BoardTile>();

            int totalAdded = 0;
            for (int i = 0; i < level.Rows; i++)
            {


                for (int j = 0; j < level.Columns; j++)
                {
                    if (totalAdded < (level.Rows * level.Columns - level.Holes))
                    {

                        boardTiles.Add(board[i][j]);
                        totalAdded++;

                    }
                    else
                    {
                        break;
                    }
                }

            }
            return boardTiles;
        }

        private BoardTile[][] CloneBoard(BoardTile[][] original)
        {
            BoardTile[][] clone = new BoardTile[original.Length][];
            for (int i = 0; i < original.Length; i++)
            {
                clone[i] = new BoardTile[original[i].Length];
                for (int j = 0; j < original[i].Length; j++)
                {
                    if (original[i][j] != null)
                    {
                        clone[i][j] = new BoardTile
                        {
                            Row = original[i][j].Row,
                            Column = original[i][j].Column
                        };
                    }
                }
            }
            return clone;
        }

        public bool CanMoveTile(BoardTile tile, MoveDirection direction)
        {
            // Find tile's current position in board
            int currentRow = -1, currentCol = -1;

            for (int i = 0; i < board.Length; i++)
            {
                for (int j = 0; j < board[i].Length; j++)
                {
                    if (board[i][j] != null && board[i][j].Row == tile.Row && board[i][j].Column == tile.Column)
                    {
                        currentRow = i;
                        currentCol = j;
                        break;
                    }
                }
                if (currentRow != -1) break;
            }

            if (currentRow == -1)
            {
                Debug.Log("Tile not found in board");
                return false;
            }

            // Calculate target position based on direction
            int targetRow = currentRow;
            int targetCol = currentCol;

            switch (direction)
            {
                case MoveDirection.UP:
                    targetRow = currentRow - 1;
                    break;
                case MoveDirection.DOWN:
                    targetRow = currentRow + 1;
                    break;
                case MoveDirection.LEFT:
                    targetCol = currentCol - 1;
                    break;
                case MoveDirection.RIGHT:
                    targetCol = currentCol + 1;
                    break;
            }

            // Check if target is within bounds
            if (targetRow < 0 || targetRow >= board.Length || targetCol < 0 || targetCol >= board[0].Length)
            {
                return false;
            }

            // Check if target position is empty
            return board[targetRow][targetCol] == null;
        }

        public void MoveTile(BoardTile tile, MoveDirection direction)
        {
            // Find tile's current position
            int currentRow = -1, currentCol = -1;

            for (int i = 0; i < board.Length; i++)
            {
                for (int j = 0; j < board[i].Length; j++)
                {
                    if (board[i][j] != null && board[i][j].Row == tile.Row && board[i][j].Column == tile.Column)
                    {
                        currentRow = i;
                        currentCol = j;
                        break;
                    }
                }
                if (currentRow != -1) break;
            }

            // Calculate target position
            int targetRow = currentRow;
            int targetCol = currentCol;

            switch (direction)
            {
                case MoveDirection.UP:
                    targetRow = currentRow - 1;
                    break;
                case MoveDirection.DOWN:
                    targetRow = currentRow + 1;
                    break;
                case MoveDirection.LEFT:
                    targetCol = currentCol - 1;
                    break;
                case MoveDirection.RIGHT:
                    targetCol = currentCol + 1;
                    break;
            }

            // Move the tile
            board[targetRow][targetCol] = board[currentRow][currentCol];
            board[currentRow][currentCol] = null;

            // Decrement moves
            movesLeft--;

            Debug.Log($"Moved tile from [{currentRow},{currentCol}] to [{targetRow},{targetCol}]. Moves left: {movesLeft}");
        }

        private bool CheckIfSolved()
        {
            // Check if all tiles are in their correct positions
            for (int i = 0; i < board.Length; i++)
            {
                for (int j = 0; j < board[i].Length; j++)
                {
                    BoardTile tile = board[i][j];

                    // If there's a tile at this position
                    if (tile != null)
                    {
                        // Check if it's in the correct position
                        // A tile is correct if its Row/Column matches its board position
                        if (tile.Row != i || tile.Column != j)
                        {
                            return false; // Found a tile in wrong position
                        }
                    }
                }
            }

            // All tiles are in correct positions
            return true;
        }

        public int GetCorrectlyPlacedTilesCount()
        {
            int count = 0;

            for (int i = 0; i < board.Length; i++)
            {
                for (int j = 0; j < board[i].Length; j++)
                {
                    BoardTile tile = board[i][j];

                    if (tile != null && tile.Row == i && tile.Column == j)
                    {
                        count++;
                    }
                }
            }

            return count;
        }
    }
}