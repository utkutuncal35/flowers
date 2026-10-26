using System;
using NUnit.Framework;
using static Flowers.Board.Tests.TestBoards;

namespace Flowers.Board.Tests
{
    /// <summary>
    /// Plays the engine's results back onto a mirror of flower objects, the way BoardView does, and checks
    /// the mirror always ends up exactly like the board: no flower lost, none duplicated.
    /// </summary>
    public class MoveReplayTests
    {
        private sealed class Token
        {
            public readonly int Type;
            public Token(int type) { Type = type; }
        }

        [Test]
        public void Apply_ChainedMoves_DoNotOverwriteEachOther()
        {
            // a: (0,2)->(0,1), b: (0,1)->(0,0). Applied one by one in this order, b would be lost.
            var a = new Token(0);
            var b = new Token(1);
            var cells = new Token[1, 3];
            cells[0, 2] = a;
            cells[0, 1] = b;

            MoveReplay.Apply(cells, new[] { new FlowerMove(P(0, 2), P(0, 1)), new FlowerMove(P(0, 1), P(0, 0)) },
                new Token[1, 3]);

            Assert.That(cells[0, 0], Is.SameAs(b));
            Assert.That(cells[0, 1], Is.SameAs(a));
            Assert.That(cells[0, 2], Is.Null);
        }

        private static readonly string[][] Layouts =
        {
            new[] { "x.....x", ".v...v.", "..p.p..", "s.sPs.s", ".......", "..vvv..", "x.....x" },
            // Heart, with pots hanging under the top bumps so the cells below fill diagonally.
            new[] { "x..x..x", ".......", "..P.P..", ".v...v.", "x.....x", "xx...xx", "xxx.xxx" },
            new[] { "...x...", "..ppp..", ".......", ".v.v.v.", "s.....s", "SSS.SSS" },
            new[] { ".......", ".p.p.p.", ".......", "P.P.P.P", "......." },
        };

        [Test]
        public void ReplayingRandomGames_KeepsTheViewInSyncWithTheBoard()
        {
            foreach (var rows in Layouts)
            {
                var layout = BoardLayout.Parse(rows);
                for (int seed = 0; seed < 30; seed++)
                {
                    var random = new Random(seed);
                    var engine = new BoardEngine(layout, 5, random);
                    var grid = engine.Grid;
                    var view = new Token[grid.Width, grid.Height];
                    var buffer = new Token[grid.Width, grid.Height];
                    for (int x = 0; x < grid.Width; x++)
                        for (int y = 0; y < grid.Height; y++)
                            if (grid[x, y] != BoardGrid.Empty) view[x, y] = new Token(grid[x, y]);

                    for (int turn = 0; turn < 40; turn++)
                    {
                        string where = $"layout {Array.IndexOf(Layouts, rows)} seed {seed} turn {turn}";
                        var moves = MoveFinder.FindAllValidMoves(grid);
                        var move = moves[random.Next(moves.Count)];
                        var result = engine.TrySwap(move.A, move.B);
                        Assert.That(result.IsValid, Is.True, where);

                        (view[move.A.X, move.A.Y], view[move.B.X, move.B.Y]) = (view[move.B.X, move.B.Y], view[move.A.X, move.A.Y]);
                        foreach (var step in result.Steps)
                        {
                            foreach (var cleared in step.Cleared)
                            {
                                var p = cleared.Position;
                                Assert.That(view[p.X, p.Y], Is.Not.Null, $"{where}: popped an empty cell {p}");
                                Assert.That(view[p.X, p.Y].Type, Is.EqualTo(cleared.Type), $"{where}: popped the wrong flower at {p}");
                                view[p.X, p.Y] = null;
                            }
                            foreach (var fall in step.Falls)
                                Assert.That(view[fall.From.X, fall.From.Y], Is.Not.Null, $"{where}: fall from an empty cell {fall}");

                            MoveReplay.Apply(view, step.Falls, buffer);

                            foreach (var spawn in step.Spawns)
                            {
                                var p = spawn.Position;
                                Assert.That(view[p.X, p.Y], Is.Null, $"{where}: spawned on top of a flower at {p}");
                                view[p.X, p.Y] = new Token(spawn.Type);
                            }
                        }
                        Assert.That(result.ShuffleRegenerated, Is.False, where);
                        if (result.Shuffled) MoveReplay.Apply(view, result.ShuffleMoves, buffer);

                        for (int x = 0; x < grid.Width; x++)
                            for (int y = 0; y < grid.Height; y++)
                            {
                                int expected = grid[x, y];
                                var token = view[x, y];
                                if (expected == BoardGrid.Empty) Assert.That(token, Is.Null, $"{where}: extra flower at ({x},{y})\n{grid}");
                                else Assert.That(token?.Type, Is.EqualTo(expected), $"{where}: wrong flower at ({x},{y})\n{grid}");
                            }
                    }
                }
            }
        }
    }
}
