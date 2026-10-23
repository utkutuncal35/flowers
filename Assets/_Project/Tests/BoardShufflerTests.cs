using System;
using NUnit.Framework;
using static Flowers.Board.Tests.TestBoards;

namespace Flowers.Board.Tests
{
    public class BoardShufflerTests
    {
        private static BoardGrid MakeDeadlockedBoard() => FromRows(
            "ABCD",
            "CDAB",
            "ABCD",
            "CDAB");

        [Test]
        public void ShuffledDeadlock_BecomesPlayable()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var board = MakeDeadlockedBoard();
                Assert.That(MoveFinder.HasValidMove(board), Is.False, "precondition");

                BoardShuffler.Shuffle(board, new Random(seed), out _);

                AssertPlayable(board);
            }
        }

        [Test]
        public void Shuffle_KeepsTheSameFlowers()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var board = MakeDeadlockedBoard();
                var before = CountTypes(board);

                BoardShuffler.Shuffle(board, new Random(seed), out bool regenerated);

                Assert.That(regenerated, Is.False, "A 4x4 board with 4 types should always be shuffleable.");
                Assert.That(CountTypes(board), Is.EquivalentTo(before));
            }
        }

        [Test]
        public void ShuffleMoves_DescribeTheNewLayout()
        {
            var board = MakeDeadlockedBoard();
            var original = board.Clone();

            var moves = BoardShuffler.Shuffle(board, new Random(7), out bool regenerated);

            Assert.That(regenerated, Is.False);
            foreach (var move in moves)
                Assert.That(board[move.To], Is.EqualTo(original[move.From]), $"Move {move}");
        }
    }
}
