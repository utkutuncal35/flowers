using System;
using System.Collections.Generic;
using Flowers.Levels;
using NUnit.Framework;

namespace Flowers.Board.Tests
{
    public class LevelSessionTests
    {
        private const int Rose = 0;
        private const int Tulip = 1;

        private static LevelDefinition Level(int moves, int twoStar, int threeStar, params LevelGoal[] goals) =>
            new LevelDefinition(7, 9, 5, moves, goals, twoStar, threeStar);

        /// <summary>A valid swap whose cascade steps clear the given flower types (one array per step).</summary>
        private static SwapResult Swap(params int[][] clearedTypesPerStep)
        {
            var steps = new List<CascadeStep>();
            foreach (var types in clearedTypesPerStep)
            {
                var cleared = new List<ClearedFlower>();
                for (int i = 0; i < types.Length; i++)
                    cleared.Add(new ClearedFlower(new Position(i, 0), types[i]));
                steps.Add(new CascadeStep(new Match[0], cleared, new FlowerMove[0], new FlowerSpawn[0]));
            }
            return SwapResult.Valid(new Position(0, 0), new Position(1, 0), steps, false, null, false);
        }

        private static int[] Many(int type, int count)
        {
            var a = new int[count];
            for (int i = 0; i < count; i++) a[i] = type;
            return a;
        }

        [Test]
        public void NewSession_StartsPlaying_WithAllMoves()
        {
            var session = new LevelSession(Level(10, 3, 6, LevelGoal.Collect(Rose, 9)));

            Assert.That(session.State, Is.EqualTo(LevelState.Playing));
            Assert.That(session.MovesLeft, Is.EqualTo(10));
            Assert.That(session.Score, Is.EqualTo(0));
            Assert.That(session.Goals[0].Remaining, Is.EqualTo(9));
        }

        [Test]
        public void ValidSwap_CostsOneMove_AndCountsFlowers()
        {
            var session = new LevelSession(Level(10, 3, 6, LevelGoal.Collect(Rose, 9)));

            bool counted = session.ApplySwap(Swap(new[] { Rose, Rose, Rose }));

            Assert.That(counted, Is.True);
            Assert.That(session.MovesLeft, Is.EqualTo(9));
            Assert.That(session.Goals[0].Current, Is.EqualTo(3));
            Assert.That(session.Goals[0].Remaining, Is.EqualTo(6));
        }

        [Test]
        public void InvalidSwap_IsFree()
        {
            var session = new LevelSession(Level(10, 3, 6, LevelGoal.Collect(Rose, 9)));

            bool counted = session.ApplySwap(SwapResult.Invalid(new Position(0, 0), new Position(1, 0)));

            Assert.That(counted, Is.False);
            Assert.That(session.MovesLeft, Is.EqualTo(10));
        }

        [Test]
        public void OtherFlowerTypes_DoNotCountTowardsACollectGoal()
        {
            var session = new LevelSession(Level(10, 3, 6, LevelGoal.Collect(Rose, 9)));

            session.ApplySwap(Swap(new[] { Tulip, Tulip, Tulip }));

            Assert.That(session.Goals[0].Current, Is.EqualTo(0));
        }

        [Test]
        public void CascadeSteps_AllCount_AndChainsScoreMore()
        {
            var session = new LevelSession(Level(10, 3, 6, LevelGoal.Collect(Rose, 9)));

            session.ApplySwap(Swap(new[] { Rose, Rose, Rose }, new[] { Rose, Rose, Rose }));

            Assert.That(session.Goals[0].Current, Is.EqualTo(6));
            // Step 0: 3 x 10 x1 = 30, step 1: 3 x 10 x2 = 60
            Assert.That(session.Score, Is.EqualTo(90));
        }

        [Test]
        public void CompletingAllGoals_Wins()
        {
            var session = new LevelSession(Level(10, 3, 6, LevelGoal.Collect(Rose, 3), LevelGoal.Collect(Tulip, 3)));

            session.ApplySwap(Swap(new[] { Rose, Rose, Rose }));
            Assert.That(session.State, Is.EqualTo(LevelState.Playing), "Only one of two goals done.");

            session.ApplySwap(Swap(new[] { Tulip, Tulip, Tulip }));
            Assert.That(session.State, Is.EqualTo(LevelState.Won));
        }

        [Test]
        public void ScoreGoal_WinsWhenScoreReached()
        {
            var session = new LevelSession(Level(10, 3, 6, LevelGoal.ReachScore(100)));

            session.ApplySwap(Swap(Many(Rose, 5)));
            Assert.That(session.Goals[0].Current, Is.EqualTo(50));
            Assert.That(session.State, Is.EqualTo(LevelState.Playing));

            session.ApplySwap(Swap(Many(Tulip, 5)));
            Assert.That(session.State, Is.EqualTo(LevelState.Won));
        }

