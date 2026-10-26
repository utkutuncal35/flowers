using System.Collections;
using Flowers.Core;
using Flowers.View;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace Flowers.UI
{
    /// <summary>
    /// Shown once the last level is won: "you collected every flower, now see the real one", and a button that
    /// opens the front camera inside a heart, so she sees herself. Texts come from <see cref="PersonalMessages"/>.
    /// The camera is stopped whenever the screen closes.
    /// </summary>
    public class FinalSurpriseScreen : MonoBehaviour
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text messageText;
        [SerializeField] private Button revealButton;
        [SerializeField] private Text revealLabel;
        [SerializeField] private Button mapButton;
        [Tooltip("The flower shown in the heart before the camera opens.")]
        [SerializeField] private RectTransform closedFlower;
        [Tooltip("Flips the picture left-right for the front camera, like a mirror.")]
        [SerializeField] private RectTransform cameraMirror;
        [SerializeField] private RawImage cameraImage;
        [SerializeField, Min(0.01f)] private float revealDuration = 0.6f;
        [Tooltip("Requested camera resolution; the phone picks the closest it has.")]
        [SerializeField] private Vector2Int cameraResolution = new Vector2Int(1280, 720);

        private PersonalMessages _messages;
        private WebCamTexture _camera;
        private bool _frontCamera;

        public Button MapButton => mapButton;

        public bool HasReferences =>
            titleText != null && messageText != null && revealButton != null && revealLabel != null &&
            mapButton != null && closedFlower != null && cameraMirror != null && cameraImage != null;

        private void Awake()
        {
            if (revealButton != null) revealButton.onClick.AddListener(Reveal);
        }

        public void Show(PersonalMessages messages)
        {
            _messages = messages;
            titleText.text = messages.FinalTitle;
            messageText.text = messages.FinalMessage;
            revealLabel.text = messages.RevealButton;
            revealButton.gameObject.SetActive(true);
            closedFlower.localScale = Vector3.one;
            closedFlower.gameObject.SetActive(true);
            cameraImage.gameObject.SetActive(false);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            StopAllCoroutines();
            StopCamera();
            gameObject.SetActive(false);
        }

        private void OnDisable() => StopCamera();

        private void Update()
        {
            if (_camera != null && _camera.isPlaying && _camera.didUpdateThisFrame)
                FitCameraImage();
        }

        private void Reveal()
        {
            revealButton.gameObject.SetActive(false);
            StartCoroutine(OpenCamera());
        }

        private IEnumerator OpenCamera()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                int answer = 0; // 0 = waiting, 1 = yes, -1 = no
                var callbacks = new PermissionCallbacks();
                callbacks.PermissionGranted += _ => answer = 1;
                callbacks.PermissionDenied += _ => answer = -1;
                Permission.RequestUserPermission(Permission.Camera, callbacks);
                while (answer == 0) yield return null;
                if (answer < 0)
                {
                    ShowNoCamera();
                    yield break;
                }
            }
#endif
            var devices = WebCamTexture.devices;
            if (devices.Length == 0)
            {
                ShowNoCamera();
                yield break;
            }

            // Front (selfie) camera if there is one.
            int index = 0;
            for (int i = 0; i < devices.Length; i++)
                if (devices[i].isFrontFacing) { index = i; break; }
            _frontCamera = devices[index].isFrontFacing;

            _camera = new WebCamTexture(devices[index].name, cameraResolution.x, cameraResolution.y, 30);
            _camera.Play();

            // The real size is only known once the first frames arrive.
            float waited = 0f;
            while (_camera.width <= 16 && waited < 4f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            if (!_camera.isPlaying || _camera.width <= 16)
            {
                StopCamera();
                ShowNoCamera();
                yield break;
            }

            cameraImage.texture = _camera;
            FitCameraImage();
            messageText.text = _messages.CameraCaption;
            FeedbackPlayer.Play(SoundId.Win);
            yield return SwapFlowerForCamera();
        }

        /// <summary>The closed flower shrinks away and the camera picture grows in its place.</summary>
        private IEnumerator SwapFlowerForCamera()
        {
            cameraImage.gameObject.SetActive(true);
            float time = 0f;
            while (time < revealDuration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / revealDuration);
                float shrink = 1f - Ease.Evaluate(EaseType.InBack, Mathf.Clamp01(t * 2f));
                closedFlower.localScale = new Vector3(shrink, shrink, 1f);
                float grow = Ease.Evaluate(EaseType.OutBack, t);
                cameraMirror.localScale = new Vector3(_frontCamera ? -grow : grow, grow, 1f);
                yield return null;
            }
            closedFlower.gameObject.SetActive(false);
            cameraMirror.localScale = new Vector3(_frontCamera ? -1f : 1f, 1f, 1f);
        }

        /// <summary>
        /// Crops the camera picture to a square (so it fills the heart whatever the phone's camera shape) and turns
        /// it upright: phone cameras deliver sideways pictures and say by how much (videoRotationAngle).
        /// </summary>
        private void FitCameraImage()
        {
            int w = _camera.width, h = _camera.height;
            if (w <= 16 || h <= 16) return;
            float side = Mathf.Min(w, h);
            float uw = side / w, vh = side / h;
            cameraImage.uvRect = new Rect((1f - uw) * 0.5f, (1f - vh) * 0.5f, uw, vh);

            var picture = cameraImage.rectTransform;
            picture.localEulerAngles = new Vector3(0f, 0f, -_camera.videoRotationAngle);
            picture.localScale = new Vector3(1f, _camera.videoVerticallyMirrored ? -1f : 1f, 1f);
        }

        private void ShowNoCamera()
        {
            messageText.text = _messages.NoCameraMessage;
        }

        private void StopCamera()
        {
            if (_camera == null) return;
            if (_camera.isPlaying) _camera.Stop();
            if (cameraImage != null) cameraImage.texture = null;
            Destroy(_camera);
            _camera = null;
        }
    }
}
