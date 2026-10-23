using System.Collections.Generic;
using Flowers.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace Flowers.UI
{
    /// <summary>Top bar: level number, moves left, and one <see cref="GoalItemView"/> per goal.</summary>
    public class LevelHud : MonoBehaviour
    {
        [SerializeField] private Text levelText;
        [SerializeField] private Text movesText;
        [SerializeField] private RectTransform goalsContainer;
        [Tooltip("Inactive template; one copy is made per goal.")]
        [SerializeField] private GoalItemView goalTemplate;
        [SerializeField] private Button mapButton;

        private readonly List<GoalItemView> _items = new List<GoalItemView>();
        private GoalIcons _icons;

        public bool HasReferences =>
            levelText != null && movesText != null && goalsContainer != null && goalTemplate != null &&
            goalTemplate.HasReferences && mapButton != null;

        public Button MapButton => mapButton;

        public void Setup(int levelNumber, LevelSession session, GoalIcons icons)
        {
            _icons = icons;
            levelText.text = $"Bölüm {levelNumber}";

            foreach (var item in _items)
                Destroy(item.gameObject);
            _items.Clear();

            goalTemplate.gameObject.SetActive(false);
            for (int i = 0; i < session.Goals.Count; i++)
            {
                var item = Instantiate(goalTemplate, goalsContainer);
                item.gameObject.SetActive(true);
                _items.Add(item);
            }

            Refresh(session);
        }

        public void Refresh(LevelSession session)
        {
            movesText.text = session.MovesLeft.ToString();
            for (int i = 0; i < _items.Count; i++)
            {
                var progress = session.Goals[i];
                var sprite = _icons.For(progress.Goal);
                _items[i].Show(sprite, progress.Remaining);
            }
        }
    }
}
