using UnityEngine;
using UnityEngine.UI;

namespace Flowers.UI
{
    /// <summary>Opening screen: garden background, the game's name and one big Play button.</summary>
    public class TitleScreen : MonoBehaviour
    {
        [SerializeField] private Button playButton;

        public Button PlayButton => playButton;
        public bool HasReferences => playButton != null;

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}
