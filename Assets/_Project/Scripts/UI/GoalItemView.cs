using UnityEngine;
using UnityEngine.UI;

namespace Flowers.UI
{
    /// <summary>One goal in the HUD: an icon and how many are still needed (or a check mark when done).</summary>
    public class GoalItemView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Text countText;
        [SerializeField] private Image checkMark;

        public void Show(Sprite iconSprite, int remaining)
        {
            icon.sprite = iconSprite;
            bool done = remaining <= 0;
            countText.text = remaining.ToString();
            countText.gameObject.SetActive(!done);
            checkMark.gameObject.SetActive(done);
        }

        public bool HasReferences => icon != null && countText != null && checkMark != null;
    }
}
