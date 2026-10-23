using System;
using System.Collections.Generic;

namespace Flowers.Board
{
    /// <summary>A flower moving from one cell to another (falling, sliding, or shuffling).</summary>
    public readonly struct FlowerMove : IEquatable<FlowerMove>
    {
        public readonly Position From;
        public readonly Position To;

        public FlowerMove(Position from, Position to)
        {
            From = from;
            To = to;
        }

        public bool Equals(FlowerMove other) => From == other.From && To == other.To;
        public override bool Equals(object obj) => obj is FlowerMove other && Equals(other);
        public override int GetHashCode() => From.GetHashCode() * 31 + To.GetHashCode();
        public override string ToString() => $"{From}->{To}";
    }

    /// <summary>A new flower entering the board from above.</summary>
    public readonly struct FlowerSpawn
    {
        /// <summary>Where the flower ends up (it may have slid sideways after entering).</summary>
        public readonly Position Position;
        public readonly int Type;

        /// <summary>The column it entered from (above the top row).</summary>
        public readonly int Column;

        /// <summary>0 for the first flower that entered this column, 1 for the next... The view stacks them above the board.</summary>
        public readonly int IndexInColumn;

        public FlowerSpawn(Position position, int type, int column, int indexInColumn)
        {
            Position = position;
            Type = type;
            Column = column;
            IndexInColumn = indexInColumn;
        }
    }

    /// <summary>
    /// Makes flowers fall into empty cells and fills the board up again. Obstacles (void cells, pots and
    /// vine-wrapped flowers) don't move and nothing passes through them, so:
    /// 1. flowers fall straight down as far as they can;
    /// 2. an empty cell with something fixed above it is filled diagonally from a neighbouring column;
    /// 3. new flowers drop in at the top of every column that is open from the top.
    /// Repeats until nothing changes. Reports one move per flower (where it started → where it ended).
    /// </summary>
    public static class Gravity
    {
        private const int MaxIterations = 10000;

        public static void Settle(BoardGrid board, Random random, List<FlowerMove> falls, List<FlowerSpawn> spawns)
        {
            int w = board.Width, h = board.Height;

            // Track where each flower came from: an existing cell, or an index into the spawns list.
            var origin = new Position[w, h];
            var spawnIndex = new int[w, h];
            var columnSpawnCount = new int[w];
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    origin[x, y] = new Position(x, y);
                    spawnIndex[x, y] = -1;
                }

            var pending = new List<(int column, int type, int index)>();

            void Move(int fx, int fy, int tx, int ty)
            {
                board[tx, ty] = board[fx, fy];
                board[fx, fy] = BoardGrid.Empty;
                origin[tx, ty] = origin[fx, fy];
                spawnIndex[tx, ty] = spawnIndex[fx, fy];
                spawnIndex[fx, fy] = -1;
            }

            bool IsHole(int x, int y)
            {
                var p = new Position(x, y);
                return board.CanHoldFlower(p) && !board.HasVine(p) && board[x, y] == BoardGrid.Empty;
            }

            bool CanSupply(int x, int y)
            {
                if (x < 0 || x >= w || y < 0 || y >= h) return false;
                var p = new Position(x, y);
                return !board.IsFixed(p) && board[x, y] != BoardGrid.Empty;
            }

            bool OpenToTop(int x, int y)
            {
                for (int yy = y + 1; yy < h; yy++)
                    if (board.IsFixed(new Position(x, yy))) return false;
                return true;
            }

            bool FallStraight()
            {
                bool moved = false;
                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        if (!IsHole(x, y)) continue;
                        for (int yy = y + 1; yy < h; yy++)
                        {
                            if (board.IsFixed(new Position(x, yy))) break;
                            if (board[x, yy] == BoardGrid.Empty) continue;
                            Move(x, yy, x, y);
                            moved = true;
                            break;
                        }
                    }
                }
                return moved;
            }

            bool SlideDiagonally()
            {
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        if (!IsHole(x, y) || OpenToTop(x, y)) continue;
                        // Alternate the side we try first so stacks don't always lean one way.
                        int first = ((x + y) & 1) == 0 ? -1 : 1;
                        for (int k = 0; k < 2; k++)
                        {
                            int sx = x + (k == 0 ? first : -first);
                            if (!CanSupply(sx, y + 1)) continue;
                            Move(sx, y + 1, x, y);
                            return true; // one at a time, then let straight falls catch up
                        }
                    }
                }
                return false;
            }

            bool DropInNewFlowers()
            {
                bool any = false;
                for (int x = 0; x < w; x++)
                {
                    // Holes reachable from the top of this column, lowest first.
                    int lowest = h;
                    for (int y = h - 1; y >= 0; y--)
                    {
                        var p = new Position(x, y);
                        if (board.IsFixed(p) || board[x, y] != BoardGrid.Empty) break;
                        lowest = y;
                    }
                    for (int y = lowest; y < h; y++)
                    {
                        int type = random.Next(board.TypeCount);
                        board[x, y] = type;
                        spawnIndex[x, y] = pending.Count;
                        pending.Add((x, type, columnSpawnCount[x]++));
                        any = true;
                    }
                }
                return any;
            }

            for (int i = 0; i < MaxIterations; i++)
            {
                if (FallStraight()) continue;
                if (SlideDiagonally()) continue;
                if (DropInNewFlowers()) continue;
                break;
            }

            // Report where everything ended up.
            var finalPositions = new Position[pending.Count];
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    if (board[x, y] == BoardGrid.Empty) continue;
                    if (spawnIndex[x, y] >= 0) finalPositions[spawnIndex[x, y]] = new Position(x, y);
                    else if (origin[x, y] != new Position(x, y)) falls.Add(new FlowerMove(origin[x, y], new Position(x, y)));
                }
            }
            for (int i = 0; i < pending.Count; i++)
                spawns.Add(new FlowerSpawn(finalPositions[i], pending[i].type, pending[i].column, pending[i].index));
        }
    }
}
