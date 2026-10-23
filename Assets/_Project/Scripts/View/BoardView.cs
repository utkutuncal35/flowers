using System;
using System.Collections;
using System.Collections.Generic;
using Flowers.Board;
using UnityEngine;

namespace Flowers.View
{
    /// <summary>
    /// Shows the board and plays back what <see cref="BoardEngine"/> decided.
    /// It never decides game rules itself: it asks the engine, then animates the result step by step.
    /// While an animation runs <see cref="IsBusy"/> is true and new swaps are ignored.
    /// Call <see cref="Build"/> to start (or restart) a board; nothing is shown before that.
    /// </summary>
    public class BoardView : MonoBehaviour
    {
        [SerializeField] private BoardViewConfig config;
        [SerializeField] private FlowerView flowerPrefab;
        [SerializeField] private Camera targetCamera;

        /// <summary>Fired when a swap's animations have fully finished (valid or not).</summary>
        public event Action<SwapResult> SwapFinished;

        /// <summary>The two flowers start sliding.</summary>
        public event Action SwapStarted;

        /// <summary>An invalid swap starts bouncing back.</summary>
        public event Action InvalidSwapReturning;

        /// <summary>A cascade step's flowers start popping. int = step index (0 = the swap's own match, 1+ = chain).</summary>
        public event Action<CascadeStep, int> StepPopping;

        // Sorting orders (higher draws on top).
        private const int TileOrder = -1;
        private const int PotOrder = 0;
        private const int VineOrder = 3;

        private struct MoveAnim
        {
            public Transform Target;
            public Vector3 From;
            public Vector3 To;
            public float Duration;
        }

        private readonly List<MoveAnim> _moves = new List<MoveAnim>();
        private readonly List<FlowerView> _popping = new List<FlowerView>();
        private readonly List<Transform> _obstaclesPopping = new List<Transform>();
        private readonly List<float> _obstaclePopScales = new List<float>();

        private BoardEngine _engine;
        private FlowerView[,] _views;
        private FlowerView[,] _shuffleBuffer;
        private SpriteRenderer[,] _tiles;
        private SpriteRenderer[,] _pots;
        private SpriteRenderer[,] _vines;
        private FlowerPool _pool;
        private Transform _flowerRoot;
        private Transform _backgroundRoot;
        private int _width;
        private int _height;
        private float _cell;
        private int _lastScreenWidth;
        private int _lastScreenHeight;
        private bool _ready;

        public bool IsBusy { get; private set; }

        /// <summary>Set false to freeze the board (e.g. while a win/lose screen is open).</summary>
        public bool InputEnabled { get; set; } = true;

        public bool CanAcceptInput => _engine != null && !IsBusy && InputEnabled;
        public float SwipeThresholdWorld => config.SwipeThresholdCells * _cell;
        public BoardViewConfig Config => config;

        private float PotScale => _cell * 0.92f;
        private float VineScale => _cell * 1.0f;
        private float TileScale => config.TileScale * _cell;

        private void Awake()
        {
            if (!HasRequiredReferences())
            {
                enabled = false;
                return;
            }

            _cell = config.CellSize;
            targetCamera.backgroundColor = config.BackgroundColor;

            _flowerRoot = new GameObject("Flowers").transform;
            _flowerRoot.SetParent(transform, false);
            _pool = new FlowerPool(flowerPrefab, _flowerRoot, 0);
            _ready = true;
        }

        private void LateUpdate()
        {
            if (_engine != null && (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight))
                FitCamera();
        }

        /// <summary>
        /// Throws away the current board (even mid-animation) and drops in a new one with the layout's shape
        /// and obstacles. seed 0 = a different board every time.
        /// </summary>
        public void Build(BoardLayout layout, int typeCount, int seed)
        {
            if (!_ready) return;
            if (typeCount > config.FlowerSprites.Length)
            {
                Debug.LogError($"[Flowers] Level wants {typeCount} flower types but BoardViewConfig only has {config.FlowerSprites.Length} sprites.", this);
                return;
            }

            StopAllCoroutines();
            ReleaseAllFlowers();

            _width = layout.Width;
            _height = layout.Height;
            _engine = new BoardEngine(layout, typeCount, new System.Random(seed != 0 ? seed : Environment.TickCount));
            _views = new FlowerView[_width, _height];
            _shuffleBuffer = new FlowerView[_width, _height];
            _pool.Prewarm(_width * _height + _width * 2);

            BuildBackground();
            FitCamera();

            IsBusy = true;
            StartCoroutine(PlayIntro());
        }

