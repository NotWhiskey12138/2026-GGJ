using UnityEngine;
using NPCSystem.Controller;
using NPCSystem.Domain;

namespace NPCSystem.Frog
{
    [RequireComponent(typeof(FrogTonguePullImpulse))]
    public class FrogController : NpcController
    {
        [Header("Frog Tongue")]
        [SerializeField] private bool enableTongueOnPossess = true;

        private FrogTonguePullImpulse tongue;
        private FrogDomain frogDomain;

        protected override void OnEnable()
        {
            tongue = GetComponent<FrogTonguePullImpulse>();
            if (tongue != null)
            {
                tongue.enabled = false;
            }

            base.OnEnable();
        }

        protected override NpcDomain CreateDomain(Vector2 startPoint)
        {
            frogDomain = new FrogDomain(new NpcStateData(NpcId, NpcPhase.Idle, 0f, CanBePossessed, default, startPoint));
            return frogDomain;
        }

        protected override Vector2 IdleAction()
        {
            // Idle: stay still.
            return frogDomain != null ? frogDomain.IdleStep(GetCurrentPosition()) : GetCurrentPosition();
        }

        protected override void UpdateMovement()
        {
            // Do not move via controller; tongue handles movement when possessed.
        }

        public override void HandlePossessedClick(GameObject target)
        {
            // Clicking is handled by ClickablePlatform to spawn bees.
            // Frog auto-catches via tongue when possessed.
        }

        protected override void HandlePossessionStarted(object sender, string targetNpcId)
        {
            base.HandlePossessionStarted(sender, targetNpcId);
            if (enableTongueOnPossess && tongue != null)
            {
                tongue.enabled = true;
            }
        }

        protected override void HandlePossessionEnded(object sender, string targetNpcId)
        {
            if (tongue != null)
            {
                tongue.enabled = false;
            }
            base.HandlePossessionEnded(sender, targetNpcId);
        }
    }
}
