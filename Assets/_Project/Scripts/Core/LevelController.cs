using System.Collections;
using System.Collections.Generic;
using Flowers.Board;
using Flowers.Levels;
using Flowers.UI;
using Flowers.View;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Flowers.Core
{
    /// <summary>
    /// Game flow: map → level → win/lose → map or next level.
    /// Feeds every swap to the <see cref="LevelSession"/>, saves stars on a win, and keeps losing free:
    /// Retry starts again instantly, no lives.
    /// </summary>
    public class LevelController : MonoBehaviour
    {
        [SerializeField] private LevelList levelList;
        [SerializeField] private BoardView boardView;
        [SerializeField] private LevelHud hud;
        [SerializeField] private LevelEndScreen winScreen;
        [SerializeField] private LevelEndScreen loseScreen;
        [SerializeField] private LevelMapScreen mapScreen;
        [SerializeField] private BoardJuice boardJuice;
        [Tooltip("Icon shown in the HUD for score goals.")]
        [SerializeField] private Sprite scoreIcon;
        [Tooltip("Testing: -1 opens the map. 0, 1, 2... jumps straight into that level (even if it is locked).")]
        [SerializeField, Min(-1)] private int debugStartLevel = -1;
        [Tooltip("Short pause after the last animation before the lose screen opens.")]
        [SerializeField, Min(0f)] private float endScreenDelay = 0.35f;
        [Tooltip("Pause before the win screen, so the celebration (petal rain + jingle) is seen first.")]
        [SerializeField, Min(0f)] private float winScreenDelay = 1.1f;

        private readonly List<string> _levelIds = new List<string>();
        private LevelProgress _progress;
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

            for (int i = 0; i < levelList.Count; i++)
                _levelIds.Add(levelList.Get(i) != null ? levelList.Get(i).name : $"<missing {i}>");
            _progress = new LevelProgress(ProgressStore.Load());

            winScreen.RetryButton.onClick.AddListener(RetryLevel);
            winScreen.NextButton.onClick.AddListener(NextLevel);
            winScreen.MapButton.onClick.AddListener(ShowMap);
            loseScreen.RetryButton.onClick.AddListener(RetryLevel);
            loseScreen.NextButton.onClick.AddListener(NextLevel);
            loseScreen.MapButton.onClick.AddListener(ShowMap);
            hud.MapButton.onClick.AddListener(ShowMap);
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
            if (!_ready) return;
            if (debugStartLevel >= 0)
                StartLevel(Mathf.Min(debugStartLevel, levelList.Count - 1));
            else
                ShowMap();
        }

        private void Update()
        {
            // Android's back button arrives as the Escape key: from a level, go back to the map.
            var keyboard = Keyboard.current;
            if (_ready && keyboard != null && keyboard.escapeKey.wasPressedThisFrame && !mapScreen.gameObject.activeSelf)
                ShowMap();
        }

        public void ShowMap()
        {
            StopAllCoroutines();
            winScreen.Hide();
            loseScreen.Hide();
            hud.gameObject.SetActive(false);
            boardView.InputEnabled = false;
            _session = null;
            mapScreen.Show(levelList, _progress, StartLevel);
        }

        public void RetryLevel() => StartLevel(_levelIndex);

        public void NextLevel()
        {
            if (HasUnlockedNext()) StartLevel(_levelIndex + 1);
            else ShowMap();
        }

        private void StartLevel(int index)
        {
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

            StopAllCoroutines();
            winScreen.Hide();
            loseScreen.Hide();
            mapScreen.Hide();
            hud.gameObject.SetActive(true);

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
            if (_session.State == LevelState.Playing) return;

            boardView.InputEnabled = false;
            if (_session.State == LevelState.Won)
            {
                _progress.RecordWin(_levelIds[_levelIndex], _session.Stars);
                ProgressStore.Save(_progress.ToData());
                boardJuice.Celebrate();
            }
            StartCoroutine(ShowEndScreen());
        }

        private IEnumerator ShowEndScreen()
        {
            bool won = _session.State == LevelState.Won;
            yield return new WaitForSeconds(won ? winScreenDelay : endScreenDelay);

            // On a loss "Next" only shows if the next level is already open (e.g. replaying an old level):
            // losing never unlocks anything, but it never takes anything away either.
            if (won)
            {
                winScreen.Show("Tebrikler!", $"Bölüm {_levelIndex + 1} tamam.", _session.Stars, HasUnlockedNext());
            }
            else
            {
                FeedbackPlayer.Play(SoundId.Lose);
                loseScreen.Show("Hamleler bitti", "Hiç sorun değil,\nhemen tekrar deneyebilirsin.", 0, HasUnlockedNext());
            }
        }

        private bool HasUnlockedNext() =>
            _levelIndex + 1 < levelList.Count && _progress.IsUnlocked(_levelIds, _levelIndex + 1);

        private string FindMissingReference()
        {
            if (levelList == null) return "Level List is not assigned";
            if (levelList.Count == 0) return "Level List has no levels";
            if (boardView == null) return "Board View is not assigned";
            if (hud == null || !hud.HasReferences) return "HUD (or one of its parts) is not assigned";
            if (winScreen == null || !winScreen.HasReferences) return "Win Screen (or one of its parts) is not assigned";
            if (loseScreen == null || !loseScreen.HasReferences) return "Lose Screen (or one of its parts) is not assigned";
            if (mapScreen == null || !mapScreen.HasReferences) return "Map Screen (or one of its parts) is not assigned";
            if (boardJuice == null) return "Board Juice is not assigned";
            if (scoreIcon == null) return "Score Icon is not assigned";
            return null;
        }
    }
}
