using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static Flowers.Board.Tests.TestBoards;

namespace Flowers.Board.Tests
{
    public class GravityTests
    {
        private static (List<FlowerMove> falls, List<FlowerSpawn> spawns) Settle(BoardGrid board, int seed = 0)
        {
            var falls = new List<FlowerMove>();
            var spawns = new List<FlowerSpawn>();
            Gravity.Settle(board, new Random(seed), falls, spawns);
            return (falls, spawns);
        }

        [Test]
        public void FlowersFallIntoGaps_KeepingTheirOrder()
        {
            var board = FromRows(
                "A",
                ".",
                "B",
                ".",
                "C");

            var (falls, spawns) = Settle(board);

            Assert.That(board[0, 0], Is.EqualTo(2), "C stays at the bottom");
            Assert.That(board[0, 1], Is.EqualTo(1), "B fell one");
            Assert.That(board[0, 2], Is.EqualTo(0), "A fell two");
            Assert.That(falls, Is.EquivalentTo(new[]
            {
                new FlowerMove(P(0, 2), P(0, 1)),
                new FlowerMove(P(0, 4), P(0, 2)),
            }));
            Assert.That(spawns, Has.Count.EqualTo(2), "The two top cells get new flowers");
        }

        [Test]
        public void FullBoard_DoesNotChange()
        {
            var board = FromRows(
                "AB",
                "CD");

            var (falls, spawns) = Settle(board);

            Assert.That(falls, Is.Empty);
            Assert.That(spawns, Is.Empty);
            Assert.That(board.ToString(), Is.EqualTo("AB\nCD"));
        }

        [Test]
        public void SpawnsFillOnlyEmptyCells_AndAreIndexedFromTheBottom()
        {
            var board = FromRows(
                ".",
                ".",
                "A");

            var (_, spawns) = Settle(board);

            AssertFullAndInRange(board);
            var ordered = spawns.OrderBy(s => s.IndexInColumn).ToList();
            Assert.That(ordered[0].Position, Is.EqualTo(P(0, 1)));
            Assert.That(ordered[1].Position, Is.EqualTo(P(0, 2)));
            Assert.That(ordered.All(s => s.Column == 0));
            foreach (var s in spawns) Assert.That(board[s.Position], Is.EqualTo(s.Type));
        }

        [Test]
        public void PotsDoNotFall_AndBlockFlowersAbove()
        {
            // Column 1: flower above a pot, hole below the pot. The flower can't pass the pot,
            // so the hole is filled diagonally from a neighbouring column.
            var board = FromRows(
                "ABC",
                "D.E",
                "F.G");
            board.SetPot(P(1, 1), 1);
            board[1, 1] = BoardGrid.Empty;

            Settle(board);

            Assert.That(board.PotLayers(P(1, 1)), Is.EqualTo(1), "pot stays");
            Assert.That(board[1, 2], Is.EqualTo(1), "B above the pot doesn't move");
            Assert.That(board[1, 0], Is.Not.EqualTo(BoardGrid.Empty), "hole under the pot is filled from the side");
            AssertNoHoles(board);
        }

        [Test]
        public void VineFlowers_DoNotFall()
        {
            var board = FromRows(
                "A",
                "B",
                ".");
            board.SetVine(P(0, 1), true);

            Settle(board);

            Assert.That(board[0, 1], Is.EqualTo(1), "the vine-wrapped B stays put");
            Assert.That(board[0, 2], Is.EqualTo(0), "A can't fall through it");
            Assert.That(board[0, 0], Is.EqualTo(BoardGrid.Empty), "nothing can reach the cell below (no neighbours)");
        }

        [Test]
        public void VoidCells_StayEmpty_AndShapedBoardsFillCompletely()
        {
            var board = new BoardGrid(5, 5, 5);
            BoardLayout.Parse(new[]
            {
                "x...x",
                ".....",
                ".....",
                "x...x",
                "xx.xx",
            }).ApplyTo(board);

            Settle(board, seed: 4);

            for (int x = 0; x < 5; x++)
                for (int y = 0; y < 5; y++)
                {
                    var p = P(x, y);
                    if (board.IsVoid(p)) Assert.That(board[p], Is.EqualTo(BoardGrid.Empty), $"void {p} got a flower");
                }
            AssertNoHoles(board);
        }

        private static void AssertNoHoles(BoardGrid board)
        {
            for (int x = 0; x < board.Width; x++)
                for (int y = 0; y < board.Height; y++)
                {
                    var p = P(x, y);
                    if (board.CanHoldFlower(p))
                        Assert.That(board[p], Is.Not.EqualTo(BoardGrid.Empty), $"hole left at {p}:\n{board}");
                }
        }
    }
}
