using System.Collections.Generic;

namespace Flowers.Board
{
    public static class MatchFinder
    {
        public const int MinMatchLength = 3;

        /// <summary>
        /// All horizontal and vertical runs of 3+. An L/T shape shows up as two matches sharing a cell;
        /// use <see cref="CollectPositions"/> to get each cell once.
        /// </summary>
        public static List<Match> FindMatches(BoardGrid board)
        {
            var matches = new List<Match>();

            for (int y = 0; y < board.Height; y++)
                ScanLine(board, matches, new Position(0, y), 1, 0, board.Width, isHorizontal: true);

            for (int x = 0; x < board.Width; x++)
                ScanLine(board, matches, new Position(x, 0), 0, 1, board.Height, isHorizontal: false);

            return matches;
        }

        public static bool HasAnyMatch(BoardGrid board)
        {
            for (int x = 0; x < board.Width; x++)
                for (int y = 0; y < board.Height; y++)
                    if (IsPartOfMatch(board, new Position(x, y)))
                        return true;
            return false;
        }

        /// <summary>True if the flower at <paramref name="p"/> is in a horizontal or vertical run of 3+.</summary>
        public static bool IsPartOfMatch(BoardGrid board, Position p)
        {
            int type = board[p];
            if (type == BoardGrid.Empty) return false;

            int horizontal = 1 + CountSame(board, p, 1, 0, type) + CountSame(board, p, -1, 0, type);
            if (horizontal >= MinMatchLength) return true;

            int vertical = 1 + CountSame(board, p, 0, 1, type) + CountSame(board, p, 0, -1, type);
            return vertical >= MinMatchLength;
        }

        public static HashSet<Position> CollectPositions(IEnumerable<Match> matches)
        {
            var set = new HashSet<Position>();
            foreach (var match in matches)
                foreach (var p in match.Positions)
                    set.Add(p);
            return set;
        }

        private static int CountSame(BoardGrid board, Position from, int dx, int dy, int type)
        {
            int count = 0;
            var p = new Position(from.X + dx, from.Y + dy);
            while (board.IsInside(p) && board[p] == type)
            {
                count++;
                p = new Position(p.X + dx, p.Y + dy);
            }
            return count;
        }

        private static void ScanLine(BoardGrid board, List<Match> matches, Position start, int dx, int dy,
            int length, bool isHorizontal)
        {
            int runStart = 0;
            for (int i = 1; i <= length; i++)
            {
                int startType = board[start.X + runStart * dx, start.Y + runStart * dy];
                bool runContinues = i < length && board[start.X + i * dx, start.Y + i * dy] == startType;
                if (runContinues) continue;

                int runLength = i - runStart;
                if (startType != BoardGrid.Empty && runLength >= MinMatchLength)
                {
                    var positions = new List<Position>(runLength);
                    for (int j = runStart; j < i; j++)
                        positions.Add(new Position(start.X + j * dx, start.Y + j * dy));
                    matches.Add(new Match(startType, isHorizontal, positions));
                }
                runStart = i;
            }
        }
    }
}
