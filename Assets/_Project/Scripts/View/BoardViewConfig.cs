using UnityEngine;

namespace Flowers.View
{
    /// <summary>How the board looks and moves. Board size comes from each level (LevelData). Edit the asset, not the code.</summary>
    [CreateAssetMenu(menuName = "Flowers/Board View Config", fileName = "BoardViewConfig")]
    public class BoardViewConfig : ScriptableObject
    {
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
        [Tooltip("Part of the screen height kept free at the top for the HUD (0.14 = 14%).")]
        [SerializeField, Range(0f, 0.4f)] private float hudReservedTopFraction = 0.14f;

        [Header("Colors")]
        [Tooltip("Camera clear color, only visible if the background picture doesn't cover the screen.")]
        [SerializeField] private Color backgroundColor = DefaultBackground;
        [SerializeField] private Color frameColor = DefaultFrame;
        [SerializeField] private Color tileColorA = DefaultTileA;
        [SerializeField] private Color tileColorB = DefaultTileB;
        [Tooltip("Soft shadow under the board so it lifts off the garden background.")]
        [SerializeField] private Color boardShadowColor = DefaultShadow;
        [SerializeField] private Vector2 boardShadowOffset = new Vector2(0f, -0.18f);

        // Current look. When these change, bump LookVersion so Setup Scene updates existing config assets once.
        private const int LookVersion = 2;
        private static readonly Color DefaultBackground = new Color32(0xFC, 0xE0, 0xDC, 0xFF);
        private static readonly Color DefaultFrame = new Color32(0x8E, 0x2C, 0x48, 0xF2);
        private static readonly Color DefaultTileA = new Color32(0xFF, 0xF7, 0xEE, 0xF0);
        private static readonly Color DefaultTileB = new Color32(0xFB, 0xE3, 0xE8, 0xF0);
        private static readonly Color DefaultShadow = new Color32(0x4A, 0x0D, 0x22, 0x50);

        // Starts at 0 on purpose: assets saved before this field existed load it as 0 and get upgraded.
        [SerializeField, HideInInspector] private int lookVersion;

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

        public Sprite[] FlowerSprites => flowerSprites;
        public Sprite TileSprite => tileSprite;
        public Sprite FrameSprite => frameSprite;
        public Sprite MaskSprite => maskSprite;
        public float CellSize => cellSize;
        public float FlowerScale => flowerScale;
        public float TileScale => tileScale;
        public float FramePadding => framePadding;
        public float ScreenMargin => screenMargin;
        public float HudReservedTopFraction => hudReservedTopFraction;
        public Color BackgroundColor => backgroundColor;
        public Color FrameColor => frameColor;
        public Color TileColorA => tileColorA;
        public Color TileColorB => tileColorB;
        public Color BoardShadowColor => boardShadowColor;
        public Vector2 BoardShadowOffset => boardShadowOffset;
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
            if (flowerSprites == null || flowerSprites.Length < 3)
                return $"needs at least 3 flower sprites but has {(flowerSprites == null ? 0 : flowerSprites.Length)}";
            for (int i = 0; i < flowerSprites.Length; i++)
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
        /// <summary>
        /// Used by Setup Scene: if this asset was made with an older look, switch its colors to the current
        /// defaults once. Later Inspector edits are kept, because the version then matches.
        /// </summary>
        public bool EditorUpgradeLook()
        {
            if (lookVersion >= LookVersion) return false;
            backgroundColor = DefaultBackground;
            frameColor = DefaultFrame;
            tileColorA = DefaultTileA;
            tileColorB = DefaultTileB;
            boardShadowColor = DefaultShadow;
            lookVersion = LookVersion;
            return true;
        }

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
