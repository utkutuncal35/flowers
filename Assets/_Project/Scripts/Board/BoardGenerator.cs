using System;

namespace Flowers.Board
{
    public static class BoardGenerator
    {
        private const int MaxAttempts = 200;

        /// <summary>A full rectangular board with no existing matches and at least one valid move.</summary>
        public static BoardGrid Generate(int width, int height, int typeCount, Random random) =>
            Generate(BoardLayout.Rectangle(width, height), typeCount, random);

        /// <summary>A board with the layout's shape and obstacles, no existing matches and at least one valid move.</summary>
        public static BoardGrid Generate(BoardLayout layout, int typeCount, Random random)
        {
            var board = new BoardGrid(layout.Width, layout.Height, typeCount);
            layout.ApplyTo(board);
            FillWithoutMatches(board, random);
            return board;
        }

        /// <summary>
        /// Puts a flower in every cell that can hold one (obstacles stay as they are). Picks each flower so it
        /// can't complete a run with the two flowers to its left or the two below it, then retries until the
        /// board has a valid move.
        /// </summary>
        public static void FillWithoutMatches(BoardGrid board, Random random)
        {
            var allowed = new int[board.TypeCount];

            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    for (int y = 0; y < board.Height; y++)
                    {
                        if (!board.CanHoldFlower(new Position(x, y)))
                        {
                            board[x, y] = BoardGrid.Empty;
                            continue;
                        }

                        int count = 0;
                        for (int type = 0; type < board.TypeCount; type++)
                            if (!WouldCompleteRun(board, x, y, type))
                                allowed[count++] = type;

                        board[x, y] = allowed[random.Next(count)];
                    }
                }

                if (MoveFinder.HasValidMove(board)) return;
            }

            throw new InvalidOperationException(
                $"Could not generate a {board.Width}x{board.Height} board with {board.TypeCount} types that has a valid move.");
        }

        // Only looks left and down, because cells are filled column by column from the bottom.
        private static bool WouldCompleteRun(BoardGrid board, int x, int y, int type)
        {
            bool left = x >= 2 && board[x - 1, y] == type && board[x - 2, y] == type;
            bool down = y >= 2 && board[x, y - 1] == type && board[x, y - 2] == type;
            return left || down;
        }
    }
}
