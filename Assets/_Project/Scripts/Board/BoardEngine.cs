using System;
using System.Collections.Generic;

namespace Flowers.Board
{
    public readonly struct ClearedFlower
    {
        public readonly Position Position;
        public readonly int Type;

        public ClearedFlower(Position position, int type)
        {
            Position = position;
            Type = type;
        }
    }

    /// <summary>One round of a cascade: matches pop, flowers fall, new ones spawn.</summary>
    public sealed class CascadeStep
    {
        public IReadOnlyList<Match> Matches { get; }
        public IReadOnlyList<ClearedFlower> Cleared { get; }
        public IReadOnlyList<FlowerMove> Falls { get; }
        public IReadOnlyList<FlowerSpawn> Spawns { get; }

        public CascadeStep(IReadOnlyList<Match> matches, IReadOnlyList<ClearedFlower> cleared,
            IReadOnlyList<FlowerMove> falls, IReadOnlyList<FlowerSpawn> spawns)
        {
            Matches = matches;
            Cleared = cleared;
            Falls = falls;
            Spawns = spawns;
        }
    }

    /// <summary>
    /// Everything that happened because of one swap, in order. The view plays this back as animations.
    /// Steps[0] is the swap's own match, Steps[1+] are chain reactions (combo = step index).
    /// </summary>
    public sealed class SwapResult
    {
        public Position A { get; }
        public Position B { get; }

        /// <summary>False: the swap made no match. The board is unchanged, the view swaps back, no move is spent.</summary>
        public bool IsValid { get; }

        public IReadOnlyList<CascadeStep> Steps { get; }

        /// <summary>True if the board ran out of moves after the cascade and was shuffled.</summary>
        public bool Shuffled { get; }

        /// <summary>Where each flower went during the shuffle. Empty if not shuffled or <see cref="ShuffleRegenerated"/>.</summary>
        public IReadOnlyList<FlowerMove> ShuffleMoves { get; }

        /// <summary>Shuffling failed and the board was rebuilt from scratch; the view should rebuild from the board.</summary>
        public bool ShuffleRegenerated { get; }

        private static readonly IReadOnlyList<CascadeStep> NoSteps = new CascadeStep[0];
        private static readonly IReadOnlyList<FlowerMove> NoMoves = new FlowerMove[0];

        private SwapResult(Position a, Position b, bool isValid, IReadOnlyList<CascadeStep> steps,
            bool shuffled, IReadOnlyList<FlowerMove> shuffleMoves, bool shuffleRegenerated)
        {
            A = a;
            B = b;
            IsValid = isValid;
            Steps = steps;
            Shuffled = shuffled;
            ShuffleMoves = shuffleMoves;
            ShuffleRegenerated = shuffleRegenerated;
        }

        public static SwapResult Invalid(Position a, Position b) =>
            new SwapResult(a, b, false, NoSteps, false, NoMoves, false);

        public static SwapResult Valid(Position a, Position b, IReadOnlyList<CascadeStep> steps,
            bool shuffled, IReadOnlyList<FlowerMove> shuffleMoves, bool shuffleRegenerated) =>
            new SwapResult(a, b, true, steps, shuffled, shuffleMoves ?? NoMoves, shuffleRegenerated);
    }

    /// <summary>
    /// The board rules in one place: swap, validate, resolve cascades, reshuffle when stuck.
    /// Knows nothing about Unity. The view calls <see cref="TrySwap"/> and animates the result.
    /// </summary>
    public sealed class BoardEngine
    {
        // A real cascade never gets close to this; it only guards against an infinite loop bug.
        private const int MaxCascadeSteps = 100;

        private readonly Random _random;

        public BoardGrid Grid { get; }

        public BoardEngine(int width, int height, int typeCount, int seed)
            : this(width, height, typeCount, new Random(seed)) { }

        public BoardEngine(int width, int height, int typeCount, Random random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
            Grid = BoardGenerator.Generate(width, height, typeCount, random);
        }

        /// <summary>Wraps an existing board (mainly for tests). The board should be full and have no matches.</summary>
        public BoardEngine(BoardGrid board, Random random)
        {
            Grid = board ?? throw new ArgumentNullException(nameof(board));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public SwapResult TrySwap(Position a, Position b)
        {
            if (!MoveFinder.IsValidSwap(Grid, a, b))
                return SwapResult.Invalid(a, b);

            Grid.Swap(a, b);
            var steps = ResolveCascades();

            if (MoveFinder.HasValidMove(Grid))
                return SwapResult.Valid(a, b, steps, false, null, false);

            var shuffleMoves = BoardShuffler.Shuffle(Grid, _random, out bool regenerated);
            return SwapResult.Valid(a, b, steps, true, shuffleMoves, regenerated);
        }

        private List<CascadeStep> ResolveCascades()
        {
            var steps = new List<CascadeStep>();

            for (int i = 0; i < MaxCascadeSteps; i++)
            {
                var matches = MatchFinder.FindMatches(Grid);
                if (matches.Count == 0) break;

                var cleared = new List<ClearedFlower>();
                foreach (var p in MatchFinder.CollectPositions(matches))
                {
                    cleared.Add(new ClearedFlower(p, Grid[p]));
                    Grid[p] = BoardGrid.Empty;
                }

                var falls = Gravity.ApplyGravity(Grid);
                var spawns = Gravity.Refill(Grid, _random);
                steps.Add(new CascadeStep(matches, cleared, falls, spawns));
            }

            return steps;
        }
    }
}
