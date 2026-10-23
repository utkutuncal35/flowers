using Flowers.Levels;

namespace Flowers.Editor
{
    /// <summary>One level as designed: board drawing, flower types, goals and the difficulty target.</summary>
    public sealed class LevelSpec
    {
        public string Name;
        public string[] Layout;
        public int Types = 5;
        public LevelGoal[] Goals;

        /// <summary>
        /// Share of games a casual-player bot should win (the difficulty target). The level tuner turns this into
        /// the move count and star thresholds in <see cref="LevelTuning"/>.
        /// </summary>
        public float TargetWinRate;
    }

    /// <summary>
    /// The 30 levels, in order. Pure data (no Unity) so the tuning simulation uses exactly the same levels.
    /// Flowers → Rebuild Levels writes these (plus the tuned moves/stars) into LevelData assets and the LevelList.
    ///
    /// Layout characters: '.' flower, 'x' no cell, 'v' vine-wrapped flower, 'p' pot, 'P' 2-layer pot,
    /// 's' soil, 'S' soil under a vine. Flower types: 0 rose, 1 tulip, 2 blossom, 3 daisy, 4 heart.
    /// Obstacle goals with amount 0 mean "all of them".
    ///
    /// Curve: 1-5 learn the basics, then each obstacle gets an easy intro level before it is mixed with others.
    /// 15 and 25 are breathers, 20 is a mid-game highlight, 30 the finale (heart-shaped board, everything).
    /// </summary>
    public static class LevelCatalog
    {
        private const int Rose = 0, Tulip = 1, Blossom = 2, Daisy = 3, Heart = 4;

        private static LevelGoal Collect(int type, int amount) => LevelGoal.Collect(type, amount);
        private static LevelGoal Score(int points) => LevelGoal.ReachScore(points);
        private static readonly LevelGoal AllPots = LevelGoal.Obstacle(GoalKind.BreakPots);
        private static readonly LevelGoal AllVines = LevelGoal.Obstacle(GoalKind.FreeVines);
        private static readonly LevelGoal AllSoil = LevelGoal.Obstacle(GoalKind.ClearSoil);

        private static string[] Rect(int width, int height)
        {
            var rows = new string[height];
            for (int i = 0; i < height; i++) rows[i] = new string('.', width);
            return rows;
        }

        private static readonly string[] HeartBoard =
        {
            "x..x..x",
            ".......",
            ".......",
            ".......",
            "x.....x",
            "xx...xx",
            "xxx.xxx",
        };

