using System;
using System.Text;

namespace Flowers.Board
{
    /// <summary>
    /// The grid of flowers. Each cell holds a flower type id (0..TypeCount-1) or <see cref="Empty"/>.
    /// Y = 0 is the bottom row; flowers fall towards lower Y.
    ///
    /// Cells can also carry obstacles:
    /// - Void: not part of the board at all (shaped boards).
    /// - Pot (1-2 layers): fills the cell instead of a flower; breaks from pops next to it.
    /// - Vine: wraps the flower in the cell; it can't be swapped or fall, but still matches.
    ///   Freed when it is part of a match or a flower next to it pops.
    /// - Soil: ground under the flower; cleared when the flower on it pops.
    /// Void, pot and vine cells are "fixed": nothing moves into, out of, or through them.
    /// </summary>
    public sealed class BoardGrid
    {
        public const int Empty = -1;

        private readonly int[,] _cells;
        private readonly bool[,] _void;
        private readonly int[,] _pot;
        private readonly bool[,] _vine;
        private readonly bool[,] _soil;

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
            _void = new bool[width, height];
            _pot = new int[width, height];
            _vine = new bool[width, height];
            _soil = new bool[width, height];
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

        // ---------- Obstacles ----------

        public bool IsVoid(Position p) => _void[p.X, p.Y];
        public int PotLayers(Position p) => _pot[p.X, p.Y];
        public bool HasVine(Position p) => _vine[p.X, p.Y];
        public bool HasSoil(Position p) => _soil[p.X, p.Y];

        public void SetVoid(Position p, bool value) => _void[p.X, p.Y] = value;
        public void SetPot(Position p, int layers) => _pot[p.X, p.Y] = Math.Max(0, layers);
        public void SetVine(Position p, bool value) => _vine[p.X, p.Y] = value;
        public void SetSoil(Position p, bool value) => _soil[p.X, p.Y] = value;

        /// <summary>Nothing moves into, out of, or through this cell.</summary>
        public bool IsFixed(Position p) => _void[p.X, p.Y] || _pot[p.X, p.Y] > 0 || _vine[p.X, p.Y];

        /// <summary>The cell can hold a flower (not void, no pot). A vine cell is open: it holds its flower.</summary>
        public bool CanHoldFlower(Position p) => !_void[p.X, p.Y] && _pot[p.X, p.Y] == 0;

        /// <summary>The player may swap the flower in this cell.</summary>
        public bool CanSwap(Position p) => IsInside(p) && !IsFixed(p) && this[p] != Empty;

        public int CountPots() => Count((x, y) => _pot[x, y] > 0);
        public int CountVines() => Count((x, y) => _vine[x, y]);
        public int CountSoil() => Count((x, y) => _soil[x, y]);

        // ---------- Helpers ----------

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
            Array.Copy(_void, copy._void, _void.Length);
            Array.Copy(_pot, copy._pot, _pot.Length);
            Array.Copy(_vine, copy._vine, _vine.Length);
            Array.Copy(_soil, copy._soil, _soil.Length);
            return copy;
        }

        private int Count(Func<int, int, bool> predicate)
        {
            int count = 0;
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    if (predicate(x, y)) count++;
            return count;
        }

        /// <summary>
        /// Top row first, one char per cell: 'A'.. for flower types, '.' for empty, '#' void, 'P' pot.
        /// Vine-wrapped flowers are lower case ('a'..). Handy in test failures.
        /// </summary>
        public override string ToString()
        {
            var sb = new StringBuilder();
            for (int y = Height - 1; y >= 0; y--)
            {
                for (int x = 0; x < Width; x++)
                {
                    char c;
                    if (_void[x, y]) c = '#';
                    else if (_pot[x, y] > 0) c = 'P';
                    else if (_cells[x, y] == Empty) c = '.';
                    else c = (char)((_vine[x, y] ? 'a' : 'A') + _cells[x, y]);
                    sb.Append(c);
                }
                if (y > 0) sb.Append('\n');
            }
            return sb.ToString();
        }
    }
}
