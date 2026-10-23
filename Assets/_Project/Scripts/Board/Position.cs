using System;

namespace Flowers.Board
{
    /// <summary>A cell on the board. X = column (0 = left), Y = row (0 = bottom).</summary>
    public readonly struct Position : IEquatable<Position>
    {
        public readonly int X;
        public readonly int Y;

        public Position(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool IsAdjacentTo(Position other)
        {
            int dx = Math.Abs(X - other.X);
            int dy = Math.Abs(Y - other.Y);
            return dx + dy == 1;
        }

        public bool Equals(Position other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is Position other && Equals(other);
        public override int GetHashCode() => (X * 397) ^ Y;
        public override string ToString() => $"({X},{Y})";

        public static bool operator ==(Position a, Position b) => a.Equals(b);
        public static bool operator !=(Position a, Position b) => !a.Equals(b);
    }
}
