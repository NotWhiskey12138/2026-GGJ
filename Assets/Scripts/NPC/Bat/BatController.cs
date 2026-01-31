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

        [Header("Possessed Move")]
        [SerializeField] private float possessedMoveSpeed = 4f;
        [SerializeField] private float possessedReachThreshold = 0.1f;

        private Vector2 homePosition;
        private bool hasPossessedTarget;
        private Vector2 possessedTarget;

        protected override void OnEnable()
        {
            base.OnEnable();
            homePosition = transform.position;
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

        protected override void Update()
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
                transform.position = newPosition;

                if (Vector2.Distance(newPosition, possessedTarget) <= possessedReachThreshold)
                {
                    hasPossessedTarget = false;
                }

                return;
            }

            UpdateMovement();
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
    }
}
