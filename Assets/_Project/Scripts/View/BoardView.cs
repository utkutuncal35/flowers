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
    /// </summary>
    public class BoardView : MonoBehaviour
    {
        [SerializeField] private BoardViewConfig config;
        [SerializeField] private FlowerView flowerPrefab;
        [SerializeField] private Camera targetCamera;

        private struct MoveAnim
        {
            public Transform Target;
            public Vector3 From;
            public Vector3 To;
            public float Duration;
        }

        private readonly List<MoveAnim> _moves = new List<MoveAnim>();
        private readonly List<FlowerView> _popping = new List<FlowerView>();

        private BoardEngine _engine;
        private FlowerView[,] _views;
        private FlowerView[,] _shuffleBuffer;
        private FlowerPool _pool;
        private Transform _flowerRoot;
        private int _width;
        private int _height;
        private float _cell;
        private int _lastScreenWidth;
        private int _lastScreenHeight;
        private bool _ready;

        public bool IsBusy { get; private set; }
        public bool CanAcceptInput => _ready && !IsBusy;
        public float SwipeThresholdWorld => config.SwipeThresholdCells * _cell;

        private void Awake()
        {
            if (!HasRequiredReferences())
            {
                enabled = false;
                return;
            }

            _width = config.Width;
            _height = config.Height;
            _cell = config.CellSize;

            int seed = config.Seed != 0 ? config.Seed : Environment.TickCount;
            _engine = new BoardEngine(_width, _height, config.TypeCount, seed);
            _views = new FlowerView[_width, _height];
            _shuffleBuffer = new FlowerView[_width, _height];

            targetCamera.backgroundColor = config.BackgroundColor;
            BuildBackground();
            _pool = new FlowerPool(flowerPrefab, _flowerRoot, _width * _height + _width * 2);
            _ready = true;
        }

        private void Start()
        {
            FitCamera();
            StartCoroutine(PlayIntro());
        }

        private void LateUpdate()
        {
            if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
                FitCamera();
        }

        /// <summary>World position → board cell. False if the point is outside the board.</summary>
        public bool TryGetCell(Vector3 worldPosition, out Position cell)
        {
            Vector3 local = transform.InverseTransformPoint(worldPosition);
            int x = Mathf.RoundToInt(local.x / _cell + (_width - 1) * 0.5f);
            int y = Mathf.RoundToInt(local.y / _cell + (_height - 1) * 0.5f);
            cell = new Position(x, y);
            return _ready && _engine.Grid.IsInside(cell);
        }

        /// <summary>Called by input. Ignored while busy or if b is off the board.</summary>
        public void RequestSwap(Position a, Position b)
        {
            if (!CanAcceptInput) return;
            if (!_engine.Grid.IsInside(a) || !_engine.Grid.IsInside(b) || !a.IsAdjacentTo(b)) return;

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
            yield return PlayMoves(EaseType.InOutQuad);

            if (!result.IsValid)
            {
                _moves.Clear();
                AddMove(flowerA.transform, posB, posA, config.InvalidSwapReturnDuration);
                AddMove(flowerB.transform, posA, posB, config.InvalidSwapReturnDuration);
                yield return PlayMoves(EaseType.OutBack);
                flowerA.SetOnTop(false);
                IsBusy = false;
                yield break;
            }

            flowerA.SetOnTop(false);
            _views[a.X, a.Y] = flowerB;
            _views[b.X, b.Y] = flowerA;

            for (int i = 0; i < result.Steps.Count; i++)
                yield return PlayCascadeStep(result.Steps[i]);

            if (result.Shuffled)
                yield return PlayShuffle(result);

            CheckViewMatchesBoard();
            IsBusy = false;
        }

        private IEnumerator PlayCascadeStep(CascadeStep step)
        {
            // 1. Pop
            _popping.Clear();
            for (int i = 0; i < step.Cleared.Count; i++)
            {
                var p = step.Cleared[i].Position;
                _popping.Add(_views[p.X, p.Y]);
                _views[p.X, p.Y] = null;
            }
            yield return PlayPops();

            // 2. Fall + refill, together
            _moves.Clear();
            for (int i = 0; i < step.Falls.Count; i++)
            {
                var fall = step.Falls[i];
                var flower = _views[fall.From.X, fall.From.Y];
                _views[fall.From.X, fall.From.Y] = null;
                _views[fall.To.X, fall.To.Y] = flower;
                AddFall(flower.transform, CellToLocal(fall.From.X, fall.From.Y), CellToLocal(fall.To.X, fall.To.Y),
                    fall.From.Y - fall.To.Y);
            }

            for (int i = 0; i < step.Spawns.Count; i++)
            {
                var spawn = step.Spawns[i];
                var flower = SpawnFlower(spawn.Type);
                _views[spawn.Position.X, spawn.Position.Y] = flower;
                int startY = _height + spawn.IndexInColumn;
                AddFall(flower.transform, CellToLocal(spawn.Position.X, startY),
                    CellToLocal(spawn.Position.X, spawn.Position.Y), startY - spawn.Position.Y);
            }
            yield return PlayMoves(EaseType.OutBackSoft);
        }

        private IEnumerator PlayShuffle(SwapResult result)
        {
            if (result.ShuffleRegenerated)
            {
                // Rare fallback: the engine built a brand new board, so pop everything and drop new flowers in.
                _popping.Clear();
                for (int x = 0; x < _width; x++)
                    for (int y = 0; y < _height; y++)
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

        /// <summary>Fills the whole board, dropping each column in from above the way refills arrive.</summary>
        private IEnumerator DropInAllFlowers()
        {
            _moves.Clear();
            var grid = _engine.Grid;
            for (int x = 0; x < _width; x++)
                for (int y = 0; y < _height; y++)
                {
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

        /// <summary>Shrinks and spins every flower in <see cref="_popping"/>, then returns them to the pool.</summary>
        private IEnumerator PlayPops()
        {
            float baseScale = config.FlowerScale * _cell;
            float duration = config.PopDuration;
            float time = 0f;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / duration);
                float scale = baseScale * (1f - Ease.Evaluate(EaseType.InBack, t));
                var rotation = Quaternion.Euler(0f, 0f, 90f * t);
                for (int i = 0; i < _popping.Count; i++)
                {
                    var tr = _popping[i].transform;
                    tr.localScale = new Vector3(scale, scale, 1f);
                    tr.localRotation = rotation;
                }
                yield return null;
            }

            for (int i = 0; i < _popping.Count; i++)
                _pool.Release(_popping[i]);
            _popping.Clear();
        }

        // ---------- Setup ----------

        private FlowerView SpawnFlower(int type)
        {
            var flower = _pool.Get();
            flower.Show(type, config.FlowerSprites[type], config.FlowerScale * _cell);
            return flower;
        }

        private Vector3 CellToLocal(int x, int y) =>
            new Vector3((x - (_width - 1) * 0.5f) * _cell, (y - (_height - 1) * 0.5f) * _cell, 0f);

        private void BuildBackground()
        {
            float boardWidth = _width * _cell;
            float boardHeight = _height * _cell;

            var frame = CreateSpriteChild("Frame", config.FrameSprite, config.FrameColor, -2);
            frame.drawMode = SpriteDrawMode.Sliced;
            frame.size = new Vector2(boardWidth + config.FramePadding * 2f, boardHeight + config.FramePadding * 2f);

            var tiles = new GameObject("Tiles").transform;
            tiles.SetParent(transform, false);
            float tileScale = config.TileScale * _cell;
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    var color = (x + y) % 2 == 0 ? config.TileColorA : config.TileColorB;
                    var tile = CreateSpriteChild($"Tile {x},{y}", config.TileSprite, color, -1);
                    tile.transform.SetParent(tiles, false);
                    tile.transform.localPosition = CellToLocal(x, y);
                    tile.transform.localScale = new Vector3(tileScale, tileScale, 1f);
                }
            }

            // Flowers are only drawn inside this mask, so refills appear to slide in from the board's top edge.
            var maskObject = new GameObject("Flower Mask");
            maskObject.transform.SetParent(transform, false);
            maskObject.transform.localScale = new Vector3(boardWidth, boardHeight, 1f);
            var mask = maskObject.AddComponent<SpriteMask>();
            mask.sprite = config.MaskSprite;

            _flowerRoot = new GameObject("Flowers").transform;
            _flowerRoot.SetParent(transform, false);
        }

        private SpriteRenderer CreateSpriteChild(string objectName, Sprite sprite, Color color, int sortingOrder)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(transform, false);
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

            targetCamera.orthographic = true;
            targetCamera.orthographicSize = Mathf.Max(neededHeight * 0.5f, neededWidth * 0.5f / aspect);
            var boardPos = transform.position;
            targetCamera.transform.position = new Vector3(boardPos.x, boardPos.y, boardPos.z - 10f);
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
                    if (view != null && view.Type == grid[x, y]) continue;

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
                    var flower = SpawnFlower(_engine.Grid[x, y]);
                    flower.transform.localPosition = CellToLocal(x, y);
                    _views[x, y] = flower;
                }
        }
    }
}
