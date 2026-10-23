using System.Collections;
using Flowers.Board;
using Flowers.Levels;
using Flowers.UI;
using Flowers.View;
using UnityEngine;

namespace Flowers.Core
{
    /// <summary>
    /// Runs one level at a time: builds the board, feeds every swap to the <see cref="LevelSession"/>,
    /// updates the HUD and opens the win/lose screen. Losing is free: Retry starts again instantly.
    /// </summary>
    public class LevelController : MonoBehaviour
    {
        [SerializeField] private LevelList levelList;
        [SerializeField] private BoardView boardView;
        [SerializeField] private LevelHud hud;
        [SerializeField] private LevelEndScreen winScreen;
        [SerializeField] private LevelEndScreen loseScreen;
        [Tooltip("Icon shown in the HUD for score goals.")]
        [SerializeField] private Sprite scoreIcon;
        [Tooltip("Which level to open when Play is pressed (0 = first). Handy for testing a specific level.")]
        [SerializeField, Min(0)] private int startLevelIndex;
        [Tooltip("Short pause after the last animation before the win/lose screen opens.")]
        [SerializeField, Min(0f)] private float endScreenDelay = 0.35f;

        private LevelSession _session;
        private int _levelIndex;
        private bool _ready;

        private void Awake()
        {
            string problem = FindMissingReference();
            if (problem != null)
            {
                Debug.LogError($"[Flowers] LevelController on '{name}': {problem}. Run Flowers → Setup Scene.", this);
                enabled = false;
                return;
            }

            winScreen.RetryButton.onClick.AddListener(RetryLevel);
            winScreen.NextButton.onClick.AddListener(NextLevel);
            loseScreen.RetryButton.onClick.AddListener(RetryLevel);
            loseScreen.NextButton.onClick.AddListener(NextLevel);
            _ready = true;
        }

        private void OnEnable()
        {
            if (boardView != null) boardView.SwapFinished += OnSwapFinished;
        }

        private void OnDisable()
        {
            if (boardView != null) boardView.SwapFinished -= OnSwapFinished;
        }

        private void Start()
        {
            if (_ready)
                StartLevel(Mathf.Clamp(startLevelIndex, 0, levelList.Count - 1));
        }

        public void RetryLevel() => StartLevel(_levelIndex);

        /// <summary>After the last level this wraps to the first one (the level map comes later).</summary>
        public void NextLevel() => StartLevel((_levelIndex + 1) % levelList.Count);

        private void StartLevel(int index)
        {
            StopAllCoroutines();
            winScreen.Hide();
            loseScreen.Hide();

            var data = levelList.Get(index);
            if (data == null)
            {
                Debug.LogError($"[Flowers] LevelList slot {index} is empty.", levelList);
                return;
            }

            var definition = data.ToDefinition();
            string problem = definition.Validate();
            if (problem != null)
            {
                Debug.LogError($"[Flowers] Level '{data.name}' can't be played: {problem}.", data);
                return;
            }

            _levelIndex = index;
            _session = new LevelSession(definition);
            boardView.Build(definition.Width, definition.Height, definition.TypeCount, data.Seed);
            boardView.InputEnabled = true;
            hud.Setup(index + 1, _session, boardView.Config.FlowerSprites, scoreIcon);
        }

        private void OnSwapFinished(SwapResult result)
        {
            if (_session == null || !_session.ApplySwap(result)) return;

            hud.Refresh(_session);
            if (_session.State != LevelState.Playing)
            {
                boardView.InputEnabled = false;
                StartCoroutine(ShowEndScreen());
            }
        }

        private IEnumerator ShowEndScreen()
        {
            yield return new WaitForSeconds(endScreenDelay);

            if (_session.State == LevelState.Won)
                winScreen.Show("Tebrikler!", $"Bölüm {_levelIndex + 1} tamam.", _session.Stars);
            else
                loseScreen.Show("Hamleler bitti", "Hiç sorun değil,\nhemen tekrar deneyebilirsin.", 0);
        }

        private string FindMissingReference()
        {
            if (levelList == null) return "Level List is not assigned";
            if (levelList.Count == 0) return "Level List has no levels";
            if (boardView == null) return "Board View is not assigned";
            if (hud == null || !hud.HasReferences) return "HUD (or one of its texts/template) is not assigned";
            if (winScreen == null || !winScreen.HasReferences) return "Win Screen (or one of its parts) is not assigned";
            if (loseScreen == null || !loseScreen.HasReferences) return "Lose Screen (or one of its parts) is not assigned";
            if (scoreIcon == null) return "Score Icon is not assigned";
            return null;
        }
    }
}
