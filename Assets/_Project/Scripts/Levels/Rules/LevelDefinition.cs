using System;
using System.Collections.Generic;
using Flowers.Board;

namespace Flowers.Levels
{
    public enum GoalKind
    {
        /// <summary>Pop N flowers of one type.</summary>
        CollectFlower = 0,
        /// <summary>Reach N points.</summary>
        Score = 1,
        /// <summary>Break N pots (a 2-layer pot counts once, when it finally breaks).</summary>
        BreakPots = 2,
        /// <summary>Free N vine-wrapped flowers.</summary>
        FreeVines = 3,
        /// <summary>Pop a flower on N soil cells.</summary>
        ClearSoil = 4,
    }

    [Serializable]
    public struct LevelGoal
    {
        public GoalKind Kind;
        /// <summary>Only used by <see cref="GoalKind.CollectFlower"/>.</summary>
        public int FlowerType;
        /// <summary>How many flowers / points / obstacles. For obstacle goals 0 means "all of them on the board".</summary>
        public int Amount;

        public static LevelGoal Collect(int flowerType, int amount) =>
            new LevelGoal { Kind = GoalKind.CollectFlower, FlowerType = flowerType, Amount = amount };

        public static LevelGoal ReachScore(int points) =>
            new LevelGoal { Kind = GoalKind.Score, Amount = points };

        public static LevelGoal Obstacle(GoalKind kind, int amount = 0) =>
            new LevelGoal { Kind = kind, Amount = amount };

        public bool IsObstacleGoal => Kind == GoalKind.BreakPots || Kind == GoalKind.FreeVines || Kind == GoalKind.ClearSoil;
    }

    /// <summary>
    /// Everything the rules need to know about one level. Built from a LevelData asset in Unity,
    /// or directly in tests.
    /// </summary>
    public sealed class LevelDefinition
    {
        public BoardLayout Layout { get; }
        public int Width => Layout.Width;
        public int Height => Layout.Height;
        public int TypeCount { get; }
        public int Moves { get; }

        /// <summary>Goals with "0 = all" already replaced by the real count from the layout.</summary>
        public IReadOnlyList<LevelGoal> Goals { get; }

        /// <summary>Win with at least this many moves left → 2 stars.</summary>
        public int TwoStarMovesLeft { get; }

        /// <summary>Win with at least this many moves left → 3 stars.</summary>
        public int ThreeStarMovesLeft { get; }

        public LevelDefinition(int width, int height, int typeCount, int moves, IReadOnlyList<LevelGoal> goals,
            int twoStarMovesLeft, int threeStarMovesLeft)
            : this(BoardLayout.Rectangle(Math.Max(1, width), Math.Max(1, height)), typeCount, moves, goals,
                twoStarMovesLeft, threeStarMovesLeft, width, height) { }

        public LevelDefinition(BoardLayout layout, int typeCount, int moves, IReadOnlyList<LevelGoal> goals,
            int twoStarMovesLeft, int threeStarMovesLeft)
            : this(layout, typeCount, moves, goals, twoStarMovesLeft, threeStarMovesLeft, layout.Width, layout.Height) { }

        private readonly int _requestedWidth;
        private readonly int _requestedHeight;

        private LevelDefinition(BoardLayout layout, int typeCount, int moves, IReadOnlyList<LevelGoal> goals,
            int twoStarMovesLeft, int threeStarMovesLeft, int requestedWidth, int requestedHeight)
        {
            Layout = layout ?? throw new ArgumentNullException(nameof(layout));
            TypeCount = typeCount;
            Moves = moves;
            TwoStarMovesLeft = twoStarMovesLeft;
            ThreeStarMovesLeft = threeStarMovesLeft;
            _requestedWidth = requestedWidth;
            _requestedHeight = requestedHeight;

            var resolved = new List<LevelGoal>();
            foreach (var goal in goals ?? Array.Empty<LevelGoal>())
            {
                var g = goal;
                if (g.IsObstacleGoal && g.Amount == 0) g.Amount = ObstaclesOnBoard(g.Kind);
                resolved.Add(g);
            }
            Goals = resolved;
        }

        public int ObstaclesOnBoard(GoalKind kind)
        {
            switch (kind)
            {
                case GoalKind.BreakPots: return Layout.PotCount;
                case GoalKind.FreeVines: return Layout.VineCount;
                case GoalKind.ClearSoil: return Layout.SoilCount;
                default: return 0;
            }
        }

        /// <summary>Null if the level is playable, otherwise what is wrong (shown in the Console).</summary>
        public string Validate()
        {
            if (_requestedWidth < 3 || _requestedHeight < 3) return $"board must be at least 3x3 (is {_requestedWidth}x{_requestedHeight})";
            if (Layout.FlowerCellCount < 9) return "the board needs at least 9 cells that hold flowers";
            if (TypeCount < 3) return $"needs at least 3 flower types (has {TypeCount})";
            if (Moves < 1) return "needs at least 1 move";
            if (Goals.Count == 0) return "has no goals";

            for (int i = 0; i < Goals.Count; i++)
            {
                var goal = Goals[i];
                if (goal.IsObstacleGoal)
                {
                    int onBoard = ObstaclesOnBoard(goal.Kind);
                    if (onBoard == 0) return $"goal {i + 1} ({goal.Kind}) but the layout has none";
                    if (goal.Amount > onBoard) return $"goal {i + 1} asks for {goal.Amount} ({goal.Kind}) but the layout has only {onBoard}";
                }
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
