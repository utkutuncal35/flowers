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

    public readonly struct PotHit
    {
        public readonly Position Position;

        /// <summary>Layers left after the hit; 0 = the pot broke and the cell is free.</summary>
        public readonly int LayersLeft;

        public PotHit(Position position, int layersLeft)
        {
            Position = position;
            LayersLeft = layersLeft;
        }
    }

    /// <summary>One round of a cascade: matches pop (and hit obstacles), flowers fall, new ones drop in.</summary>
    public sealed class CascadeStep
    {
        private static readonly Position[] NoPositions = new Position[0];
        private static readonly PotHit[] NoPotHits = new PotHit[0];

        public IReadOnlyList<Match> Matches { get; }
        public IReadOnlyList<ClearedFlower> Cleared { get; }
        public IReadOnlyList<FlowerMove> Falls { get; }
        public IReadOnlyList<FlowerSpawn> Spawns { get; }

        /// <summary>Vines that came off (the flower stays and can move again).</summary>
        public IReadOnlyList<Position> VinesFreed { get; }

        public IReadOnlyList<PotHit> PotHits { get; }

        /// <summary>Soil cells whose flower popped.</summary>
        public IReadOnlyList<Position> SoilCleared { get; }

        public CascadeStep(IReadOnlyList<Match> matches, IReadOnlyList<ClearedFlower> cleared,
            IReadOnlyList<FlowerMove> falls, IReadOnlyList<FlowerSpawn> spawns,
            IReadOnlyList<Position> vinesFreed = null, IReadOnlyList<PotHit> potHits = null,
            IReadOnlyList<Position> soilCleared = null)
        {
            Matches = matches;
            Cleared = cleared;
            Falls = falls;
            Spawns = spawns;
            VinesFreed = vinesFreed ?? NoPositions;
            PotHits = potHits ?? NoPotHits;
            SoilCleared = soilCleared ?? NoPositions;
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

        /// <summary>Shuffling failed and the flowers were re-dealt from scratch; the view should rebuild from the board.</summary>
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
    /// The board rules in one place: swap, validate, resolve cascades and obstacles, reshuffle when stuck.
    /// Knows nothing about Unity. The view calls <see cref="TrySwap"/> and animates the result.
    /// </summary>
    public sealed class BoardEngine
    {
        // A real cascade never gets close to this; it only guards against an infinite loop bug.
        private const int MaxCascadeSteps = 100;

        private static readonly Position[] Neighbours =
            { new Position(1, 0), new Position(-1, 0), new Position(0, 1), new Position(0, -1) };

        private readonly Random _random;

        public BoardGrid Grid { get; }

        public BoardEngine(int width, int height, int typeCount, int seed)
            : this(BoardLayout.Rectangle(width, height), typeCount, new Random(seed)) { }

        public BoardEngine(int width, int height, int typeCount, Random random)
            : this(BoardLayout.Rectangle(width, height), typeCount, random) { }

        public BoardEngine(BoardLayout layout, int typeCount, Random random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
            Grid = BoardGenerator.Generate(layout, typeCount, random);
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

                var matched = MatchFinder.CollectPositions(matches);
                var cleared = new List<ClearedFlower>();
                var vinesFreed = new List<Position>();
                var potHits = new List<PotHit>();
                var soilCleared = new List<Position>();

                // 1. Matched cells: a vine comes off (its flower stays), any other flower pops.
                foreach (var p in matched)
                {
                    if (Grid.HasVine(p))
                    {
                        Grid.SetVine(p, false);
                        vinesFreed.Add(p);
                        continue;
                    }
                    cleared.Add(new ClearedFlower(p, Grid[p]));
                    Grid[p] = BoardGrid.Empty;
                    if (Grid.HasSoil(p))
                    {
                        Grid.SetSoil(p, false);
                        soilCleared.Add(p);
                    }
                }

                // 2. Neighbours of the match take one hit each (once per step, however many pops touch them).
                var hit = new HashSet<Position>();
                foreach (var p in matched)
                {
                    foreach (var offset in Neighbours)
                    {
                        var n = new Position(p.X + offset.X, p.Y + offset.Y);
                        if (!Grid.IsInside(n) || matched.Contains(n) || !hit.Add(n)) continue;

                        int layers = Grid.PotLayers(n);
                        if (layers > 0)
                        {
                            Grid.SetPot(n, layers - 1);
                            potHits.Add(new PotHit(n, layers - 1));
                        }
                        else if (Grid.HasVine(n))
                        {
                            Grid.SetVine(n, false);
                            vinesFreed.Add(n);
                        }
                    }
                }

                // 3. Fall and refill.
                var falls = new List<FlowerMove>();
                var spawns = new List<FlowerSpawn>();
                Gravity.Settle(Grid, _random, falls, spawns);
                steps.Add(new CascadeStep(matches, cleared, falls, spawns, vinesFreed, potHits, soilCleared));
            }

            return steps;
        }
    }
}
