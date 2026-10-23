using UnityEngine;

namespace Flowers.Levels
{
    /// <summary>The levels in play order. Drag LevelData assets in here to add or reorder levels.</summary>
    [CreateAssetMenu(menuName = "Flowers/Level List", fileName = "LevelList")]
    public class LevelList : ScriptableObject
    {
        [SerializeField] private LevelData[] levels;

        public int Count => levels == null ? 0 : levels.Length;

        public LevelData Get(int index) => levels[index];

#if UNITY_EDITOR
        public void EditorSetLevels(LevelData[] newLevels) => levels = newLevels;
#endif
    }
}
