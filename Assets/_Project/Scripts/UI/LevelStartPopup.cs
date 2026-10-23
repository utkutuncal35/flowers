using System;
using System.Collections.Generic;
using Flowers.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace Flowers.UI
{
    /// <summary>"Bölüm 3" popup shown before a level: its goals, the move count and a big Start button.</summary>
    public class LevelStartPopup : MonoBehaviour
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text movesText;
        [SerializeField] private RectTransform goalsContainer;
        [Tooltip("Inactive template; one copy per goal.")]
        [SerializeField] private GoalItemView goalTemplate;
        [SerializeField] private Button playButton;
        [SerializeField] private Button closeButton;

        private readonly List<GoalItemView> _items = new List<GoalItemView>();
        private Action _onPlay;
        private Action _onClose;

        public bool HasReferences =>
            titleText != null && movesText != null && goalsContainer != null && goalTemplate != null &&
            goalTemplate.HasReferences && playButton != null && closeButton != null;

        private void Awake()
        {
            playButton.onClick.AddListener(() => _onPlay?.Invoke());
            closeButton.onClick.AddListener(() => _onClose?.Invoke());
        }

        public void Show(int levelNumber, LevelDefinition level, GoalIcons icons,
            Action onPlay, Action onClose)
        {
            _onPlay = onPlay;
            _onClose = onClose;
            titleText.text = $"Bölüm {levelNumber}";
            movesText.text = $"{level.Moves} hamle";

            foreach (var item in _items) Destroy(item.gameObject);
            _items.Clear();
            goalTemplate.gameObject.SetActive(false);
            foreach (var goal in level.Goals)
            {
                var item = Instantiate(goalTemplate, goalsContainer);
                item.gameObject.SetActive(true);
                var sprite = icons.For(goal);
                item.Show(sprite, goal.Amount);
                _items.Add(item);
            }

            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
