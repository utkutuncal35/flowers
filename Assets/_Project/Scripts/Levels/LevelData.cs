using System;
using Flowers.Board;
using UnityEngine;

namespace Flowers.Levels
{
    /// <summary>
    /// One level, as an asset. The 30 game levels are generated from LevelCatalog by Flowers → Rebuild Levels;
    /// new ones can also be made by hand (right-click → Create → Flowers → Level) and added to the LevelList.
    /// </summary>
    [CreateAssetMenu(menuName = "Flowers/Level", fileName = "Level")]
    public class LevelData : ScriptableObject
    {
        private const string LayoutHelp =
            "Board drawing, top row first. Leave empty for a plain Width x Height board.\n" +
            "'.' flower   'x' no cell   'v' vine-wrapped flower\n" +
            "'p' pot   'P' 2-layer pot   's' soil   'S' soil under a vine";

        [Header("Board")]
        [Tooltip(LayoutHelp)]
        [SerializeField, TextArea(5, 12)] private string layout = "";
        [Tooltip("Only used when Layout is empty.")]
        [SerializeField, Min(3)] private int width = 7;
        [Tooltip("Only used when Layout is empty.")]
        [SerializeField, Min(3)] private int height = 9;
        [Tooltip("How many different flowers appear. Fewer types = more matches = easier.")]
        [SerializeField, Range(3, 5)] private int typeCount = 5;
        [Tooltip("0 = different board every time. Any other number always gives the same board.")]
        [SerializeField] private int seed;

        [Header("Rules")]
        [SerializeField, Min(1)] private int moves = 20;
        [Tooltip("Flower types: 0 Gül (bordo), 1 Lale (kırmızı), 2 Kiraz çiçeği (pembe), 3 Papatya (krem), 4 Kalp (açık pembe).\n" +
                 "Collect Flower: pop Amount flowers of Flower Type. Score: reach Amount points.\n" +
                 "Break Pots / Free Vines / Clear Soil: Amount 0 = all of them on the board.")]
        [SerializeField] private LevelGoal[] goals = { LevelGoal.Collect(0, 10) };

        [Header("Stars (moves left when the level is won)")]
        [SerializeField, Min(1)] private int twoStarMovesLeft = 3;
        [SerializeField, Min(1)] private int threeStarMovesLeft = 6;

        public int Seed => seed;

        /// <summary>Null if the layout text is fine, otherwise what is wrong with it.</summary>
        public string LayoutError
        {
            get
            {
                if (string.IsNullOrWhiteSpace(layout)) return null;
                try
                {
                    BoardLayout.Parse(layout);
                    return null;
                }
                catch (ArgumentException e)
                {
                    return e.Message;
                }
            }
        }

        /// <summary>Check <see cref="LayoutError"/> first: a broken layout falls back to a plain board.</summary>
        public LevelDefinition ToDefinition()
        {
            if (!string.IsNullOrWhiteSpace(layout) && LayoutError == null)
                return new LevelDefinition(BoardLayout.Parse(layout), typeCount, moves, goals, twoStarMovesLeft, threeStarMovesLeft);
            return new LevelDefinition(width, height, typeCount, moves, goals, twoStarMovesLeft, threeStarMovesLeft);
        }

#if UNITY_EDITOR
        /// <summary>Used by Flowers → Rebuild Levels to write a catalog level into this asset.</summary>
        public void EditorSetup(string layoutText, int flowerTypes, int moveCount, LevelGoal[] levelGoals,
            int twoStars, int threeStars)
        {
            layout = layoutText;
            typeCount = flowerTypes;
            moves = moveCount;
            goals = levelGoals;
            twoStarMovesLeft = twoStars;
            threeStarMovesLeft = threeStars;
            seed = 0;
        }
#endif
    }
}
