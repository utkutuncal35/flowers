using Flowers.Board;
using Flowers.Core;
using Flowers.UI;
using UnityEngine;

namespace Flowers.View
{
    /// <summary>
    /// Listens to <see cref="BoardView"/> and adds the feel: sounds, petals, praise, vibration.
    /// Purely cosmetic: removing this component leaves the game fully playable.
    /// </summary>
    public class BoardJuice : MonoBehaviour
    {
        [SerializeField] private BoardView boardView;
        [SerializeField] private JuiceConfig config;
        [Tooltip("Burst of petals where flowers pop.")]
        [SerializeField] private ParticleSystem petals;
        [Tooltip("Petals raining from the top when a level is won.")]
        [SerializeField] private ParticleSystem confetti;
        [SerializeField] private PraiseText praiseText;
        [SerializeField] private Camera targetCamera;

        private bool _ready;

        private void Awake()
        {
            if (boardView == null || config == null || petals == null || confetti == null || praiseText == null || targetCamera == null)
            {
                Debug.LogError($"[Flowers] BoardJuice on '{name}': a reference is not assigned. Run Flowers → Setup Scene.", this);
                enabled = false;
                return;
            }
            _ready = true;
        }

        private void OnEnable()
        {
            if (boardView == null) return;
            boardView.SwapStarted += OnSwapStarted;
            boardView.InvalidSwapReturning += OnInvalidSwap;
            boardView.StepPopping += OnStepPopping;
        }

        private void OnDisable()
        {
            if (boardView == null) return;
            boardView.SwapStarted -= OnSwapStarted;
            boardView.InvalidSwapReturning -= OnInvalidSwap;
            boardView.StepPopping -= OnStepPopping;
        }

        /// <summary>Win: jingle, a stronger buzz and petals raining over the whole screen.</summary>
        public void Celebrate()
        {
            if (!_ready) return;
            FeedbackPlayer.Play(SoundId.Win);
            FeedbackPlayer.Vibrate(HapticStrength.Medium);

            float halfHeight = targetCamera.orthographicSize;
            float halfWidth = halfHeight * targetCamera.aspect;
            Vector3 center = targetCamera.transform.position;
            int colorCount = Mathf.Max(1, config.PetalColorCount);

            var emit = new ParticleSystem.EmitParams();
            for (int i = 0; i < config.ConfettiCount; i++)
            {
                emit.position = new Vector3(
                    center.x + Random.Range(-halfWidth, halfWidth),
                    center.y + halfHeight + Random.Range(0.2f, 3f), 0f);
                emit.velocity = new Vector3(Random.Range(-0.8f, 0.8f), Random.Range(-2.5f, -0.5f), 0f);
                emit.startColor = config.PetalColor(Random.Range(0, colorCount));
                emit.startSize = Random.Range(config.ConfettiSize.x, config.ConfettiSize.y);
                emit.startLifetime = Random.Range(config.ConfettiLifetime.x, config.ConfettiLifetime.y);
                emit.rotation = Random.Range(0f, 360f);
                emit.angularVelocity = Random.Range(-200f, 200f);
                confetti.Emit(emit, 1);
            }
        }

        private void OnSwapStarted() => FeedbackPlayer.Play(SoundId.Swap);

        private void OnInvalidSwap() => FeedbackPlayer.Play(SoundId.Invalid);

        private void OnStepPopping(CascadeStep step, int stepIndex)
        {
            FeedbackPlayer.Play(SoundId.Pop, config.ComboPitch(stepIndex));
            FeedbackPlayer.Vibrate(HapticStrength.Light);

            var emit = new ParticleSystem.EmitParams();
            for (int i = 0; i < step.Cleared.Count; i++)
            {
                var cleared = step.Cleared[i];
                Vector3 position = boardView.CellToWorld(cleared.Position);
                emit.startColor = config.PetalColor(cleared.Type);
                for (int k = 0; k < config.PetalsPerFlower; k++)
                {
                    // Spread evenly around the flower with a little randomness, biased upwards so they arc.
                    float angle = (k + Random.value * 0.6f) * Mathf.PI * 2f / config.PetalsPerFlower;
                    float speed = Random.Range(config.PetalSpeed.x, config.PetalSpeed.y);
                    emit.position = position;
                    emit.velocity = new Vector3(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed + 1.5f, 0f);
                    emit.startSize = Random.Range(config.PetalSize.x, config.PetalSize.y);
                    emit.startLifetime = Random.Range(config.PetalLifetime.x, config.PetalLifetime.y);
                    emit.rotation = Random.Range(0f, 360f);
                    emit.angularVelocity = Random.Range(-360f, 360f);
                    petals.Emit(emit, 1);
                }
            }

            string praise = config.PraiseFor(stepIndex + 1);
            if (praise != null)
            {
                praiseText.Show(praise);
                FeedbackPlayer.Play(SoundId.Praise);
            }
        }
    }
}
