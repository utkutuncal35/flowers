using UnityEngine;

namespace Flowers.View
{
    /// <summary>One flower on screen. Only shows a sprite; the rules live in Flowers.Board.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class FlowerView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        public int Type { get; private set; } = -1;

        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Show(int type, Sprite sprite, float scale)
        {
            Type = type;
            spriteRenderer.sprite = sprite;
            transform.localScale = new Vector3(scale, scale, 1f);
            transform.localRotation = Quaternion.identity;
            gameObject.name = $"Flower {type}";
        }

        /// <summary>Draws this flower above its neighbours (e.g. the one the player is dragging).</summary>
        public void SetOnTop(bool onTop) => spriteRenderer.sortingOrder = onTop ? 1 : 0;
    }
}