        /// <summary>World position → board cell. False if the point is outside the board.</summary>
        public bool TryGetCell(Vector3 worldPosition, out Position cell)
        {
            cell = default;
            if (_engine == null) return false;

            Vector3 local = transform.InverseTransformPoint(worldPosition);
            int x = Mathf.RoundToInt(local.x / _cell + (_width - 1) * 0.5f);
            int y = Mathf.RoundToInt(local.y / _cell + (_height - 1) * 0.5f);
            cell = new Position(x, y);
            return _engine.Grid.IsInside(cell) && !_engine.Grid.IsVoid(cell);
        }

        /// <summary>Called by input. Ignored while busy, off the board, or if either cell can't be moved.</summary>
        public void RequestSwap(Position a, Position b)
        {
            if (!CanAcceptInput) return;
            if (!a.IsAdjacentTo(b) || !_engine.Grid.CanSwap(a) || !_engine.Grid.CanSwap(b)) return;

            IsBusy = true;
            StartCoroutine(PlaySwap(a, b));
        }

        // ---------- Playback ----------

        private IEnumerator PlayIntro()
        {
            IsBusy = true;
            yield return DropInAllFlowers();
            IsBusy = false;
        }

        private IEnumerator PlaySwap(Position a, Position b)
        {
            var result = _engine.TrySwap(a, b);

            var flowerA = _views[a.X, a.Y];
            var flowerB = _views[b.X, b.Y];
            Vector3 posA = CellToLocal(a.X, a.Y);
            Vector3 posB = CellToLocal(b.X, b.Y);
            flowerA.SetOnTop(true);

            _moves.Clear();
            AddMove(flowerA.transform, posA, posB, config.SwapDuration);
            AddMove(flowerB.transform, posB, posA, config.SwapDuration);
            SwapStarted?.Invoke();
            yield return PlayMoves(EaseType.InOutQuad);

            if (!result.IsValid)
            {
                InvalidSwapReturning?.Invoke();
                _moves.Clear();
                AddMove(flowerA.transform, posB, posA, config.InvalidSwapReturnDuration);
                AddMove(flowerB.transform, posA, posB, config.InvalidSwapReturnDuration);
                yield return PlayMoves(EaseType.OutBack);
                flowerA.SetOnTop(false);
                IsBusy = false;
                SwapFinished?.Invoke(result);
                yield break;
            }

            flowerA.SetOnTop(false);
            _views[a.X, a.Y] = flowerB;
            _views[b.X, b.Y] = flowerA;

            for (int i = 0; i < result.Steps.Count; i++)
                yield return PlayCascadeStep(result.Steps[i], i);

            if (result.Shuffled)
                yield return PlayShuffle(result);

            CheckViewMatchesBoard();
            IsBusy = false;
            SwapFinished?.Invoke(result);
        }

