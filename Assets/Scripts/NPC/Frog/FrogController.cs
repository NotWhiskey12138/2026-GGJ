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

        private FrogDomain frogDomain;

        protected override void OnEnable()
        {
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
            return frogDomain != null
                ? frogDomain.IdleStep(GetCurrentPosition(), Time.fixedDeltaTime)
                : GetCurrentPosition();
        }

        protected override void UpdateMovement()
        {
            // Move in idle via domain-driven step.
            base.UpdateMovement();
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
            Debug.Log($"[{NpcId}] FlipIdleDirection -> {frogDomain.IdleDirection}");
        }
    }
}
