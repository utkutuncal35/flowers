using System.Collections.Generic;

namespace Flowers.Board
{
    public readonly struct Move
    {
        public readonly Position A;
        public readonly Position B;

        public Move(Position a, Position b)
        {
            A = a;
            B = b;
        }

        public override string ToString() => $"{A}<->{B}";
    }

    public static class MoveFinder
    {
        /// <summary>
        /// True if swapping a and b would create a match. Both must be adjacent, hold different flowers and be
        /// swappable (not a pot, void or vine-wrapped cell).
        /// </summary>
        public static bool IsValidSwap(BoardGrid board, Position a, Position b)
        {
            if (!board.IsInside(a) || !board.IsInside(b) || !a.IsAdjacentTo(b)) return false;
            if (!board.CanSwap(a) || !board.CanSwap(b)) return false;
            if (board[a] == board[b]) return false;

            board.Swap(a, b);
            bool creates = MatchFinder.IsPartOfMatch(board, a) || MatchFinder.IsPartOfMatch(board, b);
            board.Swap(a, b);
            return creates;
        }

        public static bool HasValidMove(BoardGrid board) => FindFirstValidMove(board).HasValue;

        /// <summary>The first valid move found, or null. Can be used later for a "hint" feature.</summary>
        public static Move? FindFirstValidMove(BoardGrid board)
        {
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var p = new Position(x, y);
                    var right = new Position(x + 1, y);
                    var up = new Position(x, y + 1);
                    if (IsValidSwap(board, p, right)) return new Move(p, right);
                    if (IsValidSwap(board, p, up)) return new Move(p, up);
                }
            }
            return null;
        }

        public static List<Move> FindAllValidMoves(BoardGrid board)
        {
            var moves = new List<Move>();
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var p = new Position(x, y);
                    var right = new Position(x + 1, y);
                    var up = new Position(x, y + 1);
                    if (IsValidSwap(board, p, right)) moves.Add(new Move(p, right));
                    if (IsValidSwap(board, p, up)) moves.Add(new Move(p, up));
                }
            }
            return moves;
        }
    }
}
