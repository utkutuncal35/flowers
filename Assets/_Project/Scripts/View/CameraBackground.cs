using UnityEngine;

namespace Flowers.View
{
    /// <summary>
    /// Keeps a background sprite covering the whole camera view (like CSS background-size: cover),
    /// whatever the screen shape or camera zoom. Drawn behind everything else.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class CameraBackground : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private float _lastSize = -1f;
        private float _lastAspect = -1f;
        private Vector3 _lastCameraPosition;

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (targetCamera == null)
            {
                Debug.LogError($"[Flowers] CameraBackground on '{name}': Target Camera is not assigned. Run Flowers → Setup Scene.", this);
                enabled = false;
            }
        }

        private void LateUpdate()
        {
            var camPos = targetCamera.transform.position;
            if (Mathf.Approximately(_lastSize, targetCamera.orthographicSize) &&
                Mathf.Approximately(_lastAspect, targetCamera.aspect) && camPos == _lastCameraPosition)
                return;

            _lastSize = targetCamera.orthographicSize;
            _lastAspect = targetCamera.aspect;
            _lastCameraPosition = camPos;

            var sprite = spriteRenderer.sprite;
            if (sprite == null) return;
            float viewHeight = targetCamera.orthographicSize * 2f;
            float viewWidth = viewHeight * targetCamera.aspect;
            Vector2 spriteSize = sprite.bounds.size;
            float scale = Mathf.Max(viewWidth / spriteSize.x, viewHeight / spriteSize.y);
            transform.localScale = new Vector3(scale, scale, 1f);
            transform.position = new Vector3(camPos.x, camPos.y, transform.position.z);
        }
    }
}
