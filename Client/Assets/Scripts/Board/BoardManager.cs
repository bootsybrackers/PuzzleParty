
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using PuzzleParty.Levels;

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

        public void AddMoves(int amount)
        {
            movesLeft += amount;
            totalMoves += amount;
            Debug.Log($"Added {amount} moves. Moves left: {movesLeft}, Total moves: {totalMoves}");
        }

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

                    // Don't set IsLocked here - locks are applied AFTER scramble
                    // based on board POSITION, not tile identity
                    tile.IsLocked = false;

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

            // Apply locks to tiles at locked POSITIONS (after scramble)
            ApplyLocksToPositions();

            // Apply ice to all tiles in ice rows (after scramble)
            ApplyIceToRows();

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
            HashSet<int> iceRowSet = GetIceRowSet();

            var icedRows = new List<int>();
            var nonIcedRows = new List<int>();
            for (int i = 0; i < level.Rows; i++)
            {
                if (iceRowSet.Contains(i)) icedRows.Add(i);
                else nonIcedRows.Add(i);
            }

            // Place holes: exactly 1 in the non-iced section, rest in iced section
            PlaceHoles(nonIcedRows, icedRows);

            // Scramble each section independently — not animated, not visible to player
            if (nonIcedRows.Count > 0)
                ScrambleSection(nonIcedRows);
            if (icedRows.Count > 0)
                ScrambleIcedSection(icedRows);
        }

        private void PlaceHoles(List<int> nonIcedRows, List<int> icedRows)
        {
            // Step 1: Place all holes at the last N global positions (row-major, bottom-right first)
            int placed = 0;
            for (int i = level.Rows - 1; i >= 0 && placed < level.Holes; i--)
                for (int j = level.Columns - 1; j >= 0 && placed < level.Holes; j--)
                {
                    board[i][j] = null;
                    placed++;
                }

            // Step 2: If the non-iced section has no holes after placement, create one.
            // This happens when ice is at the bottom — holes land in the iced rows.
            // Take the bottom-right tile of the non-iced section and move it into the
            // last iced hole, leaving a hole in the non-iced section instead.
            if (nonIcedRows.Count == 0 || icedRows.Count == 0) return;

            bool hasHoleInNonIced = false;
            foreach (int r in nonIcedRows)
                for (int c = 0; c < level.Columns; c++)
                    if (board[r][c] == null) { hasHoleInNonIced = true; break; }

            if (hasHoleInNonIced) return; // Non-iced already has a hole, nothing to do

            // Find the last (bottom-right) tile in the non-iced section
            int lastNonIcedRow = -1, lastNonIcedCol = -1;
            for (int i = nonIcedRows.Count - 1; i >= 0 && lastNonIcedRow == -1; i--)
                for (int j = level.Columns - 1; j >= 0 && lastNonIcedRow == -1; j--)
                    if (board[nonIcedRows[i]][j] != null)
                    {
                        lastNonIcedRow = nonIcedRows[i];
                        lastNonIcedCol = j;
                    }

            // Find the last hole in the iced section to place the non-iced tile into
            int holeRow = -1, holeCol = -1;
            for (int i = icedRows.Count - 1; i >= 0 && holeRow == -1; i--)
                for (int j = level.Columns - 1; j >= 0 && holeRow == -1; j--)
                    if (board[icedRows[i]][j] == null)
                    {
                        holeRow = icedRows[i];
                        holeCol = j;
                    }

            if (lastNonIcedRow == -1 || holeRow == -1)
            {
                Debug.LogWarning("PlaceHoles: could not find non-iced tile or iced hole to swap");
                return;
            }

            // Move the non-iced tile into the iced hole, leaving a hole in the non-iced section
            board[holeRow][holeCol] = board[lastNonIcedRow][lastNonIcedCol];
            board[lastNonIcedRow][lastNonIcedCol] = null;
        }

        /// <summary>
        /// Scrambles tiles within the given rows only — holes never cross section boundaries.
        /// </summary>
        private void ScrambleSection(List<int> rowIndices)
        {
            var rowSet = new HashSet<int>(rowIndices);
            System.Random rnd = new System.Random();

            int tilesInSection = 0;
            foreach (int r in rowIndices)
                for (int c = 0; c < level.Columns; c++)
                    if (board[r][c] != null) tilesInSection++;

            int scrambleMoves = tilesInSection * 4;

            int maxAttempts = 15;
            BoardTile[][] bestBoard = null;
            int lowestCorrect = int.MaxValue;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                BoardTile[][] boardCopy = CloneBoard(board);
                (int row, int col, MoveDirection dir)? lastMove = null;

                for (int m = 0; m < scrambleMoves; m++)
                {
                    var holes = new List<(int row, int col)>();
                    foreach (int r in rowIndices)
                        for (int c = 0; c < level.Columns; c++)
                            if (board[r][c] == null) holes.Add((r, c));

                    if (holes.Count == 0) break;

                    var moves = new List<(int row, int col, MoveDirection dir)>();
                    foreach (var (hr, hc) in holes)
                    {
                        if (rowSet.Contains(hr + 1) && hr + 1 < level.Rows && board[hr + 1][hc] != null)
                            moves.Add((hr + 1, hc, MoveDirection.UP));
                        if (rowSet.Contains(hr - 1) && hr - 1 >= 0 && board[hr - 1][hc] != null)
                            moves.Add((hr - 1, hc, MoveDirection.DOWN));
                        if (hc + 1 < level.Columns && board[hr][hc + 1] != null)
                            moves.Add((hr, hc + 1, MoveDirection.LEFT));
                        if (hc - 1 >= 0 && board[hr][hc - 1] != null)
                            moves.Add((hr, hc - 1, MoveDirection.RIGHT));
                    }

                    if (lastMove.HasValue)
                    {
                        MoveDirection opp = GetOppositeDirection(lastMove.Value.dir);
                        moves.RemoveAll(mv => mv.row == lastMove.Value.row && mv.col == lastMove.Value.col && mv.dir == opp);
                    }

                    if (moves.Count == 0) break;

                    var move = moves[rnd.Next(moves.Count)];
                    int newRow = move.row + (move.dir == MoveDirection.UP ? -1 : move.dir == MoveDirection.DOWN ? 1 : 0);
                    int newCol = move.col + (move.dir == MoveDirection.LEFT ? -1 : move.dir == MoveDirection.RIGHT ? 1 : 0);

                    board[newRow][newCol] = board[move.row][move.col];
                    board[move.row][move.col] = null;
                    lastMove = move;
                }

                int correct = CountCorrectInSection(rowIndices);
                if (correct < lowestCorrect)
                {
                    lowestCorrect = correct;
                    bestBoard = CloneBoard(board);
                }

                if (correct < 2) break;
                board = boardCopy;
            }

            if (lowestCorrect >= 2 && bestBoard != null)
            {
                Debug.LogWarning($"ScrambleSection: best attempt had {lowestCorrect} correct tiles in section. Using it anyway.");
                board = bestBoard;
            }
        }

        /// <summary>
        /// Scrambles the iced section. When the section has no holes (ice on top rows,
        /// all holes fell to non-iced rows), a temporary blank is created so sliding
        /// scramble can run, then the saved tile is restored at the blank's final position.
        /// With 2+ blanks available in the non-iced section after ice breaks, any resulting
        /// permutation is solvable in phase 2.
        /// </summary>
        private void ScrambleIcedSection(List<int> icedRows)
        {
            bool hasHole = false;
            foreach (int r in icedRows)
            {
                for (int c = 0; c < level.Columns; c++)
                    if (board[r][c] == null) { hasHole = true; break; }
                if (hasHole) break;
            }

            if (hasHole)
            {
                ScrambleSection(icedRows);
                return;
            }

            // No hole in section — temporarily remove the last iced tile to create one
            int tempRow = -1, tempCol = -1;
            for (int i = icedRows.Count - 1; i >= 0 && tempRow == -1; i--)
                for (int j = level.Columns - 1; j >= 0 && tempRow == -1; j--)
                    if (board[icedRows[i]][j] != null)
                    {
                        tempRow = icedRows[i];
                        tempCol = j;
                    }

            if (tempRow == -1) return;

            BoardTile savedTile = board[tempRow][tempCol];
            board[tempRow][tempCol] = null;

            ScrambleSection(icedRows);

            // Find where the blank ended up and restore the saved tile there
            int holeRow = -1, holeCol = -1;
            foreach (int r in icedRows)
            {
                for (int c = 0; c < level.Columns; c++)
                    if (board[r][c] == null) { holeRow = r; holeCol = c; break; }
                if (holeRow != -1) break;
            }

            if (holeRow != -1)
                board[holeRow][holeCol] = savedTile;
            else
                board[tempRow][tempCol] = savedTile; // fallback
        }

        private int CountCorrectInSection(List<int> rowIndices)
        {
            int count = 0;
            foreach (int r in rowIndices)
                for (int c = 0; c < level.Columns; c++)
                {
                    BoardTile tile = board[r][c];
                    if (tile != null && tile.Row == r && tile.Column == c)
                        count++;
                }
            return count;
        }

        private HashSet<int> GetIceRowSet()
        {
            var set = new HashSet<int>();
            if (level.IceRows != null)
                foreach (int r in level.IceRows)
                    set.Add(r);
            return set;
        }

        private MoveDirection GetOppositeDirection(MoveDirection direction)
        {
            switch (direction)
            {
                case MoveDirection.UP:
                    return MoveDirection.DOWN;
                case MoveDirection.DOWN:
                    return MoveDirection.UP;
                case MoveDirection.LEFT:
                    return MoveDirection.RIGHT;
                case MoveDirection.RIGHT:
                    return MoveDirection.LEFT;
                default:
                    return direction;
            }
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
                            Column = original[i][j].Column,
                            IsLocked = original[i][j].IsLocked,
                            IsIced = original[i][j].IsIced
                        };
                    }
                }
            }
            return clone;
        }

        public bool CanMoveTile(BoardTile tile, MoveDirection direction)
        {
            if (tile.IsLocked)
                return false;

            if (tile.IsIced)
                return false;

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

        /// <summary>
        /// Applies locks to tiles at the locked POSITIONS (not by tile identity).
        /// Called after scramble so locks are at fixed board positions.
        /// </summary>
        private void ApplyLocksToPositions()
        {
            // Clear any existing locks first
            for (int i = 0; i < board.Length; i++)
            {
                for (int j = 0; j < board[i].Length; j++)
                {
                    if (board[i][j] != null)
                    {
                        board[i][j].IsLocked = false;
                    }
                }
            }

            // Apply locks to tiles at the locked positions
            foreach (var lockedPos in level.LockedTiles)
            {
                int row = lockedPos.row;
                int col = lockedPos.column;

                if (row >= 0 && row < board.Length && col >= 0 && col < board[row].Length)
                {
                    BoardTile tile = board[row][col];
                    if (tile != null)
                    {
                        tile.IsLocked = true;
                    }
                    else
                    {
                        // Hole is at the locked position — swap with the nearest non-hole tile
                        bool swapped = false;
                        for (int i = 0; i < board.Length && !swapped; i++)
                        {
                            for (int j = 0; j < board[i].Length && !swapped; j++)
                            {
                                if (board[i][j] != null)
                                {
                                    board[row][col] = board[i][j];
                                    board[i][j] = null;
                                    board[row][col].IsLocked = true;
                                    swapped = true;
                                }
                            }
                        }
                        if (!swapped)
                            Debug.LogWarning($"Could not apply lock at [{row},{col}]: no tiles to swap with");
                    }
                }
            }
        }

        /// <summary>
        /// Check locked tiles and unlock any when 4 tiles are in correct positions
        /// Returns list of tiles that were unlocked
        /// </summary>
        public List<(int row, int col)> CheckAndUnlockTiles()
        {
            List<(int row, int col)> unlockedTiles = new List<(int row, int col)>();

            // Count how many tiles are in correct positions
            int correctTilesCount = GetCorrectlyPlacedTilesCount();

            Debug.Log($"CheckAndUnlockTiles: {correctTilesCount} tiles are correctly placed");

            // If 4 or more tiles are correct, unlock all locked tiles
            if (correctTilesCount >= 4)
            {
                for (int i = 0; i < board.Length; i++)
                {
                    for (int j = 0; j < board[i].Length; j++)
                    {
                        BoardTile tile = board[i][j];

                        if (tile != null && tile.IsLocked)
                        {
                            tile.IsLocked = false;
                            unlockedTiles.Add((i, j));
                            Debug.Log($"Unlocked tile at [{i},{j}] (4+ tiles are correct)");
                        }
                    }
                }
            }

            return unlockedTiles;
        }

        private void ApplyIceToRows()
        {
            if (level.IceRows == null || level.IceRows.Count == 0)
                return;

            HashSet<int> iceRowSet = GetIceRowSet();

            for (int i = 0; i < board.Length; i++)
                for (int j = 0; j < board[i].Length; j++)
                    if (board[i][j] != null)
                        board[i][j].IsIced = iceRowSet.Contains(i);
        }

        /// <summary>
        /// Checks if all non-iced tiles are in their correct positions.
        /// If so, clears all ice and returns the board positions that were iced.
        /// </summary>
        public List<(int row, int col)> CheckAndBreakIce()
        {
            // Check if there is any ice at all
            bool hasIce = false;
            for (int i = 0; i < board.Length; i++)
                for (int j = 0; j < board[i].Length; j++)
                    if (board[i][j] != null && board[i][j].IsIced)
                        { hasIce = true; break; }

            if (!hasIce)
                return new List<(int row, int col)>();

            // Check all non-iced tiles are correctly placed
            for (int i = 0; i < board.Length; i++)
            {
                for (int j = 0; j < board[i].Length; j++)
                {
                    BoardTile tile = board[i][j];
                    if (tile != null && !tile.IsIced)
                    {
                        if (tile.Row != i || tile.Column != j)
                            return new List<(int row, int col)>();
                    }
                }
            }

            // All non-iced tiles are correct — break the ice
            List<(int row, int col)> icedPositions = new List<(int row, int col)>();
            for (int i = 0; i < board.Length; i++)
            {
                for (int j = 0; j < board[i].Length; j++)
                {
                    BoardTile tile = board[i][j];
                    if (tile != null && tile.IsIced)
                    {
                        tile.IsIced = false;
                        icedPositions.Add((i, j));
                    }
                }
            }

            return icedPositions;
        }
    }
}