        private IEnumerator PlayCascadeStep(CascadeStep step, int stepIndex)
        {
            // 1. Pop flowers, and everything the pops broke.
            _popping.Clear();
            _obstaclesPopping.Clear();
            _obstaclePopScales.Clear();
            for (int i = 0; i < step.Cleared.Count; i++)
            {
                var p = step.Cleared[i].Position;
                _popping.Add(_views[p.X, p.Y]);
                _views[p.X, p.Y] = null;
            }
            for (int i = 0; i < step.VinesFreed.Count; i++)
            {
                var p = step.VinesFreed[i];
                if (_vines[p.X, p.Y] == null) continue;
                _obstaclesPopping.Add(_vines[p.X, p.Y].transform);
                _obstaclePopScales.Add(VineScale);
                _vines[p.X, p.Y] = null;
            }
            for (int i = 0; i < step.PotHits.Count; i++)
            {
                var hit = step.PotHits[i];
                var pot = _pots[hit.Position.X, hit.Position.Y];
                if (pot == null) continue;
                if (hit.LayersLeft > 0)
                {
                    pot.sprite = config.PotCrackedSprite;
                    StartCoroutine(Punch(pot.transform, PotScale));
                }
                else
                {
                    _obstaclesPopping.Add(pot.transform);
                    _obstaclePopScales.Add(PotScale);
                    _pots[hit.Position.X, hit.Position.Y] = null;
                }
            }
            for (int i = 0; i < step.SoilCleared.Count; i++)
            {
                var p = step.SoilCleared[i];
                var tile = _tiles[p.X, p.Y];
                if (tile == null) continue;
                tile.sprite = config.TileSprite;
                tile.color = TileColor(p.X, p.Y);
                StartCoroutine(Punch(tile.transform, TileScale));
            }

            StepPopping?.Invoke(step, stepIndex);
            yield return PlayPops();

            // 2. Fall (straight or diagonally) + new flowers, together.
            _moves.Clear();
            for (int i = 0; i < step.Falls.Count; i++)
            {
                var fall = step.Falls[i];
                var flower = _views[fall.From.X, fall.From.Y];
                _views[fall.From.X, fall.From.Y] = null;
                _views[fall.To.X, fall.To.Y] = flower;
                int cells = Mathf.Max(Mathf.Abs(fall.From.Y - fall.To.Y), Mathf.Abs(fall.From.X - fall.To.X));
                AddFall(flower.transform, CellToLocal(fall.From.X, fall.From.Y), CellToLocal(fall.To.X, fall.To.Y), cells);
            }

            for (int i = 0; i < step.Spawns.Count; i++)
            {
                var spawn = step.Spawns[i];
                var flower = SpawnFlower(spawn.Type);
                _views[spawn.Position.X, spawn.Position.Y] = flower;
                int startY = _height + spawn.IndexInColumn;
                AddFall(flower.transform, CellToLocal(spawn.Column, startY),
                    CellToLocal(spawn.Position.X, spawn.Position.Y), startY - spawn.Position.Y);
            }
            yield return PlayMoves(EaseType.OutBackSoft);
        }

        private IEnumerator PlayShuffle(SwapResult result)
        {
            if (result.ShuffleRegenerated)
            {
                // Rare fallback: the engine re-dealt every flower, so pop them all and drop new ones in.
                _popping.Clear();
                _obstaclesPopping.Clear();
                _obstaclePopScales.Clear();
                for (int x = 0; x < _width; x++)
                    for (int y = 0; y < _height; y++)
                        if (_views[x, y] != null)
                        {
                            _popping.Add(_views[x, y]);
                            _views[x, y] = null;
                        }
                yield return PlayPops();
                yield return DropInAllFlowers();
                yield break;
            }

            Array.Copy(_views, _shuffleBuffer, _views.Length);
            _moves.Clear();
            for (int i = 0; i < result.ShuffleMoves.Count; i++)
            {
                var move = result.ShuffleMoves[i];
                var flower = _views[move.From.X, move.From.Y];
                _shuffleBuffer[move.To.X, move.To.Y] = flower;
                AddMove(flower.transform, CellToLocal(move.From.X, move.From.Y), CellToLocal(move.To.X, move.To.Y),
                    config.ShuffleDuration);
            }
            Array.Copy(_shuffleBuffer, _views, _views.Length);
            yield return PlayMoves(EaseType.InOutQuad);
        }

        /// <summary>Fills the board, dropping each column in from above the way refills arrive.</summary>
        private IEnumerator DropInAllFlowers()
        {
            _moves.Clear();
            var grid = _engine.Grid;
            for (int x = 0; x < _width; x++)
                for (int y = 0; y < _height; y++)
                {
                    if (grid[x, y] == BoardGrid.Empty) continue;
                    var flower = SpawnFlower(grid[x, y]);
                    _views[x, y] = flower;
                    AddFall(flower.transform, CellToLocal(x, _height + y), CellToLocal(x, y), _height);
                }
            yield return PlayMoves(EaseType.OutBackSoft);
        }

