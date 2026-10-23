using System;
using System.Collections.Generic;
using Flowers.Core;
using Flowers.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace Flowers.UI
{
    /// <summary>
    /// Vertical level map: level 1 at the bottom, the path winds upwards in a gentle zigzag.
    /// Nodes are built from the LevelList every time the map opens, so new levels show up automatically.
    /// </summary>
    public class LevelMapScreen : MonoBehaviour
    {
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform content;
        [Tooltip("Inactive template; one copy per level.")]
        [SerializeField] private LevelNodeView nodeTemplate;
        [SerializeField] private Text starsText;
        [Tooltip("Small circle used for the dotted path between levels.")]
        [SerializeField] private Sprite dotSprite;

        [Header("Layout (canvas units)")]
        [SerializeField, Min(50f)] private float nodeSpacing = 280f;
        [SerializeField, Min(0f)] private float zigzagWidth = 230f;
        [SerializeField, Min(0f)] private float edgePadding = 260f;
        [SerializeField, Range(0, 8)] private int dotsBetweenNodes = 4;
        [SerializeField] private Color dotOpenColor = new Color32(0xF0, 0x8F, 0xB0, 0xFF);
        [SerializeField] private Color dotLockedColor = new Color32(0xE6, 0xD3, 0xD0, 0xFF);

        private readonly List<GameObject> _spawned = new List<GameObject>();
        private readonly List<string> _ids = new List<string>();

        public bool HasReferences =>
            scrollRect != null && content != null && nodeTemplate != null && nodeTemplate.HasReferences &&
            starsText != null && dotSprite != null;

        public void Show(LevelList levels, LevelProgress progress, Action<int> onLevelChosen)
        {
            gameObject.SetActive(true);
            Clear();

            _ids.Clear();
            for (int i = 0; i < levels.Count; i++)
                _ids.Add(levels.Get(i) != null ? levels.Get(i).name : $"<missing {i}>");

            int current = progress.CurrentIndex(_ids);
            float height = edgePadding * 2f + Mathf.Max(0, levels.Count - 1) * nodeSpacing;
            content.sizeDelta = new Vector2(content.sizeDelta.x, height);

            // Dots first so the nodes draw on top of them.
            for (int i = 0; i < levels.Count - 1; i++)
            {
                var color = progress.IsUnlocked(_ids, i + 1) ? dotOpenColor : dotLockedColor;
                for (int d = 1; d <= dotsBetweenNodes; d++)
                {
                    float t = d / (dotsBetweenNodes + 1f);
                    var position = Vector2.Lerp(NodePosition(i), NodePosition(i + 1), t);
                    SpawnDot(position, color);
                }
            }

            nodeTemplate.gameObject.SetActive(false);
            for (int i = 0; i < levels.Count; i++)
            {
                var node = Instantiate(nodeTemplate, content);
                node.gameObject.SetActive(true);
                node.name = $"Level {i + 1}";
                ((RectTransform)node.transform).anchoredPosition = NodePosition(i);

                var status = progress.GetStatus(_ids, i);
                node.Show(i + 1, status, progress.GetStars(_ids[i]), i == current);

                int index = i; // captured by the click handler
                node.Button.onClick.AddListener(() =>
                {
                    FeedbackPlayer.Play(SoundId.Click);
                    onLevelChosen(index);
                });
                _spawned.Add(node.gameObject);
            }

            starsText.text = $"{progress.TotalStars} / {levels.Count * LevelProgress.MaxStars}";
            ScrollTo(NodePosition(current).y, height);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private Vector2 NodePosition(int index) =>
            new Vector2(Mathf.Sin(index * 1.1f) * zigzagWidth, edgePadding + index * nodeSpacing);

        private void SpawnDot(Vector2 position, Color color)
        {
            var go = new GameObject("Dot", typeof(RectTransform));
            go.layer = gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(content, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(26f, 26f);
            rect.anchoredPosition = position;
            var image = go.AddComponent<Image>();
            image.sprite = dotSprite;
            image.color = color;
            image.raycastTarget = false;
            _spawned.Add(go);
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
