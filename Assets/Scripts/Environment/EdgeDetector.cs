using UnityEngine;
namespace EnvironmentSystem
{
    /// <summary>
    /// Casts a ray in a specified local-space direction to detect a cliff.
    /// If no collider is hit within distance, it's considered a cliff.
    /// Sends an event to notify listeners (e.g., parent controller).
    /// </summary>
    public class EdgeDetector : MonoBehaviour
    {
        private enum RayDirection
        {
            Down,
            Up,
            Left,
            Right,
            Custom
        }

        [Header("Ray Settings")]
        [SerializeField] private RayDirection rayDirection = RayDirection.Down;
        [SerializeField] private Vector2 localDirection = Vector2.down;
        [SerializeField] private float rayDistance = 1.0f;
        [SerializeField] private LayerMask groundLayer;

        [Header("Wall Check")]
        [SerializeField] private bool enableWallCheck = true;
        [SerializeField] private float wallDistance = 0.3f;
        [SerializeField] private bool verboseDebug = false;

        private bool wasEdge;

        public bool IsEdge { get; private set; }
        public bool IsWall { get; private set; }

        private void Update()
        {
            UpdateEdgeState();
            UpdateWallState();
        }

        private void UpdateEdgeState()
        {
            Vector2 origin = transform.position;
            Vector2 dir = GetLocalDirection();
            dir = transform.TransformDirection(dir.normalized);
            RaycastHit2D hit = Physics2D.Raycast(origin, dir, rayDistance, groundLayer);

            IsEdge = hit.collider == null;

            if (IsEdge != wasEdge)
            {
                if (IsEdge)
                {
                    Debug.Log($"[EdgeDetector] Edge detected by {name} at {origin} dir={dir} dist={rayDistance}");
                    // Edge takes priority over wall in the same frame.
                    FlipParentDirection();
                }
                else
                {
                    Debug.Log($"[EdgeDetector] Ground detected by {name} hit={hit.collider.name}");
                }
                wasEdge = IsEdge;
            }
            if (verboseDebug)
            {
                string hitName = hit.collider != null ? hit.collider.name : "None";
                Debug.Log($"[EdgeDetector] EdgeCheck {name} hit={hitName} isEdge={IsEdge} dir={dir} dist={rayDistance} layerMask={groundLayer.value}");
            }
        }

        private void UpdateWallState()
        {
            if (!enableWallCheck)
            {
                IsWall = false;
                return;
            }

            // If edge is detected, skip wall handling this frame (edge priority).
            if (IsEdge)
            {
                IsWall = false;
                return;
            }

            Vector2 origin = transform.position;
            Vector2 leftDir = transform.TransformDirection(Vector2.left);
            Vector2 rightDir = transform.TransformDirection(Vector2.right);

            RaycastHit2D hitLeft = Physics2D.Raycast(origin, leftDir, wallDistance, groundLayer);
            RaycastHit2D hitRight = Physics2D.Raycast(origin, rightDir, wallDistance, groundLayer);

            bool nowWall = hitLeft.collider != null || hitRight.collider != null;

            if (nowWall != IsWall)
            {
                if (nowWall)
                {
                    string hitName = hitLeft.collider != null ? hitLeft.collider.name : hitRight.collider.name;
                    Debug.Log($"[EdgeDetector] Wall detected by {name} hit={hitName}");
                    FlipParentDirection();
                }
                IsWall = nowWall;
            }
            if (verboseDebug)
            {
                string leftName = hitLeft.collider != null ? hitLeft.collider.name : "None";
                string rightName = hitRight.collider != null ? hitRight.collider.name : "None";
                Debug.Log($"[EdgeDetector] WallCheck {name} left={leftName} right={rightName} isWall={IsWall} dist={wallDistance} layerMask={groundLayer.value}");
            }
        }

        private void FlipParentDirection()
        {
            var frogController = GetComponentInParent<NPCSystem.Frog.FrogController>();
            if (frogController != null)
            {
                frogController.FlipIdleDirection();
            }
            else if (verboseDebug)
            {
                Debug.LogWarning($"[EdgeDetector] No FrogController found in parent hierarchy for {name}");
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector2 origin = transform.position;
            Vector2 dir = GetLocalDirection();
            dir = transform.TransformDirection(dir.normalized);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(origin, origin + dir * rayDistance);
            Gizmos.DrawWireSphere(origin + dir * rayDistance, 0.05f);

            if (enableWallCheck)
            {
                Vector2 leftDir = transform.TransformDirection(Vector2.left);
                Vector2 rightDir = transform.TransformDirection(Vector2.right);
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(origin, origin + leftDir * wallDistance);
                Gizmos.DrawLine(origin, origin + rightDir * wallDistance);
            }
        }

        private Vector2 GetLocalDirection()
        {
            switch (rayDirection)
            {
                case RayDirection.Up:
                    return Vector2.up;
                case RayDirection.Left:
                    return Vector2.left;
                case RayDirection.Right:
                    return Vector2.right;
                case RayDirection.Custom:
                    return localDirection == Vector2.zero ? Vector2.down : localDirection;
                case RayDirection.Down:
                default:
                    return Vector2.down;
            }
        }
    }
}
