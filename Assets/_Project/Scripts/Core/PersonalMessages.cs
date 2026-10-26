using UnityEngine;

namespace Flowers.Core
{
    /// <summary>
    /// The personal texts in the game, kept out of code so they can be changed in the Inspector
    /// (Assets/_Project/Data/PersonalMessages). Setup Scene creates it once and never overwrites it.
    /// </summary>
    [CreateAssetMenu(menuName = "Flowers/Personal Messages", fileName = "PersonalMessages")]
    public class PersonalMessages : ScriptableObject
    {
        [Header("Final surprise (after the last level)")]
        [SerializeField] private string finalTitle = "Tebrikler!";
        [SerializeField, TextArea(2, 5)] private string finalMessage = "Tüm çiçekleri topladın.\nArtık gerçek çiçeği görebilirsin.";
        [SerializeField] private string revealButton = "Gerçek çiçeği gör";
        [Tooltip("Shown above the heart once the camera is on.")]
        [SerializeField, TextArea(2, 5)] private string cameraCaption = "İşte bahçenin\nen güzel çiçeği";
        [Tooltip("Shown if the camera can't be opened (no permission, no camera).")]
        [SerializeField, TextArea(2, 5)] private string noCameraMessage = "Kamera açılmadı ama olsun:\ngerçek çiçek sensin.";

        public string FinalTitle => finalTitle;
        public string FinalMessage => finalMessage;
        public string RevealButton => revealButton;
        public string CameraCaption => cameraCaption;
        public string NoCameraMessage => noCameraMessage;
    }
}
