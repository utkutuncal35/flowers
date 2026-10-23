using System;
using System.Collections.Generic;

namespace Flowers.Board
{
    public enum CellKind
    {
        Normal,
        Void,
        Pot1,
        Pot2,
        Vine,
        Soil,
        SoilVine,
    }

    /// <summary>
    /// The shape of a level's board and where its obstacles start, written as text so levels are easy to draw:
    /// <code>
    /// '.' flower        'x' no cell (shaped boards)
    /// 'v' vine-wrapped flower
    /// 'p' pot (1 layer) 'P' pot (2 layers)
    /// 's' soil under a flower   'S' soil under a vine-wrapped flower
    /// </code>
    /// Rows are given top row first, the way they look on screen.
    /// </summary>
    public sealed class BoardLayout
    {
        private readonly CellKind[,] _cells;

        public int Width { get; }
        public int Height { get; }

        private BoardLayout(CellKind[,] cells)
        {
            _cells = cells;
            Width = cells.GetLength(0);
            Height = cells.GetLength(1);
        }

        public CellKind this[int x, int y] => _cells[x, y];

        public static BoardLayout Rectangle(int width, int height) => new BoardLayout(new CellKind[width, height]);

        /// <summary>Parses the text form. Throws ArgumentException with a readable message on bad input.</summary>
        public static BoardLayout Parse(IReadOnlyList<string> rowsTopFirst)
        {
            if (rowsTopFirst == null || rowsTopFirst.Count == 0) throw new ArgumentException("layout has no rows");
            int height = rowsTopFirst.Count;
            int width = rowsTopFirst[0].Length;
            if (width == 0) throw new ArgumentException("layout rows are empty");

            var cells = new CellKind[width, height];
            for (int row = 0; row < height; row++)
            {
                string line = rowsTopFirst[row];
                if (line.Length != width)
                    throw new ArgumentException($"layout row {row + 1} has {line.Length} cells, expected {width}");
                int y = height - 1 - row;
                for (int x = 0; x < width; x++)
                    cells[x, y] = FromChar(line[x], row + 1, x + 1);
            }
            return new BoardLayout(cells);
        }

        /// <summary>Splits multi-line text (blank lines and surrounding spaces ignored) and parses it.</summary>
        public static BoardLayout Parse(string text)
        {
            var rows = new List<string>();
            foreach (var raw in (text ?? "").Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length > 0) rows.Add(line);
            }
            return Parse(rows);
        }

        public int Count(CellKind kind)
        {
            int count = 0;
            foreach (var c in _cells) if (c == kind) count++;
            return count;
        }

        public int PotCount => Count(CellKind.Pot1) + Count(CellKind.Pot2);
        public int VineCount => Count(CellKind.Vine) + Count(CellKind.SoilVine);
        public int SoilCount => Count(CellKind.Soil) + Count(CellKind.SoilVine);

        /// <summary>Cells that hold a flower at the start (everything except void and pots).</summary>
        public int FlowerCellCount => Width * Height - Count(CellKind.Void) - PotCount;

        /// <summary>Copies the obstacles onto an (empty) grid of the same size.</summary>
        public void ApplyTo(BoardGrid grid)
        {
            if (grid.Width != Width || grid.Height != Height) throw new ArgumentException("grid size does not match the layout");
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    var p = new Position(x, y);
                    var kind = _cells[x, y];
                    grid.SetVoid(p, kind == CellKind.Void);
                    grid.SetPot(p, kind == CellKind.Pot1 ? 1 : kind == CellKind.Pot2 ? 2 : 0);
                    grid.SetVine(p, kind == CellKind.Vine || kind == CellKind.SoilVine);
                    grid.SetSoil(p, kind == CellKind.Soil || kind == CellKind.SoilVine);
                }
            }
        }

        private static CellKind FromChar(char c, int row, int column)
        {
            switch (c)
            {
                case '.': return CellKind.Normal;
                case 'x': return CellKind.Void;
                case 'v': return CellKind.Vine;
                case 'p': return CellKind.Pot1;
                case 'P': return CellKind.Pot2;
                case 's': return CellKind.Soil;
                case 'S': return CellKind.SoilVine;
                default: throw new ArgumentException($"unknown layout character '{c}' at row {row}, column {column}");
            }
        }
    }
}
