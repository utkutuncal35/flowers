using Flowers.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace Flowers.UI
{
    /// <summary>One level circle on the map: number, lock or stars, and a gentle pulse on the current level.</summary>
    public class LevelNodeView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Text numberText;
        [SerializeField] private Image lockIcon;
        [SerializeField] private Image[] stars;

        [Header("Colors")]
        [SerializeField] private Color completedColor = new Color32(0x8E, 0x2C, 0x48, 0xFF);
        [SerializeField] private Color openColor = new Color32(0xE8, 0x34, 0x4A, 0xFF);
        [SerializeField] private Color lockedColor = new Color32(0xE6, 0xD3, 0xD0, 0xFF);
        [SerializeField] private Color numberColor = new Color32(0xFF, 0xF7, 0xEE, 0xFF);
        [SerializeField] private Color starEarnedColor = new Color32(0xC8, 0x1D, 0x3F, 0xFF);
        [SerializeField] private Color starMissedColor = new Color32(0xE6, 0xD3, 0xD0, 0xFF);

        [Header("Pulse (current level)")]
        [SerializeField, Min(0f)] private float pulseAmount = 0.07f;
        [SerializeField, Min(0.01f)] private float pulseSpeed = 3f;

        private bool _pulse;

        public Button Button => button;

        public bool HasReferences => button != null && background != null && numberText != null && lockIcon != null && stars != null;

        public void Show(int levelNumber, LevelStatus status, int earnedStars, bool isCurrent)
        {
            numberText.text = levelNumber.ToString();
            numberText.color = numberColor;
            numberText.gameObject.SetActive(status != LevelStatus.Locked);
            lockIcon.gameObject.SetActive(status == LevelStatus.Locked);
            button.interactable = status != LevelStatus.Locked;

            background.color = status == LevelStatus.Completed ? completedColor
                : status == LevelStatus.Open ? openColor
                : lockedColor;

            for (int i = 0; i < stars.Length; i++)
            {
                stars[i].gameObject.SetActive(status == LevelStatus.Completed);
                stars[i].color = i < earnedStars ? starEarnedColor : starMissedColor;
            }

            _pulse = isCurrent && status != LevelStatus.Locked;
            background.rectTransform.localScale = Vector3.one;
        }

        private void Update()
        {
            if (!_pulse) return;
            float s = 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmount;
            background.rectTransform.localScale = new Vector3(s, s, 1f);
        }
    }
}
