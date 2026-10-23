using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Flowers.Board.Tests
{
    /// <summary>Builds boards from text so tests read like pictures.</summary>
    public static class TestBoards
    {
        public const int DefaultTypeCount = 5;

        /// <summary>
        /// Rows are given top row first, the way they look on screen. 'A'..'E' are flower types, '.' is empty.
        /// </summary>
        public static BoardGrid FromRows(params string[] rows) => FromRows(DefaultTypeCount, rows);

        public static BoardGrid FromRows(int typeCount, params string[] rows)
        {
            int height = rows.Length;
            int width = rows[0].Length;
            var board = new BoardGrid(width, height, typeCount);

            for (int row = 0; row < height; row++)
            {
                if (rows[row].Length != width) throw new ArgumentException("All rows must have the same length.");
                int y = height - 1 - row;
                for (int x = 0; x < width; x++)
                {
                    char c = rows[row][x];
                    board[x, y] = c == '.' ? BoardGrid.Empty : c - 'A';
                }
            }
            return board;
        }

        public static Position P(int x, int y) => new Position(x, y);

        public static void AssertFullAndInRange(BoardGrid board)
        {
            for (int x = 0; x < board.Width; x++)
                for (int y = 0; y < board.Height; y++)
                {
                    int type = board[x, y];
                    Assert.That(type, Is.InRange(0, board.TypeCount - 1), $"Bad cell at ({x},{y}):\n{board}");
                }
        }

        public static void AssertPlayable(BoardGrid board)
        {
            AssertFullAndInRange(board);
            Assert.That(MatchFinder.HasAnyMatch(board), Is.False, $"Board has a leftover match:\n{board}");
            Assert.That(MoveFinder.HasValidMove(board), Is.True, $"Board has no valid move:\n{board}");
        }

        public static Dictionary<int, int> CountTypes(BoardGrid board)
        {
            var counts = new Dictionary<int, int>();
            for (int x = 0; x < board.Width; x++)
                for (int y = 0; y < board.Height; y++)
                {
                    counts.TryGetValue(board[x, y], out int c);
                    counts[board[x, y]] = c + 1;
                }
            return counts;
        }
    }
}
