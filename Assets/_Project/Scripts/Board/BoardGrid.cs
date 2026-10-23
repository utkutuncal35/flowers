using System;
using System.Text;

namespace Flowers.Board
{
    /// <summary>
    /// The grid of flowers. Each cell holds a flower type id (0..TypeCount-1) or <see cref="Empty"/>.
    /// Y = 0 is the bottom row; flowers fall towards lower Y.
    /// </summary>
    public sealed class BoardGrid
    {
        public const int Empty = -1;

        private readonly int[,] _cells;

        public int Width { get; }
        public int Height { get; }
        public int TypeCount { get; }

        public BoardGrid(int width, int height, int typeCount)
        {
            if (width < 1 || height < 1) throw new ArgumentException("BoardGrid size must be at least 1x1.");
            if (typeCount < 3) throw new ArgumentException("Need at least 3 flower types, otherwise matches can't be avoided.");

            Width = width;
            Height = height;
            TypeCount = typeCount;
            _cells = new int[width, height];
            Fill(Empty);
        }

        public int this[int x, int y]
        {
            get => _cells[x, y];
            set => _cells[x, y] = value;
        }

        public int this[Position p]
        {
            get => _cells[p.X, p.Y];
            set => _cells[p.X, p.Y] = value;
        }

        public bool IsInside(Position p) => p.X >= 0 && p.X < Width && p.Y >= 0 && p.Y < Height;

        public void Swap(Position a, Position b)
        {
            int temp = this[a];
            this[a] = this[b];
            this[b] = temp;
        }

        public void Fill(int value)
        {
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    _cells[x, y] = value;
        }

        public BoardGrid Clone()
        {
            var copy = new BoardGrid(Width, Height, TypeCount);
            Array.Copy(_cells, copy._cells, _cells.Length);
            return copy;
        }

        /// <summary>Top row first, one char per cell: 'A'.. for types, '.' for empty. Handy in test failures.</summary>
        public override string ToString()
        {
            var sb = new StringBuilder();
            for (int y = Height - 1; y >= 0; y--)
            {
                for (int x = 0; x < Width; x++)
                    sb.Append(_cells[x, y] == Empty ? '.' : (char)('A' + _cells[x, y]));
                if (y > 0) sb.Append('\n');
            }
            return sb.ToString();
        }
    }
}
