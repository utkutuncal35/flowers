using System;
using System.Collections.Generic;
using Flowers.Core;
using Flowers.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace Flowers.UI
{
    /// <summary>
    /// Scrolling level map: a meadow with a sky at the top, a winding stone path, one round button per level
    /// (level 1 at the bottom), a bouncing heart above the current level and a big Play button.
    /// Rebuilt from the LevelList every time it opens, so new levels show up automatically.
    /// </summary>
    public class LevelMapScreen : MonoBehaviour
    {
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform content;
        [Tooltip("Inactive template; one copy per level.")]
        [SerializeField] private LevelNodeView nodeTemplate;
        [SerializeField] private Text starsText;
        [SerializeField] private RectTransform marker;
        [SerializeField] private Button playButton;

        [Header("Art")]
        [SerializeField] private Sprite meadowSprite;
        [SerializeField] private Sprite skyCapSprite;
        [Tooltip("Circle stamped along the path.")]
        [SerializeField] private Sprite pathSprite;

        [Header("Layout (canvas units)")]
        [SerializeField, Min(50f)] private float nodeSpacing = 300f;
        [SerializeField, Min(0f)] private float zigzagWidth = 240f;
        [Tooltip("Space under level 1 (the Play button sits there).")]
        [SerializeField, Min(0f)] private float bottomPadding = 420f;
        [Tooltip("Space above the last level, where the sky shows.")]
        [SerializeField, Min(0f)] private float topPadding = 760f;
        [SerializeField, Min(0f)] private float markerHeight = 150f;

        [Header("Path")]
        [SerializeField] private Color pathEdgeColor = new Color32(0xDB, 0xB8, 0x98, 0xFF);
        [SerializeField] private Color pathColor = new Color32(0xFF, 0xF1, 0xDE, 0xFF);
        [SerializeField, Min(1f)] private float pathWidth = 92f;
        [SerializeField, Min(0f)] private float pathEdge = 14f;
        [SerializeField, Min(2f)] private float pathStep = 22f;

        private readonly List<GameObject> _spawned = new List<GameObject>();
        private readonly List<string> _ids = new List<string>();
        private readonly List<Vector2> _points = new List<Vector2>();
        private Action<int> _onLevelChosen;
        private int _current;

        public bool HasReferences =>
            scrollRect != null && content != null && nodeTemplate != null && nodeTemplate.HasReferences &&
            starsText != null && marker != null && playButton != null &&
            meadowSprite != null && skyCapSprite != null && pathSprite != null;

        private void Awake()
        {
            playButton.onClick.AddListener(() => _onLevelChosen?.Invoke(_current));
        }

        public void Show(LevelList levels, LevelProgress progress, Action<int> onLevelChosen)
        {
            _onLevelChosen = onLevelChosen;
            gameObject.SetActive(true);
            Clear();

            _ids.Clear();
            for (int i = 0; i < levels.Count; i++)
                _ids.Add(levels.Get(i) != null ? levels.Get(i).name : $"<missing {i}>");
            _current = progress.CurrentIndex(_ids);

            Canvas.ForceUpdateCanvases();
            // At least one screen tall, so the sky never ends halfway down when there are only a few levels.
            float height = Mathf.Max(scrollRect.viewport.rect.height,
                bottomPadding + Mathf.Max(0, levels.Count - 1) * nodeSpacing + topPadding);
            content.sizeDelta = new Vector2(content.sizeDelta.x, height);
            Canvas.ForceUpdateCanvases();
            float width = content.rect.width;

            BuildBackground(width, height);
            BuildPath(levels.Count, progress);

            nodeTemplate.gameObject.SetActive(false);
            for (int i = 0; i < levels.Count; i++)
            {
                var node = Instantiate(nodeTemplate, content);
                node.gameObject.SetActive(true);
                node.name = $"Level {i + 1}";
                ((RectTransform)node.transform).anchoredPosition = NodePosition(i);
                node.Show(i + 1, progress.GetStatus(_ids, i), progress.GetStars(_ids[i]), i == _current);
                foreach (var motion in node.GetComponentsInChildren<UiMotion>(true))
                    motion.AddPhase(i * 0.9f); // each level's stars sparkle at a different moment

                int index = i; // captured by the click handler
                node.Button.onClick.AddListener(() =>
                {
                    FeedbackPlayer.Play(SoundId.Click);
                    _onLevelChosen?.Invoke(index);
                });
                _spawned.Add(node.gameObject);
            }

            marker.SetParent(content, false);
            marker.SetAsLastSibling();
            marker.anchoredPosition = NodePosition(_current) + new Vector2(0f, markerHeight);
            var bob = marker.GetComponent<UiMotion>();
            if (bob != null) bob.ResetBase();

            starsText.text = $"{progress.TotalStars} / {levels.Count * LevelProgress.MaxStars}";
            ScrollTo(NodePosition(_current).y, height);
        }

        public void Hide() => gameObject.SetActive(false);

        private Vector2 NodePosition(int index) =>
            new Vector2(Mathf.Sin(index * 1.1f) * zigzagWidth, bottomPadding + index * nodeSpacing);

        // ---------- Building ----------

        private void BuildBackground(float width, float height)
        {
            // Meadow tiles stacked from the bottom (the art tiles vertically), sky cap on top.
            float tileHeight = width * meadowSprite.rect.height / meadowSprite.rect.width;
            float capHeight = width * skyCapSprite.rect.height / skyCapSprite.rect.width;
            int tiles = Mathf.CeilToInt((height - capHeight * 0.9f) / tileHeight) + 1;
            for (int i = 0; i < tiles; i++)
            {
                var image = SpawnImage("Meadow", meadowSprite, Color.white);
                var rect = image.rectTransform;
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.sizeDelta = new Vector2(0f, tileHeight + 2f); // +2 hides hairline gaps between tiles
                rect.anchoredPosition = new Vector2(0f, i * tileHeight);
            }

            var cap = SpawnImage("Sky", skyCapSprite, Color.white);
            var capRect = cap.rectTransform;
            capRect.anchorMin = new Vector2(0f, 1f);
            capRect.anchorMax = new Vector2(1f, 1f);
            capRect.pivot = new Vector2(0.5f, 1f);
            capRect.sizeDelta = new Vector2(0f, capHeight);
            capRect.anchoredPosition = Vector2.zero;
        }

        /// <summary>The path is many overlapping circles: a darker edge layer, then the lighter stones on top.</summary>
        private void BuildPath(int levelCount, LevelProgress progress)
        {
            _points.Clear();
            _points.Add(new Vector2(NodePosition(0).x, 0f)); // starts at the bottom edge
            for (int i = 0; i < levelCount; i++) _points.Add(NodePosition(i));
            if (levelCount > 0)
                _points.Add(NodePosition(levelCount - 1) + new Vector2(0f, nodeSpacing * 0.3f)); // short stub: "more to come"

            StampPath(pathWidth + pathEdge * 2f, pathEdgeColor);
            StampPath(pathWidth, pathColor);
        }

        private void StampPath(float diameter, Color color)
        {
            for (int s = 0; s < _points.Count - 1; s++)
            {
                Vector2 a = _points[s], b = _points[s + 1];
                int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) / pathStep));
                for (int k = 0; k < steps; k++)
                {
                    // Gentle S-curve between nodes instead of straight lines.
                    float t = k / (float)steps;
                    float eased = t * t * (3f - 2f * t);
                    var p = new Vector2(Mathf.Lerp(a.x, b.x, eased), Mathf.Lerp(a.y, b.y, t));
                    var image = SpawnImage("Path", pathSprite, color);
                    var rect = image.rectTransform;
                    rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
                    rect.sizeDelta = new Vector2(diameter, diameter);
                    rect.anchoredPosition = p;
                }
            }
        }

        private Image SpawnImage(string objectName, Sprite sprite, Color color)
        {
            var go = new GameObject(objectName, typeof(RectTransform));
            go.layer = gameObject.layer;
            go.transform.SetParent(content, false);
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            _spawned.Add(go);
            return image;
        }

        /// <summary>Scrolls so the given content height (from the bottom) sits in the middle of the view.</summary>
        private void ScrollTo(float y, float contentHeight)
        {
            Canvas.ForceUpdateCanvases();
            float viewHeight = scrollRect.viewport.rect.height;
            float scrollable = contentHeight - viewHeight;
            scrollRect.verticalNormalizedPosition = scrollable <= 0f ? 0f : Mathf.Clamp01((y - viewHeight * 0.5f) / scrollable);
            scrollRect.velocity = Vector2.zero;
        }

        private void Clear()
        {
            foreach (var go in _spawned)
                Destroy(go);
            _spawned.Clear();
        }
    }
}
