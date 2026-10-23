using System.Collections;
using Flowers.View;
using UnityEngine;
using UnityEngine.UI;

namespace Flowers.UI
{
    /// <summary>A word like "Harika!" that pops in, holds for a moment and fades out. Calling Show again restarts it.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class PraiseText : MonoBehaviour
    {
        [SerializeField] private Text label;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField, Min(0.01f)] private float popDuration = 0.25f;
        [SerializeField, Min(0f)] private float holdDuration = 0.55f;
        [SerializeField, Min(0.01f)] private float fadeDuration = 0.3f;

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        public bool HasReferences => label != null;

        public void Show(string text)
        {
            label.text = text;
            StopAllCoroutines();
            StartCoroutine(Play());
        }

        private IEnumerator Play()
        {
            var rect = (RectTransform)transform;
            canvasGroup.alpha = 1f;

            float time = 0f;
            while (time < popDuration)
            {
                time += Time.deltaTime;
                float s = Ease.Evaluate(EaseType.OutBack, Mathf.Clamp01(time / popDuration));
                rect.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            rect.localScale = Vector3.one;

            yield return new WaitForSeconds(holdDuration);

            time = 0f;
            while (time < fadeDuration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / fadeDuration);
                canvasGroup.alpha = 1f - t;
                rect.localScale = Vector3.one * (1f + 0.15f * t);
                yield return null;
            }
            canvasGroup.alpha = 0f;
        }
    }
}
