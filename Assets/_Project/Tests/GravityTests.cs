using System;
using NUnit.Framework;
using static Flowers.Board.Tests.TestBoards;

namespace Flowers.Board.Tests
{
    public class GravityTests
    {
        [Test]
        public void FlowersFallIntoGaps_KeepingTheirOrder()
        {
            var board = FromRows(
                "A",
                ".",
                "B",
                ".",
                "C");

            var falls = Gravity.ApplyGravity(board);

            Assert.That(board.ToString(), Is.EqualTo(".\n.\nA\nB\nC"));
            Assert.That(falls, Is.EquivalentTo(new[]
            {
                new FlowerMove(P(0, 2), P(0, 1)),
                new FlowerMove(P(0, 4), P(0, 2)),
            }));
        }

        [Test]
        public void FullColumn_DoesNotMove()
        {
            var board = FromRows(
                "AB",
                "CD");

            Assert.That(Gravity.ApplyGravity(board), Is.Empty);
            Assert.That(board.ToString(), Is.EqualTo("AB\nCD"));
        }

        [Test]
        public void ColumnsAreIndependent()
        {
            var board = FromRows(
                "AB",
                ".C",
                "..");

            Gravity.ApplyGravity(board);

            Assert.That(board.ToString(), Is.EqualTo("..\n.B\nAC"));
        }

        [Test]
        public void Refill_FillsOnlyEmptyCells_WithValidTypes()
        {
            var board = FromRows(
                "..A",
                ".BC",
                "DEA");

            var spawns = Gravity.Refill(board, new Random(3));

            AssertFullAndInRange(board);
            Assert.That(spawns, Has.Count.EqualTo(3));
            Assert.That(board[2, 2], Is.EqualTo(0), "Existing flowers must not change.");
            Assert.That(board[1, 1], Is.EqualTo(1));
            foreach (var s in spawns)
                Assert.That(board[s.Position], Is.EqualTo(s.Type));
        }

        [Test]
        public void Refill_IndexesSpawnsFromTheBottomOfEachColumn()
        {
            var board = FromRows(
                ".",
                ".",
                "A");

            var spawns = Gravity.Refill(board, new Random(0));

            Assert.That(spawns[0].Position, Is.EqualTo(P(0, 1)));
            Assert.That(spawns[0].IndexInColumn, Is.EqualTo(0));
            Assert.That(spawns[1].Position, Is.EqualTo(P(0, 2)));
            Assert.That(spawns[1].IndexInColumn, Is.EqualTo(1));
        }
    }
}
