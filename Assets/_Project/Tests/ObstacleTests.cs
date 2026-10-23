using System;
using System.Linq;
using NUnit.Framework;
using static Flowers.Board.Tests.TestBoards;

namespace Flowers.Board.Tests
{
    public class ObstacleTests
    {
        // Bottom row A A B A: swapping (2,0) with (3,0) makes A A A at x0..x2.
        private static BoardGrid MakeBoard() => FromRows(
            "CDEC",
            "DCDE",
            "AABA");

        [Test]
        public void Layout_ParsesEveryCellKind()
        {
            var layout = BoardLayout.Parse(new[]
            {
                "x.v",
                "pPs",
                "S..",
            });

            Assert.That(layout.Width, Is.EqualTo(3));
            Assert.That(layout.Height, Is.EqualTo(3));
            Assert.That(layout[0, 2], Is.EqualTo(CellKind.Void));
            Assert.That(layout[2, 2], Is.EqualTo(CellKind.Vine));
            Assert.That(layout[0, 1], Is.EqualTo(CellKind.Pot1));
            Assert.That(layout[1, 1], Is.EqualTo(CellKind.Pot2));
            Assert.That(layout[2, 1], Is.EqualTo(CellKind.Soil));
            Assert.That(layout[0, 0], Is.EqualTo(CellKind.SoilVine));
            Assert.That(layout.PotCount, Is.EqualTo(2));
            Assert.That(layout.VineCount, Is.EqualTo(2));
            Assert.That(layout.SoilCount, Is.EqualTo(2));
            Assert.That(layout.FlowerCellCount, Is.EqualTo(6));
        }

        [Test]
        public void Layout_RejectsBadInput()
        {
            Assert.Throws<ArgumentException>(() => BoardLayout.Parse(new[] { "...", ".." }));
            Assert.Throws<ArgumentException>(() => BoardLayout.Parse(new[] { "..?" }));
            Assert.Throws<ArgumentException>(() => BoardLayout.Parse(new string[0]));
        }

        [Test]
        public void VineFlowers_CannotBeSwapped()
        {
            var board = MakeBoard();
            board.SetVine(P(3, 0), true);

            Assert.That(MoveFinder.IsValidSwap(board, P(2, 0), P(3, 0)), Is.False);
        }

        [Test]
        public void MatchNextToAPot_HitsIt()
        {
            var board = MakeBoard();
            board.SetPot(P(0, 1), 2);
            board[0, 1] = BoardGrid.Empty;
            var engine = new BoardEngine(board, new Random(0));

            var result = engine.TrySwap(P(2, 0), P(3, 0));

            var hit = result.Steps[0].PotHits.Single(h => h.Position == P(0, 1));
            Assert.That(hit.LayersLeft, Is.EqualTo(1));
            Assert.That(board.PotLayers(P(0, 1)), Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void MatchedVineFlower_LosesItsVine_ButStays()
        {
            // (0,0) is vine-wrapped but can still be part of the A A A match.
            var board = MakeBoard();
            board.SetVine(P(0, 0), true);
            var engine = new BoardEngine(board, new Random(0));

            var result = engine.TrySwap(P(2, 0), P(3, 0));

            var first = result.Steps[0];
            Assert.That(first.VinesFreed, Has.Member(P(0, 0)));
            Assert.That(first.Cleared.Select(c => c.Position), Has.No.Member(P(0, 0)));
            Assert.That(board.HasVine(P(0, 0)), Is.False);
        }

        [Test]
        public void PopNextToVine_FreesIt()
        {
            var board = MakeBoard();
            board.SetVine(P(1, 1), true); // C above the middle A
            var engine = new BoardEngine(board, new Random(0));

            var result = engine.TrySwap(P(2, 0), P(3, 0));

            Assert.That(result.Steps[0].VinesFreed, Has.Member(P(1, 1)));
        }

        [Test]
        public void PoppingOnSoil_ClearsIt()
        {
            var board = MakeBoard();
            board.SetSoil(P(1, 0), true);
            board.SetSoil(P(3, 2), true); // far away, untouched
            var engine = new BoardEngine(board, new Random(0));

            var result = engine.TrySwap(P(2, 0), P(3, 0));

            Assert.That(result.Steps[0].SoilCleared, Has.Member(P(1, 0)));
            Assert.That(board.HasSoil(P(1, 0)), Is.False);
            Assert.That(board.HasSoil(P(3, 2)), Is.True);
        }

        [Test]
        public void GeneratedBoardsWithObstacles_ArePlayable()
        {
            var layout = BoardLayout.Parse(new[]
            {
                "x.....x",
                ".v...v.",
                "..p.p..",
                "s.sPs.s",
                ".......",
                "..vvv..",
                "x.....x",
            });

            for (int seed = 0; seed < 50; seed++)
            {
                var board = BoardGenerator.Generate(layout, 5, new Random(seed));
                Assert.That(MatchFinder.HasAnyMatch(board), Is.False, $"seed {seed}:\n{board}");
                Assert.That(MoveFinder.HasValidMove(board), Is.True, $"seed {seed}:\n{board}");
                Assert.That(board.CountPots(), Is.EqualTo(3));
                Assert.That(board.CountVines(), Is.EqualTo(5));
                Assert.That(board.CountSoil(), Is.EqualTo(4));
            }
        }

        [Test]
        public void RandomGames_WithObstacles_StayConsistent()
        {
            var layout = BoardLayout.Parse(new[]
            {
                "x.....x",
                ".v...v.",
                "..p.p..",
                "s.sPs.s",
                ".......",
                "..vvv..",
                "x.....x",
            });

            for (int seed = 0; seed < 15; seed++)
            {
                var random = new Random(seed);
                var engine = new BoardEngine(layout, 5, random);
                for (int turn = 0; turn < 60; turn++)
                {
                    var moves = MoveFinder.FindAllValidMoves(engine.Grid);
                    Assert.That(moves, Is.Not.Empty, $"seed {seed} turn {turn}: stuck\n{engine.Grid}");
                    var move = moves[random.Next(moves.Count)];
                    engine.TrySwap(move.A, move.B);

                    Assert.That(MatchFinder.HasAnyMatch(engine.Grid), Is.False, $"seed {seed} turn {turn}: leftover match\n{engine.Grid}");
                    for (int x = 0; x < layout.Width; x++)
                        for (int y = 0; y < layout.Height; y++)
                        {
                            var p = P(x, y);
                            if (engine.Grid.IsVoid(p)) Assert.That(engine.Grid[p], Is.EqualTo(BoardGrid.Empty));
                            if (engine.Grid.PotLayers(p) > 0) Assert.That(engine.Grid[p], Is.EqualTo(BoardGrid.Empty));
                        }
                }
            }
        }
    }
}
