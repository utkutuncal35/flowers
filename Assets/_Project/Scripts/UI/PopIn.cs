using System.Collections;
using Flowers.View;
using UnityEngine;

namespace Flowers.UI
{
    /// <summary>Makes a popup card spring in (scale 0.7 → 1 with a small overshoot) each time it is enabled.</summary>
    public class PopIn : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float duration = 0.3f;
        [SerializeField, Range(0f, 1f)] private float startScale = 0.7f;

        private void OnEnable()
        {
            StopAllCoroutines();
            StartCoroutine(Play());
        }

        private IEnumerator Play()
        {
            float time = 0f;
            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float t = Ease.Evaluate(EaseType.OutBack, Mathf.Clamp01(time / duration));
                float s = Mathf.LerpUnclamped(startScale, 1f, t);
                transform.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            transform.localScale = Vector3.one;
        }
    }
}
