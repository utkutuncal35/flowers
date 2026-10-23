using System.Collections;
using Flowers.View;
using UnityEngine;
using UnityEngine.UI;

namespace Flowers.UI
{
    /// <summary>
    /// The win or lose popup. Same component for both; the lose screen just has no stars.
    /// Lives on the panel object, which stays inactive until <see cref="Show"/>.
    /// </summary>
    public class LevelEndScreen : MonoBehaviour
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text messageText;
        [Tooltip("Leave empty on the lose screen.")]
        [SerializeField] private Image[] stars;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button mapButton;
        [SerializeField] private Color starEarnedColor = new Color32(0xC8, 0x1D, 0x3F, 0xFF);
        [SerializeField] private Color starMissedColor = new Color32(0xE6, 0xD3, 0xD0, 0xFF);
        [SerializeField, Min(0.01f)] private float starPopDuration = 0.3f;
        [SerializeField, Min(0f)] private float starDelay = 0.2f;

        public Button RetryButton => retryButton;
        public Button NextButton => nextButton;
        public Button MapButton => mapButton;

        public bool HasReferences =>
            titleText != null && messageText != null && retryButton != null && nextButton != null && mapButton != null;

        public void Show(string title, string message, int earnedStars, bool canGoNext)
        {
            titleText.text = title;
            messageText.text = message;
            nextButton.gameObject.SetActive(canGoNext);
            gameObject.SetActive(true);
            if (stars != null && stars.Length > 0)
                StartCoroutine(PlayStars(earnedStars));
        }

        public void Hide()
        {
            StopAllCoroutines();
            gameObject.SetActive(false);
        }

        /// <summary>Stars appear one by one; earned ones pop in colored, missed ones stay pale.</summary>
        private IEnumerator PlayStars(int earned)
        {
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i].color = i < earned ? starEarnedColor : starMissedColor;
                stars[i].rectTransform.localScale = Vector3.zero;
            }

            for (int i = 0; i < stars.Length; i++)
            {
                yield return new WaitForSeconds(starDelay);
                var star = stars[i].rectTransform;
                float time = 0f;
                while (time < starPopDuration)
                {
                    time += Time.deltaTime;
                    float s = Ease.Evaluate(EaseType.OutBack, Mathf.Clamp01(time / starPopDuration));
                    star.localScale = new Vector3(s, s, 1f);
                    yield return null;
                }
                star.localScale = Vector3.one;
            }
        }
    }
}
