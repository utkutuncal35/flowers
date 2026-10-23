using System;
using System.Collections.Generic;

namespace Flowers.Levels
{
    public enum GoalKind
    {
        /// <summary>Pop N flowers of one type.</summary>
        CollectFlower = 0,
        /// <summary>Reach N points.</summary>
        Score = 1,
    }

    [Serializable]
    public struct LevelGoal
    {
        public GoalKind Kind;
        /// <summary>Only used by <see cref="GoalKind.CollectFlower"/>.</summary>
        public int FlowerType;
        /// <summary>How many flowers, or how many points.</summary>
        public int Amount;

        public static LevelGoal Collect(int flowerType, int amount) =>
            new LevelGoal { Kind = GoalKind.CollectFlower, FlowerType = flowerType, Amount = amount };

        public static LevelGoal ReachScore(int points) =>
            new LevelGoal { Kind = GoalKind.Score, Amount = points };
    }

    /// <summary>
    /// Everything the rules need to know about one level. Built from a LevelData asset in Unity,
    /// or directly in tests.
    /// </summary>
    public sealed class LevelDefinition
    {
        public int Width { get; }
        public int Height { get; }
        public int TypeCount { get; }
        public int Moves { get; }
        public IReadOnlyList<LevelGoal> Goals { get; }

        /// <summary>Win with at least this many moves left → 2 stars.</summary>
        public int TwoStarMovesLeft { get; }

        /// <summary>Win with at least this many moves left → 3 stars.</summary>
        public int ThreeStarMovesLeft { get; }

        public LevelDefinition(int width, int height, int typeCount, int moves, IReadOnlyList<LevelGoal> goals,
            int twoStarMovesLeft, int threeStarMovesLeft)
        {
            Width = width;
            Height = height;
            TypeCount = typeCount;
            Moves = moves;
            Goals = goals ?? Array.Empty<LevelGoal>();
            TwoStarMovesLeft = twoStarMovesLeft;
            ThreeStarMovesLeft = threeStarMovesLeft;
        }

        /// <summary>Null if the level is playable, otherwise what is wrong (shown in the Console).</summary>
        public string Validate()
        {
            if (Width < 3 || Height < 3) return $"board must be at least 3x3 (is {Width}x{Height})";
            if (TypeCount < 3) return $"needs at least 3 flower types (has {TypeCount})";
            if (Moves < 1) return "needs at least 1 move";
            if (Goals.Count == 0) return "has no goals";

            for (int i = 0; i < Goals.Count; i++)
            {
                var goal = Goals[i];
                if (goal.Amount < 1) return $"goal {i + 1} amount must be at least 1";
                if (goal.Kind == GoalKind.CollectFlower && (goal.FlowerType < 0 || goal.FlowerType >= TypeCount))
                    return $"goal {i + 1} asks for flower type {goal.FlowerType}, but the level only has types 0..{TypeCount - 1}";
            }

            if (TwoStarMovesLeft < 1) return "2-star threshold must be at least 1 move left";
            if (ThreeStarMovesLeft < TwoStarMovesLeft) return "3-star threshold must be >= 2-star threshold";
            if (ThreeStarMovesLeft >= Moves) return "3-star threshold must be smaller than the move count";
            return null;
        }
    }
}
