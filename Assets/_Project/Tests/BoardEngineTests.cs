using System;
using System.Linq;
using NUnit.Framework;
using static Flowers.Board.Tests.TestBoards;

namespace Flowers.Board.Tests
{
    public class BoardEngineTests
    {
        [Test]
        public void NewEngine_StartsWithPlayableBoard()
        {
            var engine = new BoardEngine(7, 9, 5, seed: 123);

            AssertPlayable(engine.Grid);
        }

        [Test]
        public void InvalidSwap_LeavesBoardUnchanged()
        {
            var board = FromRows(
                "CDEC",
                "DCDE",
                "AABA");
            var engine = new BoardEngine(board, new Random(0));
            string before = board.ToString();

            var result = engine.TrySwap(P(0, 2), P(1, 2));

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Steps, Is.Empty);
            Assert.That(board.ToString(), Is.EqualTo(before));
        }

        [Test]
        public void ValidSwap_ClearsTheMatch_AndLeavesAPlayableBoard()
        {
            var board = FromRows(
                "CDEC",
                "DCDE",
                "AABA");
            var engine = new BoardEngine(board, new Random(0));

            var result = engine.TrySwap(P(2, 0), P(3, 0));

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Steps, Is.Not.Empty);
            var firstCleared = result.Steps[0].Cleared.Select(c => c.Position);
            Assert.That(firstCleared, Is.EquivalentTo(new[] { P(0, 0), P(1, 0), P(2, 0) }));
            Assert.That(result.Steps[0].Cleared.All(c => c.Type == 0), Is.True, "All three cleared flowers are A.");
            AssertPlayable(board);
        }

        [Test]
        public void FallingFlowers_CanTriggerAChainReaction()
        {
            // Swap B(2,0) with A(3,0): bottom row becomes A A A B and clears x0..x2.
            // Columns 0..2 drop by one, so the bottom row becomes C B B + B = a second match.
            var board = FromRows(
                "ECEC",
                "CBBD",
                "AABA");
            Assume.That(MatchFinder.HasAnyMatch(board), Is.False);
            var engine = new BoardEngine(board, new Random(0));

            var result = engine.TrySwap(P(2, 0), P(3, 0));

            Assert.That(result.Steps.Count, Is.GreaterThanOrEqualTo(2));
            var secondCleared = result.Steps[1].Cleared.Select(c => c.Position);
            Assert.That(secondCleared, Is.SupersetOf(new[] { P(1, 0), P(2, 0), P(3, 0) }));
            AssertPlayable(board);
        }

        [Test]
        public void CascadeStep_FallsAndSpawnsRefillEveryClearedCell()
        {
            var engine = new BoardEngine(FromRows(
                "CDEC",
                "DCDE",
                "AABA"), new Random(0));

            var result = engine.TrySwap(P(2, 0), P(3, 0));

            foreach (var step in result.Steps)
                Assert.That(step.Spawns.Count, Is.EqualTo(step.Cleared.Count),
                    "Every popped flower is replaced by exactly one new flower.");
        }

        [Test]
        public void RandomGames_AlwaysStayPlayable()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                var random = new Random(seed);
                var engine = new BoardEngine(7, 9, 5, random);

                for (int turn = 0; turn < 100; turn++)
                {
                    var moves = MoveFinder.FindAllValidMoves(engine.Grid);
                    Assert.That(moves, Is.Not.Empty, $"Seed {seed}, turn {turn}: no moves left and no shuffle happened.\n{engine.Grid}");

                    var move = moves[random.Next(moves.Count)];
                    var result = engine.TrySwap(move.A, move.B);

                    Assert.That(result.IsValid, Is.True);
                    AssertPlayable(engine.Grid);
                }
            }
        }

        [Test]
        public void StuckBoard_IsShuffledAfterTheSwap()
        {
            // 3 types on a tiny board run out of moves often, so shuffles actually happen here.
            int shuffles = 0;
            for (int seed = 0; seed < 200; seed++)
            {
                var random = new Random(seed);
                var engine = new BoardEngine(4, 4, 3, random);
                for (int turn = 0; turn < 20; turn++)
                {
                    var move = MoveFinder.FindFirstValidMove(engine.Grid).Value;
                    var result = engine.TrySwap(move.A, move.B);
                    if (result.Shuffled) shuffles++;
                    AssertPlayable(engine.Grid);
                }
            }

            Assert.That(shuffles, Is.GreaterThan(0), "Expected at least one shuffle across all games.");
        }
    }
}
