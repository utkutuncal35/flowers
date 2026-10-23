using System.Collections.Generic;

namespace Flowers.Board
{
    /// <summary>A straight horizontal or vertical run of 3+ flowers of the same type.</summary>
    public sealed class Match
    {
        public int Type { get; }
        public bool IsHorizontal { get; }
        public IReadOnlyList<Position> Positions { get; }
        public int Length => Positions.Count;

        public Match(int type, bool isHorizontal, IReadOnlyList<Position> positions)
        {
            Type = type;
            IsHorizontal = isHorizontal;
            Positions = positions;
        }
    }
}
