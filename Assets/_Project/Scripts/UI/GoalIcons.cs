using Flowers.Levels;
using UnityEngine;

namespace Flowers.UI
{
    /// <summary>Which picture stands for which goal (flower, score star, pot, vine, soil).</summary>
    public sealed class GoalIcons
    {
        private readonly Sprite[] _flowers;
        private readonly Sprite _score;
        private readonly Sprite _pot;
        private readonly Sprite _vine;
        private readonly Sprite _soil;

        public GoalIcons(Sprite[] flowers, Sprite score, Sprite pot, Sprite vine, Sprite soil)
        {
            _flowers = flowers;
            _score = score;
            _pot = pot;
            _vine = vine;
            _soil = soil;
        }

        public Sprite For(LevelGoal goal)
        {
            switch (goal.Kind)
            {
                case GoalKind.CollectFlower: return _flowers[goal.FlowerType];
                case GoalKind.BreakPots: return _pot;
                case GoalKind.FreeVines: return _vine;
                case GoalKind.ClearSoil: return _soil;
                default: return _score;
            }
        }
    }
}
