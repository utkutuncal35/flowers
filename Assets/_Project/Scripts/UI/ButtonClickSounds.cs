using Flowers.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Flowers.UI
{
    /// <summary>
    /// Put on the Canvas: every Button under it plays the click sound. Buttons created at runtime
    /// (map nodes) are not covered; they play the click themselves.
    /// </summary>
    public class ButtonClickSounds : MonoBehaviour
    {
        private static readonly UnityEngine.Events.UnityAction PlayClick = () => FeedbackPlayer.Play(SoundId.Click);

        private void Awake()
        {
            foreach (var button in GetComponentsInChildren<Button>(true))
                button.onClick.AddListener(PlayClick);
        }
    }
}
