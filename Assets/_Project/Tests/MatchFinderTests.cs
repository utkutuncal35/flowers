using System.Linq;
using NUnit.Framework;
using static Flowers.Board.Tests.TestBoards;

namespace Flowers.Board.Tests
{
    public class MatchFinderTests
    {
        [Test]
        public void NoMatches_OnBoardWithoutRuns()
        {
            var board = FromRows(
                "ABAB",
                "BABA",
                "AABB");

            Assert.That(MatchFinder.FindMatches(board), Is.Empty);
            Assert.That(MatchFinder.HasAnyMatch(board), Is.False);
        }

        [Test]
        public void FindsHorizontalThree()
        {
            var board = FromRows(
                "BCDE",
                "AAAB");

            var matches = MatchFinder.FindMatches(board);

            Assert.That(matches, Has.Count.EqualTo(1));
            Assert.That(matches[0].IsHorizontal, Is.True);
            Assert.That(matches[0].Type, Is.EqualTo(0));
            Assert.That(matches[0].Positions, Is.EquivalentTo(new[] { P(0, 0), P(1, 0), P(2, 0) }));
        }

        [Test]
        public void FindsVerticalThree()
        {
            var board = FromRows(
                "BC",
                "BD",
                "BE",
                "AC");

            var matches = MatchFinder.FindMatches(board);

            Assert.That(matches, Has.Count.EqualTo(1));
            Assert.That(matches[0].IsHorizontal, Is.False);
            Assert.That(matches[0].Positions, Is.EquivalentTo(new[] { P(0, 1), P(0, 2), P(0, 3) }));
        }

        [Test]
        public void RunOfFive_IsOneMatchOfLengthFive()
        {
            var board = FromRows("CCCCCA");

            var matches = MatchFinder.FindMatches(board);

            Assert.That(matches, Has.Count.EqualTo(1));
            Assert.That(matches[0].Length, Is.EqualTo(5));
        }

        [Test]
        public void TwoSeparateRunsInOneRow_AreTwoMatches()
        {
            var board = FromRows("AAABBB");

            Assert.That(MatchFinder.FindMatches(board), Has.Count.EqualTo(2));
        }

        [Test]
        public void LShape_IsTwoMatches_SharingOneCell()
        {
            var board = FromRows(
                "ABC",
                "ACB",
                "AAA");

            var matches = MatchFinder.FindMatches(board);
            var cells = MatchFinder.CollectPositions(matches);

            Assert.That(matches, Has.Count.EqualTo(2));
            Assert.That(cells, Has.Count.EqualTo(5), "The corner cell must be counted once.");
        }

        [Test]
        public void EmptyCells_NeverMatch()
        {
            var board = FromRows(
                "...",
                "ABC");

            Assert.That(MatchFinder.FindMatches(board), Is.Empty);
            Assert.That(MatchFinder.IsPartOfMatch(board, P(0, 1)), Is.False);
        }

        [Test]
        public void IsPartOfMatch_AgreesWithFindMatches()
        {
            var board = FromRows(
                "ABAD",
                "ABCD",
                "ABDD");

            var fromFind = MatchFinder.CollectPositions(MatchFinder.FindMatches(board));
            for (int x = 0; x < board.Width; x++)
                for (int y = 0; y < board.Height; y++)
                    Assert.That(MatchFinder.IsPartOfMatch(board, P(x, y)), Is.EqualTo(fromFind.Contains(P(x, y))),
                        $"Mismatch at ({x},{y})");

            Assert.That(fromFind.Count(), Is.EqualTo(9));
        }
    }
}