        // ---------- Tween helpers ----------

        private void AddMove(Transform target, Vector3 from, Vector3 to, float duration)
        {
            target.localPosition = from;
            _moves.Add(new MoveAnim { Target = target, From = from, To = to, Duration = duration });
        }

        private void AddFall(Transform target, Vector3 from, Vector3 to, int cells)
        {
            AddMove(target, from, to, config.FallBaseDuration + config.FallDurationPerCell * cells);
        }

        /// <summary>Plays every queued move at once and waits for the slowest one.</summary>
        private IEnumerator PlayMoves(EaseType ease)
        {
            float longest = 0f;
            for (int i = 0; i < _moves.Count; i++)
                longest = Mathf.Max(longest, _moves[i].Duration);

            float time = 0f;
            while (time < longest)
            {
                time += Time.deltaTime;
                for (int i = 0; i < _moves.Count; i++)
                {
                    var m = _moves[i];
                    float t = Mathf.Clamp01(time / m.Duration);
                    m.Target.localPosition = Vector3.LerpUnclamped(m.From, m.To, Ease.Evaluate(ease, t));
                }
                yield return null;
            }

            for (int i = 0; i < _moves.Count; i++)
                _moves[i].Target.localPosition = _moves[i].To;
            _moves.Clear();
        }

        /// <summary>
        /// Shrinks and spins every flower in <see cref="_popping"/> (then returns them to the pool) and every
        /// broken obstacle in <see cref="_obstaclesPopping"/> (then hides it).
        /// </summary>
        private IEnumerator PlayPops()
        {
            float flowerScale = config.FlowerScale * _cell;
            float duration = config.PopDuration;
            float time = 0f;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / duration);
                float shrink = 1f - Ease.Evaluate(EaseType.InBack, t);
                var rotation = Quaternion.Euler(0f, 0f, 90f * t);
                for (int i = 0; i < _popping.Count; i++)
                {
                    var tr = _popping[i].transform;
                    tr.localScale = new Vector3(flowerScale * shrink, flowerScale * shrink, 1f);
                    tr.localRotation = rotation;
                }
                for (int i = 0; i < _obstaclesPopping.Count; i++)
                {
                    float s = _obstaclePopScales[i] * shrink;
                    _obstaclesPopping[i].localScale = new Vector3(s, s, 1f);
                    _obstaclesPopping[i].localRotation = rotation;
                }
                yield return null;
            }

