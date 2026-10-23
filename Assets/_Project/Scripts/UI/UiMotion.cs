using UnityEngine;

namespace Flowers.UI
{
    /// <summary>
    /// Small looping idle animation for decorations: spinning light rays, a breathing Play button,
    /// the bobbing map marker. Uses unscaled time and touches only this transform.
    /// </summary>
    public class UiMotion : MonoBehaviour
    {
        public enum Kind
        {
            Rotate,
            Pulse,
            Bob,
            /// <summary>Star sparkle: a gentle pulse plus a small rocking rotation.</summary>
            Twinkle,
        }

        [SerializeField] private Kind kind = Kind.Pulse;
        [Tooltip("Rotate: degrees per second. Pulse/Twinkle: scale change (0.05 = ±5%). Bob: distance in canvas units.")]
        [SerializeField] private float amount = 0.05f;
        [SerializeField, Min(0.01f)] private float speed = 2.5f;
        [Tooltip("Start offset in radians, so neighbours (e.g. three stars) don't move in lockstep.")]
        [SerializeField] private float phase;

        private Vector3 _baseScale;
        private Vector2 _basePosition;
        private float _baseAngle;
        private RectTransform _rect;

        private void Awake()
        {
            _rect = transform as RectTransform;
            _baseScale = transform.localScale;
            _baseAngle = transform.localEulerAngles.z;
            if (_rect != null) _basePosition = _rect.anchoredPosition;
        }

        /// <summary>Shifts the animation in time; used so copies made from one template don't move in sync.</summary>
        public void AddPhase(float radians) => phase += radians;

        /// <summary>Call after moving the object from code so bobbing happens around the new spot.</summary>
        public void ResetBase()
        {
            if (_rect != null) _basePosition = _rect.anchoredPosition;
        }

        private void Update()
        {
            float t = Time.unscaledTime * speed + phase;
            switch (kind)
            {
                case Kind.Rotate:
                    transform.Rotate(0f, 0f, -amount * Time.unscaledDeltaTime);
                    break;
                case Kind.Pulse:
                    transform.localScale = _baseScale * (1f + Mathf.Sin(t) * amount);
                    break;
                case Kind.Bob:
                    if (_rect != null)
                        _rect.anchoredPosition = _basePosition + new Vector2(0f, Mathf.Abs(Mathf.Sin(t)) * amount);
                    break;
                case Kind.Twinkle:
                    // Sharper peak than a plain sine: mostly calm, then a quick "sparkle".
                    float sparkle = Mathf.Pow(Mathf.Sin(t) * 0.5f + 0.5f, 3f);
                    transform.localScale = _baseScale * (1f + sparkle * amount);
                    transform.localRotation = Quaternion.Euler(0f, 0f, _baseAngle + Mathf.Sin(t * 0.6f) * 6f);
                    break;
            }
        }

#if UNITY_EDITOR
        public void EditorSetup(Kind motion, float motionAmount, float motionSpeed, float startPhase = 0f)
        {
            kind = motion;
            amount = motionAmount;
            speed = motionSpeed;
            phase = startPhase;
        }
#endif
    }
}
