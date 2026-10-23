using UnityEngine;

namespace Flowers.View
{
    public enum EaseType
    {
        Linear,
        OutQuad,
        InOutQuad,
        /// <summary>Overshoots a little and settles: the soft "boing".</summary>
        OutBack,
        /// <summary>Like OutBack with a much smaller overshoot, for long falls.</summary>
        OutBackSoft,
        /// <summary>Pulls back a little before going: used for popping (shrink).</summary>
        InBack,
    }

    public static class Ease
    {
        private const float BackOvershoot = 1.70158f;
        private const float SoftOvershoot = 0.6f;

        /// <summary>t in 0..1 → eased value (can go slightly outside 0..1 for the Back curves).</summary>
        public static float Evaluate(EaseType type, float t)
        {
            switch (type)
            {
                case EaseType.OutQuad:
                    return 1f - (1f - t) * (1f - t);
                case EaseType.InOutQuad:
                    return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;
                case EaseType.OutBack:
                {
                    float c3 = BackOvershoot + 1f;
                    float u = t - 1f;
                    return 1f + c3 * u * u * u + BackOvershoot * u * u;
                }
                case EaseType.OutBackSoft:
                {
                    float u = t - 1f;
                    return 1f + (SoftOvershoot + 1f) * u * u * u + SoftOvershoot * u * u;
                }
                case EaseType.InBack:
                {
                    float c3 = BackOvershoot + 1f;
                    return c3 * t * t * t - BackOvershoot * t * t;
                }
                default:
                    return t;
            }
        }
    }
}