            for (int i = 0; i < _popping.Count; i++)
                _pool.Release(_popping[i]);
            _popping.Clear();
            for (int i = 0; i < _obstaclesPopping.Count; i++)
                _obstaclesPopping[i].gameObject.SetActive(false);
            _obstaclesPopping.Clear();
            _obstaclePopScales.Clear();
        }

        /// <summary>Quick squash-and-back on a hit (cracked pot, cleared soil).</summary>
        private IEnumerator Punch(Transform target, float baseScale)
        {
            const float duration = 0.22f;
            float time = 0f;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / duration);
                float s = baseScale * (1f + Mathf.Sin(t * Mathf.PI) * 0.18f);
                target.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            target.localScale = new Vector3(baseScale, baseScale, 1f);
        }

        // ---------- Setup ----------

        private FlowerView SpawnFlower(int type)
        {
            var flower = _pool.Get();
            flower.Show(type, config.FlowerSprites[type], config.FlowerScale * _cell);
            return flower;
        }

        public Vector3 CellToWorld(Position p) => transform.TransformPoint(CellToLocal(p.X, p.Y));

        private Vector3 CellToLocal(int x, int y) =>
            new Vector3((x - (_width - 1) * 0.5f) * _cell, (y - (_height - 1) * 0.5f) * _cell, 0f);

        private Color TileColor(int x, int y) => (x + y) % 2 == 0 ? config.TileColorA : config.TileColorB;

        /// <summary>Returns every flower (on the board or mid-pop) to the pool.</summary>
        private void ReleaseAllFlowers()
        {
            if (_views != null)
                for (int x = 0; x < _width; x++)
                    for (int y = 0; y < _height; y++)
                        if (_views[x, y] != null)
                        {
                            _pool.Release(_views[x, y]);
                            _views[x, y] = null;
                        }

            foreach (var flower in _popping)
                if (flower.gameObject.activeSelf)
                    _pool.Release(flower);
            _popping.Clear();
            _obstaclesPopping.Clear();
            _obstaclePopScales.Clear();
            _moves.Clear();
        }

        private void BuildBackground()
        {
            if (_backgroundRoot != null) Destroy(_backgroundRoot.gameObject);
            _backgroundRoot = new GameObject("Background").transform;
            _backgroundRoot.SetParent(transform, false);

            var grid = _engine.Grid;
            _tiles = new SpriteRenderer[_width, _height];
            _pots = new SpriteRenderer[_width, _height];
            _vines = new SpriteRenderer[_width, _height];

            bool shaped = false;
            for (int x = 0; x < _width && !shaped; x++)
                for (int y = 0; y < _height; y++)
                    if (grid.IsVoid(new Position(x, y))) { shaped = true; break; }

            if (shaped) BuildShapedFrame(grid);
            else BuildRectangleFrame();

            var cells = new GameObject("Cells").transform;
            cells.SetParent(_backgroundRoot, false);
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    var p = new Position(x, y);
                    if (grid.IsVoid(p)) continue;

                    bool soil = grid.HasSoil(p);
                    var tile = CreateSpriteChild($"Tile {x},{y}", soil ? config.SoilTileSprite : config.TileSprite,
                        soil ? Color.white : TileColor(x, y), TileOrder, cells);
                    tile.transform.localPosition = CellToLocal(x, y);
                    tile.transform.localScale = new Vector3(TileScale, TileScale, 1f);
                    _tiles[x, y] = tile;

                    if (grid.PotLayers(p) > 0)
                    {
                        var pot = CreateSpriteChild($"Pot {x},{y}", config.PotSprite, Color.white, PotOrder, cells);
                        pot.transform.localPosition = CellToLocal(x, y);
                        pot.transform.localScale = new Vector3(PotScale, PotScale, 1f);
                        _pots[x, y] = pot;
                    }

                    if (grid.HasVine(p))
                    {
                        var vine = CreateSpriteChild($"Vine {x},{y}", config.VineSprite, Color.white, VineOrder, cells);
                        vine.transform.localPosition = CellToLocal(x, y);
                        vine.transform.localScale = new Vector3(VineScale, VineScale, 1f);
                        _vines[x, y] = vine;
                    }
                }
            }
        }

        /// <summary>Plain board: one frame, one soft shadow, one mask over the whole rectangle.</summary>
        private void BuildRectangleFrame()
        {
            float boardWidth = _width * _cell;
            float boardHeight = _height * _cell;
            var frameSize = new Vector2(boardWidth + config.FramePadding * 2f, boardHeight + config.FramePadding * 2f);

            var shadow = CreateSpriteChild("Shadow", config.FrameSprite, config.BoardShadowColor, -3, _backgroundRoot);
            shadow.drawMode = SpriteDrawMode.Sliced;
            shadow.size = frameSize + new Vector2(0.15f, 0.15f);
            shadow.transform.localPosition = config.BoardShadowOffset;

            var frame = CreateSpriteChild("Frame", config.FrameSprite, config.FrameColor, -2, _backgroundRoot);
            frame.drawMode = SpriteDrawMode.Sliced;
            frame.size = frameSize;

            // Flowers are only drawn inside the mask, so refills appear to slide in from the board's top edge.
            var maskObject = new GameObject("Flower Mask");
            maskObject.transform.SetParent(_backgroundRoot, false);
            maskObject.transform.localScale = new Vector3(boardWidth, boardHeight, 1f);
            maskObject.AddComponent<SpriteMask>().sprite = config.MaskSprite;
        }

        /// <summary>
        /// Shaped board (heart, diamond...): a frame piece and a mask piece per cell, so the frame and the
        /// visible area follow the shape. Pieces overlap, which joins them into one outline.
        /// </summary>
        private void BuildShapedFrame(BoardGrid grid)
        {
            var frames = new GameObject("Frame").transform;
            frames.SetParent(_backgroundRoot, false);
            var masks = new GameObject("Flower Mask").transform;
            masks.SetParent(_backgroundRoot, false);
            float frameSize = _cell + config.FramePadding * 2f;

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    if (grid.IsVoid(new Position(x, y))) continue;

                    var frame = CreateSpriteChild($"Frame {x},{y}", config.FrameSprite, config.FrameColor, -2, frames);
                    frame.drawMode = SpriteDrawMode.Sliced;
                    frame.size = new Vector2(frameSize, frameSize);
                    frame.transform.localPosition = CellToLocal(x, y);

                    var maskObject = new GameObject($"Mask {x},{y}");
                    maskObject.transform.SetParent(masks, false);
                    maskObject.transform.localPosition = CellToLocal(x, y);
                    maskObject.transform.localScale = new Vector3(_cell, _cell, 1f);
                    maskObject.AddComponent<SpriteMask>().sprite = config.MaskSprite;
                }
            }
        }

        private SpriteRenderer CreateSpriteChild(string objectName, Sprite sprite, Color color, int sortingOrder, Transform parent)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            return sr;
        }

        private void FitCamera()
        {
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;

            float extra = (config.FramePadding + config.ScreenMargin) * 2f;
            float neededWidth = _width * _cell + extra;
            float neededHeight = _height * _cell + extra;
            float aspect = Mathf.Max(0.01f, targetCamera.aspect);

            // The top part of the screen is kept free for the HUD. The board hangs just below it;
            // any spare height ends up at the bottom, where the garden's flower bushes show.
            float reserved = config.HudReservedTopFraction;
            float size = Mathf.Max(neededHeight * 0.5f / (1f - reserved), neededWidth * 0.5f / aspect);

            var boardPos = transform.position;
            float boardTop = boardPos.y + _height * _cell * 0.5f + config.FramePadding;
            float viewTop = boardTop + config.ScreenMargin + size * 2f * reserved;

            targetCamera.orthographic = true;
            targetCamera.orthographicSize = size;
            targetCamera.transform.position = new Vector3(boardPos.x, viewTop - size, boardPos.z - 10f);
        }

        private bool HasRequiredReferences()
        {
            string problem = null;
            if (config == null) problem = "Config is not assigned";
            else if (flowerPrefab == null) problem = "Flower Prefab is not assigned";
            else if (targetCamera == null) problem = "Target Camera is not assigned";
            else
            {
                string configProblem = config.Validate();
                if (configProblem != null) problem = $"BoardViewConfig '{config.name}' {configProblem}";
            }

            if (problem == null) return true;
            Debug.LogError($"[Flowers] BoardView on '{name}': {problem}. Run Flowers → Setup Scene.", this);
            return false;
        }

        /// <summary>Safety net: if the view ever drifts from the rules, say so loudly and rebuild.</summary>
        private void CheckViewMatchesBoard()
        {
            var grid = _engine.Grid;
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    var view = _views[x, y];
                    int expected = grid[x, y];
                    if (view == null ? expected == BoardGrid.Empty : view.Type == expected) continue;

                    Debug.LogError($"[Flowers] View is out of sync with the board at ({x},{y}). Rebuilding. Board:\n{grid}", this);
                    RebuildAllFlowers();
                    return;
                }
            }
        }

        private void RebuildAllFlowers()
        {
            for (int x = 0; x < _width; x++)
                for (int y = 0; y < _height; y++)
                {
                    if (_views[x, y] != null) _pool.Release(_views[x, y]);
                    _views[x, y] = null;
                    if (_engine.Grid[x, y] == BoardGrid.Empty) continue;
                    var flower = SpawnFlower(_engine.Grid[x, y]);
                    flower.transform.localPosition = CellToLocal(x, y);
                    _views[x, y] = flower;
                }
        }
    }
}