        public static readonly LevelSpec[] All =
        {
            // ---------- Basics ----------
            new LevelSpec { Name = "Level01", Layout = Rect(7, 8), Types = 4, Goals = new[] { Collect(Blossom, 15) }, TargetWinRate = 0.99f },
            new LevelSpec { Name = "Level02", Layout = Rect(7, 8), Types = 4, Goals = new[] { Collect(Daisy, 18) }, TargetWinRate = 0.98f },
            new LevelSpec { Name = "Level03", Layout = Rect(7, 9), Goals = new[] { Collect(Rose, 20) }, TargetWinRate = 0.97f },
            new LevelSpec { Name = "Level04", Layout = Rect(7, 9), Goals = new[] { Score(2000) }, TargetWinRate = 0.95f },
            new LevelSpec { Name = "Level05", Layout = Rect(7, 9), Goals = new[] { Collect(Heart, 15), Collect(Tulip, 15) }, TargetWinRate = 0.93f },

            // ---------- Soil ----------
            new LevelSpec
            {
                Name = "Level06", TargetWinRate = 0.95f, Goals = new[] { AllSoil },
                Layout = new[] { ".......", ".......", ".sssss.", ".sssss.", ".sssss.", ".......", ".......", ".......", "......." },
            },
            new LevelSpec
            {
                Name = "Level07", TargetWinRate = 0.9f, Goals = new[] { AllSoil, Collect(Blossom, 15) },
                Layout = new[] { ".......", ".......", "sssssss", ".......", ".......", "sssssss", ".......", ".......", "......." },
            },

            // ---------- Vines ----------
            new LevelSpec
            {
                Name = "Level08", TargetWinRate = 0.95f, Goals = new[] { AllVines, Collect(Tulip, 15) },
                Layout = new[] { ".......", ".......", ".v.v.v.", ".......", ".v.v.v.", ".......", ".......", ".......", "......." },
            },

            // ---------- Shaped board ----------
            new LevelSpec
            {
                Name = "Level09", TargetWinRate = 0.9f, Goals = new[] { Collect(Daisy, 22) },
                Layout = new[] { "xx...xx", "x.....x", ".......", ".......", ".......", ".......", ".......", "x.....x", "xx...xx" },
            },

            // ---------- Pots ----------
            new LevelSpec
            {
                Name = "Level10", TargetWinRate = 0.92f, Goals = new[] { AllPots },
                Layout = new[] { ".......", ".......", "p.....p", ".......", ".p.p.p.", ".......", "p.....p", ".......", "......." },
            },
            new LevelSpec
            {
                Name = "Level11", TargetWinRate = 0.85f, Goals = new[] { AllPots, Collect(Rose, 18) },
                Layout = new[] { ".......", ".......", ".......", "ppp.ppp", ".......", ".......", ".......", ".......", "......." },
            },
            new LevelSpec
            {
                Name = "Level12", TargetWinRate = 0.85f, Goals = new[] { AllSoil },
                Layout = new[] { ".......", ".......", ".......", "sssssss", "sssssss", "..sss..", ".......", ".......", "......." },
            },
            new LevelSpec
            {
                Name = "Level13", TargetWinRate = 0.82f, Goals = new[] { AllVines, AllSoil },
                Layout = new[] { ".......", ".v...v.", "..v.v..", ".SsSsS.", "sssssss", ".......", ".......", ".......", "......." },
            },
            new LevelSpec
            {
                Name = "Level14", TargetWinRate = 0.85f, Goals = new[] { AllPots },
                Layout = new[] { ".......", ".P...P.", ".......", "..P.P..", ".......", ".P...P.", ".......", "...P...", "......." },
            },

            // ---------- Breather: heart board ----------
            new LevelSpec { Name = "Level15", Layout = HeartBoard, TargetWinRate = 0.95f, Goals = new[] { Collect(Blossom, 18) } },

            new LevelSpec
            {
                Name = "Level16", TargetWinRate = 0.8f, Goals = new[] { AllVines, Collect(Daisy, 20) },
                Layout = new[] { ".......", "x.v.v.x", "xx...xx", "xx.v.xx", "xx...xx", "x.v.v.x", ".......", ".v...v.", "......." },
            },
            new LevelSpec
            {
                Name = "Level17", TargetWinRate = 0.78f, Goals = new[] { AllPots, Collect(Tulip, 20) },
                Layout = new[] { ".......", ".......", ".......", "pp.p.pp", ".......", ".......", ".......", ".......", "......." },
            },
            new LevelSpec
            {
                Name = "Level18", TargetWinRate = 0.78f, Goals = new[] { AllSoil },
                Layout = new[] { ".......", "...s...", "..sss..", ".sssss.", "sssssss", ".sssss.", "..sss..", "...s...", "......." },
            },
            new LevelSpec
            {
                Name = "Level19", TargetWinRate = 0.75f, Goals = new[] { AllVines, AllPots, Collect(Heart, 12) },
                Layout = new[] { ".......", ".v.P.v.", "p.....p", ".P.v.P.", "p.....p", ".v.P.v.", ".......", ".......", "......." },
            },

            // ---------- Highlight: heart with soil in the middle ----------
            new LevelSpec
            {
                Name = "Level20", TargetWinRate = 0.8f, Goals = new[] { AllSoil, Collect(Rose, 18) },
                Layout = new[] { "x..x..x", ".......", "..sss..", "..sss..", "x.sss.x", "xx...xx", "xxx.xxx" },
            },

            new LevelSpec
            {
                Name = "Level21", TargetWinRate = 0.72f, Goals = new[] { AllPots },
                Layout = new[] { ".......", ".PP.PP.", ".......", "p.p.p.p", ".......", ".PP.PP.", ".......", ".......", "......." },
            },
            new LevelSpec
            {
                Name = "Level22", TargetWinRate = 0.72f, Goals = new[] { AllVines, AllSoil, Collect(Blossom, 15) },
                Layout = new[] { ".......", ".......", ".......", ".......", ".......", "sssssss", "sSSSSSs", "sssssss", "......." },
            },
            new LevelSpec
            {
                Name = "Level23", TargetWinRate = 0.72f, Goals = new[] { Score(4000) },
                Layout = new[] { "xx...xx", "x.....x", ".......", ".......", ".......", ".......", ".......", "x.....x", "xx...xx" },
            },
            new LevelSpec
            {
                Name = "Level24", TargetWinRate = 0.7f, Goals = new[] { AllPots, Collect(Daisy, 15) },
                Layout = new[] { ".......", ".p...p.", ".P...P.", ".P...P.", ".......", ".P...P.", ".p...p.", ".......", "......." },
            },

            // ---------- Breather ----------
            new LevelSpec { Name = "Level25", Layout = Rect(7, 9), TargetWinRate = 0.92f, Goals = new[] { Collect(Blossom, 18), Collect(Heart, 18) } },

            new LevelSpec
            {
                Name = "Level26", TargetWinRate = 0.68f, Goals = new[] { AllVines, AllPots, AllSoil, Collect(Tulip, 12) },
                Layout = new[] { "x.....x", ".v...v.", "..p.p..", "s.sPs.s", "sssssss", "..vvv..", ".......", ".......", "......." },
            },
            new LevelSpec
            {
                Name = "Level27", TargetWinRate = 0.68f, Goals = new[] { AllSoil, AllVines },
                Layout = new[] { ".......", "sssssss", "sSsssSs", ".......", ".......", "sssSsss", "sssssss", ".......", "......." },
            },
            new LevelSpec
            {
                Name = "Level28", TargetWinRate = 0.66f, Goals = new[] { AllPots, AllVines, Collect(Tulip, 15) },
                Layout = new[] { ".......", "p.v.v.p", ".p.v.p.", "...v...", ".p.v.p.", "p.v.v.p", ".......", ".......", "......." },
            },
            new LevelSpec
            {
                Name = "Level29", TargetWinRate = 0.65f, Goals = new[] { AllSoil, AllPots },
                Layout = new[] { ".......", ".......", "sssssss", "sssssss", "ssPsPss", "sssssss", "sssssss", ".......", "......." },
            },

            // ---------- Finale: heart board with everything ----------
            new LevelSpec
            {
                Name = "Level30", TargetWinRate = 0.7f, Goals = new[] { AllSoil, AllVines, AllPots, Collect(Rose, 18) },
                Layout = new[] { "x..x..x", ".v.v.v.", ".PsSsP.", "p.sss.p", "x.sSs.x", "xx.s.xx", "xxx.xxx" },
            },
        };
    }
}
