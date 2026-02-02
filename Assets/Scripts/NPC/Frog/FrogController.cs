using UnityEngine;
using NPCSystem.Controller;
using NPCSystem.Domain;

namespace NPCSystem.Frog
{
    [RequireComponent(typeof(FrogTonguePullImpulse))]
    public class FrogController : NpcController
    {
        [Header("Idle Patrol")]
        [SerializeField] private float idleSpeed = 1.0f;
        [SerializeField] private Vector2 idleDirection = Vector2.right;
        [SerializeField] private bool requireGroundedForPatrol = true;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.12f;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private bool debugGrounded = false;
        [SerializeField] private bool disableEdgeDetectorInAir = true;
        [SerializeField] private bool debugIdle = false;

        private FrogDomain frogDomain;
        private FrogAbility frogAbility;
        private EnvironmentSystem.EdgeDetector[] edgeDetectors;
        private bool edgeDetectorsInitialized;

        protected override void OnEnable()
        {
            frogAbility = GetComponentInChildren<FrogAbility>(true);
            CacheEdgeDetectors();
            base.OnEnable();
        }

        protected override NpcDomain CreateDomain(Vector2 startPoint)
        {
            frogDomain = new FrogDomain(new NpcStateData(NpcId, NpcPhase.Idle, 0f, CanBePossessed, default, startPoint));
            frogDomain.SetIdleSpeed(idleSpeed);
            frogDomain.SetIdleDirection(idleDirection);
            return frogDomain;
        }

        protected override Vector2 IdleAction()
        {
            bool grounded = !requireGroundedForPatrol || IsGrounded();
            return frogDomain != null
                ? frogDomain.IdleStep(GetCurrentPosition(), Time.fixedDeltaTime, grounded)
                : GetCurrentPosition();
        }

        protected override void UpdateMovement()
        {
            bool grounded = !requireGroundedForPatrol || IsGrounded();
            UpdateEdgeDetectors(grounded);
            if (requireGroundedForPatrol && !grounded)
            {
                return;
            }

            // Move in idle via domain-driven step.
            base.UpdateMovement();
            if (debugIdle && frogDomain != null)
            {
                Debug.Log($"[{NpcId}] Idle tick speed={frogDomain.IdleSpeed:0.###} dir={frogDomain.IdleDirection}");
                LogEdgeState();
            }
        }

        protected override bool ShouldPatrol(NpcPhase phase)
        {
            return phase == NpcPhase.Idle || phase == NpcPhase.Possessed;
        }

        public override void HandlePossessedClick(GameObject target)
        {
            // Clicking is handled by ClickablePlatform to spawn bees.
            // Frog auto-catches via tongue when possessed.
        }

        public void FlipIdleDirection()
        {
            if (frogDomain == null)
            {
                Debug.LogWarning($"[{NpcId}] FlipIdleDirection called but frogDomain is null.");
                return;
            }

            frogDomain.FlipIdleDirection();
            Vector3 scale = transform.localScale;
            scale.x = -scale.x;
            transform.localScale = scale;
            Debug.Log($"[{NpcId}] FlipIdleDirection -> {frogDomain.IdleDirection}");
        }

        private bool IsGrounded()
        {
            if (groundCheck == null) return false;
            bool grounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer) != null;
            if (debugGrounded)
            {
                Debug.Log($"[{NpcId}] IsGrounded={grounded} pos={groundCheck.position} radius={groundCheckRadius} layerMask={groundLayer.value}");
            }
            return grounded;
        }

        private void CacheEdgeDetectors()
        {
            edgeDetectors = GetComponentsInChildren<EnvironmentSystem.EdgeDetector>(true);
            edgeDetectorsInitialized = true;
        }

        private void UpdateEdgeDetectors(bool grounded)
        {
            if (!disableEdgeDetectorInAir) return;
            if (!edgeDetectorsInitialized)
            {
                CacheEdgeDetectors();
            }
            if (edgeDetectors == null) return;

            bool enable = grounded;
            for (int i = 0; i < edgeDetectors.Length; i++)
            {
                if (edgeDetectors[i] != null)
                {
                    edgeDetectors[i].enabled = enable;
                }
            }
        }

        private void LogEdgeState()
        {
            if (!edgeDetectorsInitialized)
            {
                CacheEdgeDetectors();
            }
            if (edgeDetectors == null || edgeDetectors.Length == 0) return;
            for (int i = 0; i < edgeDetectors.Length; i++)
            {
                var detector = edgeDetectors[i];
                if (detector == null) continue;
                Debug.Log($"[{NpcId}] EdgeDetector[{i}] edge={detector.IsEdge} wall={detector.IsWall} enabled={detector.enabled}");
            }
        }
    }
}
