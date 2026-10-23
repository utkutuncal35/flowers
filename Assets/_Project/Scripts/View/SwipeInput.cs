using Flowers.Board;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Flowers.View
{
    /// <summary>
    /// Turns a press + drag on a flower into a swap with its neighbour in the drag direction.
    /// Uses <see cref="Pointer.current"/>, so the mouse works in the editor and touch works on the phone.
    /// </summary>
    public class SwipeInput : MonoBehaviour
    {
        [SerializeField] private BoardView boardView;
        [SerializeField] private Camera targetCamera;

        private bool _tracking;
        private Position _startCell;
        private Vector3 _startWorld;

        private void Awake()
        {
            if (boardView == null || targetCamera == null)
            {
                Debug.LogError($"[Flowers] SwipeInput on '{name}': Board View or Target Camera is not assigned. Run Flowers → Setup Scene.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            var pointer = Pointer.current;
            if (pointer == null) return;

            var press = pointer.press;

            if (press.wasPressedThisFrame)
            {
                _tracking = false;
                // A swipe that starts during an animation is ignored completely, not queued.
                if (boardView.CanAcceptInput)
                {
                    var world = ScreenToWorld(pointer.position.ReadValue());
                    if (boardView.TryGetCell(world, out _startCell))
                    {
                        _startWorld = world;
                        _tracking = true;
                    }
                }
            }

            if (_tracking && press.isPressed)
            {
                Vector3 delta = ScreenToWorld(pointer.position.ReadValue()) - _startWorld;
                if (delta.magnitude >= boardView.SwipeThresholdWorld)
                {
                    _tracking = false;
                    boardView.RequestSwap(_startCell, NeighbourInDirection(_startCell, delta));
                }
            }

            if (press.wasReleasedThisFrame)
                _tracking = false;
        }

        private static Position NeighbourInDirection(Position cell, Vector3 delta)
        {
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
                return new Position(cell.X + (delta.x > 0f ? 1 : -1), cell.Y);
            return new Position(cell.X, cell.Y + (delta.y > 0f ? 1 : -1));
        }

        private Vector3 ScreenToWorld(Vector2 screen)
        {
            var world = targetCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -targetCamera.transform.position.z));
            world.z = 0f;
            return world;
        }
    }
}
