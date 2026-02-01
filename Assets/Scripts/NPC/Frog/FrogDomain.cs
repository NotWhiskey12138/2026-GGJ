using UnityEngine;
using NPCSystem.Domain;

namespace NPCSystem.Frog
{
    public class FrogDomain : NpcDomain
    {
        private Vector2 velocity;
        private Vector2 idleDirection = Vector2.right;
        private float idleSpeed = 1.0f;

        public FrogDomain(NpcStateData initialState) : base(initialState)
        {
            velocity = Vector2.zero;
        }

        public Vector2 Velocity => velocity;
        public Vector2 IdleDirection => idleDirection;
        public float IdleSpeed => idleSpeed;

        public void SetIdleDirection(Vector2 direction)
        {
            idleDirection = direction == Vector2.zero ? Vector2.right : direction.normalized;
        }

        public void FlipIdleDirection()
        {
            idleDirection = new Vector2(-idleDirection.x, idleDirection.y);
        }

        public void SetIdleSpeed(float speed)
        {
            idleSpeed = Mathf.Max(0f, speed);
        }

        public Vector2 IdleStep(Vector2 currentPosition, float deltaTime, bool grounded)
        {
            if (!grounded)
            {
                velocity = Vector2.zero;
                SetPosition(currentPosition);
                return currentPosition;
            }

            Vector2 dir = idleDirection == Vector2.zero ? Vector2.right : idleDirection.normalized;
            velocity = dir * idleSpeed;
            Vector2 newPosition = currentPosition + velocity * deltaTime;
            SetPosition(newPosition);
            return newPosition;
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
