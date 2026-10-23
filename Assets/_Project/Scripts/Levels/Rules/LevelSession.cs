using System;
using System.Collections.Generic;
using Flowers.Board;

namespace Flowers.Levels
{
    public enum LevelState
    {
        Playing,
        Won,
        Lost,
    }

    public sealed class GoalProgress
    {
        public LevelGoal Goal { get; }
        public int Current { get; internal set; }
        public int Remaining => Math.Max(0, Goal.Amount - Current);
        public bool IsComplete => Current >= Goal.Amount;

        internal GoalProgress(LevelGoal goal)
        {
            Goal = goal;
        }
    }

    /// <summary>
    /// One attempt at a level: counts moves, score and goal progress, and decides win/lose.
    /// Feed it every swap result from the board; it ignores invalid swaps (they cost nothing).
    /// </summary>
    public sealed class LevelSession
    {
        public const int PointsPerFlower = 10;

        private readonly List<GoalProgress> _goals = new List<GoalProgress>();

        public LevelDefinition Definition { get; }
        public int MovesLeft { get; private set; }
        public int Score { get; private set; }
        public LevelState State { get; private set; } = LevelState.Playing;
        public IReadOnlyList<GoalProgress> Goals => _goals;

        /// <summary>0 while playing or after losing, 1-3 after winning.</summary>
        public int Stars { get; private set; }

        public LevelSession(LevelDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            string problem = definition.Validate();
            if (problem != null) throw new ArgumentException($"Invalid level: {problem}");

            MovesLeft = definition.Moves;
            foreach (var goal in definition.Goals)
                _goals.Add(new GoalProgress(goal));
        }

        /// <summary>Applies one swap. Returns true if it counted as a move.</summary>
        public bool ApplySwap(SwapResult result)
        {
            if (State != LevelState.Playing || result == null || !result.IsValid)
                return false;

            MovesLeft--;

            // Chain reactions are worth more: step 0 x1, step 1 x2, ...
            for (int step = 0; step < result.Steps.Count; step++)
            {
                var cleared = result.Steps[step].Cleared;
                int multiplier = step + 1;
                for (int i = 0; i < cleared.Count; i++)
                {
                    Score += PointsPerFlower * multiplier;
                    CountCollected(cleared[i].Type);
                }
            }

            foreach (var goal in _goals)
                if (goal.Goal.Kind == GoalKind.Score)
                    goal.Current = Score;

            UpdateState();
            return true;
        }

        private void CountCollected(int flowerType)
        {
            foreach (var goal in _goals)
                if (goal.Goal.Kind == GoalKind.CollectFlower && goal.Goal.FlowerType == flowerType)
                    goal.Current++;
        }

        private void UpdateState()
        {
            // Finishing the goals on the very last move is a win, not a loss.
            if (AllGoalsComplete())
            {
                State = LevelState.Won;
                Stars = CalculateStars(MovesLeft);
            }
            else if (MovesLeft <= 0)
            {
                State = LevelState.Lost;
            }
        }

        private bool AllGoalsComplete()
        {
            foreach (var goal in _goals)
                if (!goal.IsComplete)
                    return false;
            return true;
        }

        private int CalculateStars(int movesLeft)
        {
            if (movesLeft >= Definition.ThreeStarMovesLeft) return 3;
            if (movesLeft >= Definition.TwoStarMovesLeft) return 2;
            return 1;
        }
    }
}
