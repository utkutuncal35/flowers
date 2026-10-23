using UnityEngine;

namespace Flowers.Levels
{
    /// <summary>
    /// One level, as an asset. Create new ones with right-click → Create → Flowers → Level,
    /// then add them to the LevelList. No code needed.
    /// </summary>
    [CreateAssetMenu(menuName = "Flowers/Level", fileName = "Level")]
    public class LevelData : ScriptableObject
    {
        [Header("Board")]
        [SerializeField, Min(3)] private int width = 7;
        [SerializeField, Min(3)] private int height = 9;
        [Tooltip("How many different flowers appear. Fewer types = more matches = easier.")]
        [SerializeField, Range(3, 5)] private int typeCount = 5;
        [Tooltip("0 = different board every time. Any other number always gives the same board.")]
        [SerializeField] private int seed;

        [Header("Rules")]
        [SerializeField, Min(1)] private int moves = 20;
        [Tooltip("Flower types: 0 Gül (bordo), 1 Lale (kırmızı), 2 Kiraz çiçeği (pembe), 3 Papatya (krem), 4 Kalp (açık pembe).\n" +
                 "Collect Flower: pop Amount flowers of Flower Type. Score: reach Amount points (Flower Type is ignored).")]
        [SerializeField] private LevelGoal[] goals = { LevelGoal.Collect(0, 10) };

        [Header("Stars (moves left when the level is won)")]
        [SerializeField, Min(1)] private int twoStarMovesLeft = 3;
        [SerializeField, Min(1)] private int threeStarMovesLeft = 6;

        public int Seed => seed;

        public LevelDefinition ToDefinition() =>
            new LevelDefinition(width, height, typeCount, moves, goals, twoStarMovesLeft, threeStarMovesLeft);

#if UNITY_EDITOR
        /// <summary>Used by Flowers → Setup Scene to fill in the sample levels.</summary>
        public void EditorSetup(int boardWidth, int boardHeight, int flowerTypes, int moveCount,
            LevelGoal[] levelGoals, int twoStars, int threeStars)
        {
            width = boardWidth;
            height = boardHeight;
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
