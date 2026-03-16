using UnityEngine;

namespace PuzzleParty.Board
{
    /// <summary>
    /// Detects swipe gestures and reports them as (tile world position, direction) events.
    /// BoardController subscribes to OnSwipe and handles game logic.
    /// </summary>
    public class InputHandler : MonoBehaviour
    {
        [SerializeField] private float dragThreshold = 0.2f;

        public event System.Action<Vector3, MoveDirection> OnSwipe;

        private Vector3 mouseDownPosition;
        private bool isDragging = false;
        private bool inputEnabled = false;

        public void EnableInput() => inputEnabled = true;
        public void DisableInput() => inputEnabled = false;

        void Update()
        {
            if (!inputEnabled) return;

            if (Input.GetMouseButtonDown(0))
            {
                mouseDownPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mouseDownPosition.z = 0;
                isDragging = true;
            }

            if (Input.GetMouseButton(0) && isDragging)
            {
                Vector3 currentPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                currentPosition.z = 0;

                if (Vector3.Distance(mouseDownPosition, currentPosition) >= dragThreshold)
                {
                    MoveDirection direction = GetSwipeDirection(currentPosition - mouseDownPosition);
                    OnSwipe?.Invoke(mouseDownPosition, direction);
                    isDragging = false;
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;
            }
        }

        private MoveDirection GetSwipeDirection(Vector3 swipeVector)
        {
            if (Mathf.Abs(swipeVector.x) > Mathf.Abs(swipeVector.y))
                return swipeVector.x > 0 ? MoveDirection.RIGHT : MoveDirection.LEFT;
            else
                return swipeVector.y > 0 ? MoveDirection.UP : MoveDirection.DOWN;
        }
    }
}
