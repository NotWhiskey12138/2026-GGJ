using UnityEngine;
using NPCSystem.Domain;

namespace NPCSystem.Frog
{
    public class FrogDomain : NpcDomain
    {
        private Vector2 velocity;

        public FrogDomain(NpcStateData initialState) : base(initialState)
        {
            velocity = Vector2.zero;
        }

        public Vector2 Velocity => velocity;

        public Vector2 IdleStep(Vector2 currentPosition)
        {
            velocity = Vector2.zero;
            SetPosition(currentPosition);
            return currentPosition;
        }

        public Vector2 MoveToTarget(Vector2 currentPosition, Vector2 targetPosition, float speed, float deltaTime)
        {
            Vector2 toTarget = targetPosition - currentPosition;
            if (toTarget.sqrMagnitude <= 0.0001f)
            {
                velocity = Vector2.zero;
                SetPosition(currentPosition);
                return currentPosition;
            }

            Vector2 dir = toTarget.normalized;
            velocity = dir * speed;
            Vector2 newPosition = currentPosition + velocity * deltaTime;
            SetPosition(newPosition);
            return newPosition;
        }
    }
}
