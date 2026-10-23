using System;
using System.Collections.Generic;

namespace Flowers.Board
{
    public static class BoardShuffler
    {
        private const int MaxAttempts = 200;

        /// <summary>
        /// Rearranges the existing flowers so there are no matches and at least one valid move.
        /// Returns where each flower went, so the view can animate it.
        /// In the rare case no good arrangement is found, the board is regenerated instead:
        /// <paramref name="regenerated"/> is true and the returned list is empty (the view should rebuild).
        /// </summary>
        public static List<FlowerMove> Shuffle(BoardGrid board, Random random, out bool regenerated)
        {
            var positions = new List<Position>(board.Width * board.Height);
            for (int x = 0; x < board.Width; x++)
                for (int y = 0; y < board.Height; y++)
                    if (board[x, y] != BoardGrid.Empty)
                        positions.Add(new Position(x, y));

            var original = board.Clone();
            var targets = new List<Position>(positions);

            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                ShuffleInPlace(targets, random);

                for (int i = 0; i < positions.Count; i++)
                    board[targets[i]] = original[positions[i]];

                if (!MatchFinder.HasAnyMatch(board) && MoveFinder.HasValidMove(board))
                {
                    regenerated = false;
                    var moves = new List<FlowerMove>(positions.Count);
                    for (int i = 0; i < positions.Count; i++)
                        if (positions[i] != targets[i])
                            moves.Add(new FlowerMove(positions[i], targets[i]));
                    return moves;
                }
            }

            BoardGenerator.FillWithoutMatches(board, random);
            regenerated = true;
            return new List<FlowerMove>();
        }

        private static void ShuffleInPlace(List<Position> list, Random random)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                var temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }
    }
}
