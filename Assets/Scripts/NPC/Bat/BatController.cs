using UnityEngine;
using NPCSystem.Controller;
using NPCSystem.Domain;

namespace NPCSystem.Bat
{
    public class BatController : NpcController
    {
        [Header("Bat Path")]
        [SerializeField] private float curveAmplitude = 1f;
        [SerializeField] private float curveCycles = 1f;
        [SerializeField] private float patrolSpeed = 2f;

        [Header("Possessed Move")]
        [SerializeField] private float possessedMoveSpeed = 4f;
        [SerializeField] private float possessedReachThreshold = 0.1f;

        [Header("Patrol Settings")]
        [SerializeField] private Transform patrolPointAObj;
        [SerializeField] private Transform patrolPointBObj;
        [SerializeField] private float reachThreshold = 0.1f;

        private Vector2 homePosition;
        private bool hasPossessedTarget;
        private Vector2 possessedTarget;

        protected override void OnEnable()
        {
            Vector2 startPoint = GetPatrolPointA();
            MoveTo(startPoint);
            base.OnEnable();
            homePosition = GetCurrentPosition();
        }

        protected override NpcDomain CreateDomain(Vector2 startPoint)
        {
            return new BatDomain(
                new NpcStateData(NpcId, NpcPhase.Idle, 0f, CanBePossessed, default, startPoint),
                curveAmplitude,
                curveCycles,
                possessedMoveSpeed
            );
        }

        protected override void FixedUpdate()
        {
            if (domain == null) return;

            var state = domain.GetState();
            if (state.Phase == NpcPhase.Stunned)
            {
                domain.UpdateStun(Time.deltaTime);
                return;
            }

            if (state.Phase == NpcPhase.Possessed && hasPossessedTarget)
            {
                Vector2 newPosition = domain.PossessedAction(possessedTarget);
                MoveTo(newPosition);

                if (Vector2.Distance(newPosition, possessedTarget) <= possessedReachThreshold)
                {
                    hasPossessedTarget = false;
                }

                return;
            }

            UpdateMovement();
        }

        protected override void UpdateMovement()
        {
            if (domain == null) return;

            Vector2 pointA = GetPatrolPointA();
            Vector2 pointB = GetPatrolPointB();
            Vector2 newPosition = domain.IdleAction(
                GetCurrentPosition(),
                pointA,
                pointB,
                patrolSpeed,
                Time.fixedDeltaTime,
                reachThreshold
            );
            MoveTo(newPosition);
        }

        public override void HandlePossessedClick(GameObject target)
        {
            if (domain == null || target == null) return;
            if (domain.GetState().Phase != NpcPhase.Possessed) return;
            if (!IsValidPossessedTarget(target)) return;

            possessedTarget = target.transform.position;
            hasPossessedTarget = true;
        }

        protected override bool IsValidPossessedTarget(GameObject target)
        {
            return target.GetComponent<BatTarget>() != null;
        }

        protected override void HandlePossessionEnded(object sender, string targetNpcId)
        {
            if (targetNpcId != NpcId) return;
            if (domain == null) return;

            hasPossessedTarget = false;
            domain.Release(0f);
            Debug.Log($"[{NpcId}] Bat released, keep position.");
        }

        private Vector2 GetPatrolPointA()
        {
            return patrolPointAObj != null ? (Vector2)patrolPointAObj.position : transform.position;
        }

        private Vector2 GetPatrolPointB()
        {
            return patrolPointBObj != null ? (Vector2)patrolPointBObj.position : transform.position;
        }
    }
}
