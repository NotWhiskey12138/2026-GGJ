using UnityEngine;
using NPCSystem.Controller;
using NPCSystem.Domain;

namespace NPCSystem.Frog
{
    [RequireComponent(typeof(FrogTonguePullImpulse))]
    public class FrogController : NpcController
    {
        private FrogDomain frogDomain;

        protected override void OnEnable()
        {
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
    }
}