        [Test]
        public void GoalProgress_CanOvershoot_ButRemainingStopsAtZero()
        {
            var session = new LevelSession(Level(10, 3, 6, LevelGoal.Collect(Rose, 3), LevelGoal.Collect(Tulip, 3)));

            session.ApplySwap(Swap(Many(Rose, 5)));

            Assert.That(session.Goals[0].Current, Is.EqualTo(5));
            Assert.That(session.Goals[0].Remaining, Is.EqualTo(0));
            Assert.That(session.Goals[0].IsComplete, Is.True);
        }

        [Test]
        public void RunningOutOfMoves_Loses()
        {
            var session = new LevelSession(Level(2, 1, 1, LevelGoal.Collect(Rose, 9)));

            session.ApplySwap(Swap(new[] { Tulip, Tulip, Tulip }));
            session.ApplySwap(Swap(new[] { Tulip, Tulip, Tulip }));

            Assert.That(session.State, Is.EqualTo(LevelState.Lost));
            Assert.That(session.MovesLeft, Is.EqualTo(0));
            Assert.That(session.Stars, Is.EqualTo(0));
        }

        [Test]
        public void FinishingOnTheLastMove_IsAWin()
        {
            var session = new LevelSession(Level(2, 1, 1, LevelGoal.Collect(Rose, 3)));

            session.ApplySwap(Swap(new[] { Tulip, Tulip, Tulip }));
            session.ApplySwap(Swap(new[] { Rose, Rose, Rose }));

            Assert.That(session.MovesLeft, Is.EqualTo(0));
            Assert.That(session.State, Is.EqualTo(LevelState.Won));
            Assert.That(session.Stars, Is.EqualTo(1));
        }

        [Test]
        public void AfterTheLevelEnds_SwapsAreIgnored()
        {
            var session = new LevelSession(Level(10, 3, 6, LevelGoal.Collect(Rose, 3)));
            session.ApplySwap(Swap(new[] { Rose, Rose, Rose }));
            Assume.That(session.State, Is.EqualTo(LevelState.Won));

            bool counted = session.ApplySwap(Swap(new[] { Rose, Rose, Rose }));

            Assert.That(counted, Is.False);
            Assert.That(session.MovesLeft, Is.EqualTo(9));
        }

        [TestCase(9, 3)] // used 1 move  → 9 left
        [TestCase(6, 3)] // exactly the 3-star line
        [TestCase(5, 2)]
        [TestCase(3, 2)] // exactly the 2-star line
        [TestCase(2, 1)]
        [TestCase(0, 1)]
        public void Stars_DependOnMovesLeft(int movesLeftAtWin, int expectedStars)
        {
            var session = new LevelSession(Level(10, 3, 6, LevelGoal.Collect(Rose, 3)));

            for (int i = 0; i < 10 - movesLeftAtWin - 1; i++)
                session.ApplySwap(Swap(new[] { Tulip, Tulip, Tulip }));
            session.ApplySwap(Swap(new[] { Rose, Rose, Rose }));

            Assert.That(session.State, Is.EqualTo(LevelState.Won));
            Assert.That(session.MovesLeft, Is.EqualTo(movesLeftAtWin));
            Assert.That(session.Stars, Is.EqualTo(expectedStars));
        }

        [Test]
        public void InvalidDefinitions_AreRejectedWithAReason()
        {
            Assert.That(Level(10, 3, 6).Validate(), Does.Contain("no goals"));
            Assert.That(Level(10, 3, 6, LevelGoal.Collect(7, 5)).Validate(), Does.Contain("flower type 7"));
            Assert.That(Level(10, 6, 3, LevelGoal.Collect(Rose, 5)).Validate(), Does.Contain("3-star"));
            Assert.That(Level(10, 3, 10, LevelGoal.Collect(Rose, 5)).Validate(), Does.Contain("smaller than the move count"));
            Assert.That(Level(0, 1, 1, LevelGoal.Collect(Rose, 5)).Validate(), Does.Contain("at least 1 move"));
            Assert.That(Level(10, 3, 6, LevelGoal.Collect(Rose, 5)).Validate(), Is.Null);

            Assert.Throws<ArgumentException>(() => new LevelSession(Level(10, 3, 6)));
        }

        [Test]
        public void RealEngine_SessionStaysConsistent()
        {
            // Plays real games end to end: the session must always end in Won or Lost, never get stuck.
            for (int seed = 0; seed < 20; seed++)
            {
                var random = new Random(seed);
                var definition = Level(15, 3, 6, LevelGoal.Collect(Rose, 20), LevelGoal.Collect(Tulip, 20));
                var engine = new BoardEngine(definition.Width, definition.Height, definition.TypeCount, random);
                var session = new LevelSession(definition);

                while (session.State == LevelState.Playing)
                {
                    var moves = MoveFinder.FindAllValidMoves(engine.Grid);
                    var move = moves[random.Next(moves.Count)];
                    session.ApplySwap(engine.TrySwap(move.A, move.B));
                }

                Assert.That(session.MovesLeft, Is.InRange(0, 15));
                if (session.State == LevelState.Won) Assert.That(session.Stars, Is.InRange(1, 3));
                else Assert.That(session.MovesLeft, Is.EqualTo(0));
            }
        }
    }
}
