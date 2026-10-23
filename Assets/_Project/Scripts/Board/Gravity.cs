using System;
using System.Collections.Generic;

namespace Flowers.Board
{
    /// <summary>A flower moving from one cell to another (falling, or shuffling).</summary>
    public readonly struct FlowerMove
    {
        public readonly Position From;
        public readonly Position To;

        public FlowerMove(Position from, Position to)
        {
            From = from;
            To = to;
        }

        public override string ToString() => $"{From}->{To}";
    }

    /// <summary>A new flower appearing at the top of a column.</summary>
    public readonly struct FlowerSpawn
    {
        public readonly Position Position;
        public readonly int Type;

        /// <summary>0 for the lowest new flower in its column, 1 for the one above it, ... The view can use this to stack them above the board.</summary>
        public readonly int IndexInColumn;

        public FlowerSpawn(Position position, int type, int indexInColumn)
        {
            Position = position;
            Type = type;
            IndexInColumn = indexInColumn;
        }
    }

    public static class Gravity
    {
        /// <summary>Drops flowers down into empty cells, keeping their order. Empty cells end up at the top.</summary>
        public static List<FlowerMove> ApplyGravity(BoardGrid board)
        {
            var falls = new List<FlowerMove>();
            for (int x = 0; x < board.Width; x++)
            {
                int writeY = 0;
                for (int y = 0; y < board.Height; y++)
                {
                    int type = board[x, y];
                    if (type == BoardGrid.Empty) continue;

                    if (y != writeY)
                    {
                        board[x, writeY] = type;
                        board[x, y] = BoardGrid.Empty;
                        falls.Add(new FlowerMove(new Position(x, y), new Position(x, writeY)));
                    }
                    writeY++;
                }
            }
            return falls;
        }

        /// <summary>Fills every empty cell with a random flower.</summary>
        public static List<FlowerSpawn> Refill(BoardGrid board, Random random)
        {
            var spawns = new List<FlowerSpawn>();
            for (int x = 0; x < board.Width; x++)
            {
                int index = 0;
                for (int y = 0; y < board.Height; y++)
                {
                    if (board[x, y] != BoardGrid.Empty) continue;

                    int type = random.Next(board.TypeCount);
                    board[x, y] = type;
                    spawns.Add(new FlowerSpawn(new Position(x, y), type, index++));
                }
            }
            return spawns;
        }
    }
}
