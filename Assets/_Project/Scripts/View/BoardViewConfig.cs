using UnityEngine;

namespace Flowers.View
{
    /// <summary>Board size, look and animation timing. Edit the asset, not the code.</summary>
    [CreateAssetMenu(menuName = "Flowers/Board View Config", fileName = "BoardViewConfig")]
    public class BoardViewConfig : ScriptableObject
    {
        [Header("Board")]
        [SerializeField, Min(3)] private int width = 7;
        [SerializeField, Min(3)] private int height = 9;
        [SerializeField, Range(3, 6)] private int typeCount = 5;
        [Tooltip("0 = different board every time. Any other number always gives the same board (handy for bug reports).")]
        [SerializeField] private int seed;

        [Header("Sprites")]
        [Tooltip("One sprite per flower type, index = type id.")]
        [SerializeField] private Sprite[] flowerSprites;
        [SerializeField] private Sprite tileSprite;
        [Tooltip("Rounded rect with 9-slice borders, drawn behind the tiles.")]
        [SerializeField] private Sprite frameSprite;
        [Tooltip("Plain square. Hides flowers outside the board while they fall in.")]
        [SerializeField] private Sprite maskSprite;

        [Header("Layout")]
        [SerializeField, Min(0.1f)] private float cellSize = 1f;
        [SerializeField, Range(0.3f, 1f)] private float flowerScale = 0.86f;
        [SerializeField, Range(0.3f, 1f)] private float tileScale = 0.94f;
        [SerializeField, Min(0f)] private float framePadding = 0.2f;
        [Tooltip("Empty space between the board frame and the screen edge, in world units.")]
        [SerializeField, Min(0f)] private float screenMargin = 0.35f;

        [Header("Colors")]
        [SerializeField] private Color backgroundColor = new Color32(0xFB, 0xF3, 0xEA, 0xFF);
        [SerializeField] private Color frameColor = new Color32(0x8E, 0x2C, 0x48, 0xFF);
        [SerializeField] private Color tileColorA = new Color32(0xF5, 0xE1, 0xDD, 0xFF);
        [SerializeField] private Color tileColorB = new Color32(0xEC, 0xD0, 0xCD, 0xFF);

        private const float DefaultSwapDuration = 0.25f;
        private const float DefaultInvalidSwapReturnDuration = 0.3f;
        private const float DefaultPopDuration = 0.25f;
        private const float DefaultFallBaseDuration = 0.2f;
        private const float DefaultFallDurationPerCell = 0.06f;
        private const float DefaultShuffleDuration = 0.4f;

        [Header("Animation (seconds)")]
        [SerializeField, Min(0.01f)] private float swapDuration = DefaultSwapDuration;
        [SerializeField, Min(0.01f)] private float invalidSwapReturnDuration = DefaultInvalidSwapReturnDuration;
        [SerializeField, Min(0.01f)] private float popDuration = DefaultPopDuration;
        [SerializeField, Min(0.01f)] private float fallBaseDuration = DefaultFallBaseDuration;
        [SerializeField, Min(0f)] private float fallDurationPerCell = DefaultFallDurationPerCell;
        [SerializeField, Min(0.01f)] private float shuffleDuration = DefaultShuffleDuration;

        [Header("Input")]
        [Tooltip("How far (in cells) the finger has to move before it counts as a swipe.")]
        [SerializeField, Range(0.1f, 1f)] private float swipeThresholdCells = 0.35f;

        public int Width => width;
        public int Height => height;
        public int TypeCount => typeCount;
        public int Seed => seed;
        public Sprite[] FlowerSprites => flowerSprites;
        public Sprite TileSprite => tileSprite;
        public Sprite FrameSprite => frameSprite;
        public Sprite MaskSprite => maskSprite;
        public float CellSize => cellSize;
        public float FlowerScale => flowerScale;
        public float TileScale => tileScale;
        public float FramePadding => framePadding;
        public float ScreenMargin => screenMargin;
        public Color BackgroundColor => backgroundColor;
        public Color FrameColor => frameColor;
        public Color TileColorA => tileColorA;
        public Color TileColorB => tileColorB;
        public float SwapDuration => swapDuration;
        public float InvalidSwapReturnDuration => invalidSwapReturnDuration;
        public float PopDuration => popDuration;
        public float FallBaseDuration => fallBaseDuration;
        public float FallDurationPerCell => fallDurationPerCell;
        public float ShuffleDuration => shuffleDuration;
        public float SwipeThresholdCells => swipeThresholdCells;

        /// <summary>Null if the config is usable, otherwise a message saying what is wrong.</summary>
        public string Validate()
        {
            if (flowerSprites == null || flowerSprites.Length < typeCount)
                return $"needs {typeCount} flower sprites but has {(flowerSprites == null ? 0 : flowerSprites.Length)}";
            for (int i = 0; i < typeCount; i++)
                if (flowerSprites[i] == null)
                    return $"flower sprite {i} is empty";
            if (tileSprite == null) return "tile sprite is empty";
            if (frameSprite == null) return "frame sprite is empty";
            if (maskSprite == null) return "mask sprite is empty";
            return null;
        }

#if UNITY_EDITOR
        /// <summary>Right-click the asset's Inspector header (or the ⋮ menu) to use this.</summary>
        [ContextMenu("Reset Animation Timing To Defaults")]
        private void ResetAnimationTiming()
        {
            UnityEditor.Undo.RecordObject(this, "Reset Animation Timing");
            swapDuration = DefaultSwapDuration;
            invalidSwapReturnDuration = DefaultInvalidSwapReturnDuration;
            popDuration = DefaultPopDuration;
            fallBaseDuration = DefaultFallBaseDuration;
            fallDurationPerCell = DefaultFallDurationPerCell;
            shuffleDuration = DefaultShuffleDuration;
            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>Used by the Setup Scene menu to plug in the generated placeholder sprites.</summary>
        public void EditorAssignSprites(Sprite[] flowers, Sprite tile, Sprite frame, Sprite mask)
        {
            flowerSprites = flowers;
            tileSprite = tile;
            frameSprite = frame;
            maskSprite = mask;
        }
#endif
    }
}
