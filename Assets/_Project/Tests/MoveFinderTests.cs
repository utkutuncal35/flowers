using NUnit.Framework;
using static Flowers.Board.Tests.TestBoards;

namespace Flowers.Board.Tests
{
    public class MoveFinderTests
    {
        // Bottom row: A A B A. Swapping the B with the A on its right makes A A A.
        private static BoardGrid MakeBoardWithOneObviousMove() => FromRows(
            "CDEC",
            "DCDE",
            "AABA");

        [Test]
        public void SwapThatCreatesMatch_IsValid()
        {
            var board = MakeBoardWithOneObviousMove();

            Assert.That(MoveFinder.IsValidSwap(board, P(2, 0), P(3, 0)), Is.True);
            Assert.That(MoveFinder.IsValidSwap(board, P(3, 0), P(2, 0)), Is.True, "Order of the two cells must not matter.");
        }

        [Test]
        public void SwapWithoutMatch_IsInvalid()
        {
            var board = MakeBoardWithOneObviousMove();

            Assert.That(MoveFinder.IsValidSwap(board, P(0, 2), P(1, 2)), Is.False);
        }

        [Test]
        public void NonAdjacentSwaps_AreInvalid()
        {
            var board = MakeBoardWithOneObviousMove();

            Assert.That(MoveFinder.IsValidSwap(board, P(0, 0), P(2, 0)), Is.False, "two apart");
            Assert.That(MoveFinder.IsValidSwap(board, P(2, 0), P(3, 1)), Is.False, "diagonal");
            Assert.That(MoveFinder.IsValidSwap(board, P(2, 0), P(2, 0)), Is.False, "same cell");
        }

        [Test]
        public void OutOfBoundsSwap_IsInvalid()
        {
            var board = MakeBoardWithOneObviousMove();

            Assert.That(MoveFinder.IsValidSwap(board, P(3, 0), P(4, 0)), Is.False);
            Assert.That(MoveFinder.IsValidSwap(board, P(0, 0), P(0, -1)), Is.False);
        }

        [Test]
        public void SameTypeSwap_IsInvalid()
        {
            var board = MakeBoardWithOneObviousMove();

            Assert.That(MoveFinder.IsValidSwap(board, P(0, 0), P(1, 0)), Is.False);
        }

        [Test]
        public void CheckingASwap_DoesNotChangeTheBoard()
        {
            var board = MakeBoardWithOneObviousMove();
            string before = board.ToString();

            MoveFinder.IsValidSwap(board, P(2, 0), P(3, 0));
            MoveFinder.FindAllValidMoves(board);

            Assert.That(board.ToString(), Is.EqualTo(before));
        }

        [Test]
        public void DeadlockedBoard_HasNoValidMove()
        {
            var board = FromRows(
                "ABCD",
                "CDAB",
                "ABCD",
                "CDAB");

            Assert.That(MatchFinder.HasAnyMatch(board), Is.False, "precondition");
            Assert.That(MoveFinder.HasValidMove(board), Is.False);
            Assert.That(MoveFinder.FindAllValidMoves(board), Is.Empty);
        }

        [Test]
        public void FindFirstValidMove_ReturnsAMoveThatIsValid()
        {
            var board = MakeBoardWithOneObviousMove();

            var move = MoveFinder.FindFirstValidMove(board);

            Assert.That(move.HasValue, Is.True);
            Assert.That(MoveFinder.IsValidSwap(board, move.Value.A, move.Value.B), Is.True);
        }
    }
}
