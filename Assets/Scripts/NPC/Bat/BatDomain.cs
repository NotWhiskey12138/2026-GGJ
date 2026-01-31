using UnityEngine;
using NPCSystem.Domain;

namespace NPCSystem.Bat
{
    public class BatDomain : NpcDomain
    {
        private readonly float curveAmplitude;
        private readonly float curveCycles;
        private readonly float possessedMoveSpeed;
        private float progress;
        private bool movingToB;

        public BatDomain(NpcStateData initialState, float curveAmplitude, float curveCycles, float possessedMoveSpeed)
            : base(initialState)
        {
            this.curveAmplitude = curveAmplitude;
            this.curveCycles = Mathf.Max(0.01f, curveCycles);
            this.possessedMoveSpeed = possessedMoveSpeed;
            movingToB = true;
            progress = 0f;
        }

        public override Vector2 IdleAction(
            Vector2 currentPosition,
            Vector2 pointA,
            Vector2 pointB,
            float walkSpeed,
            float deltaTime,
            float reachThreshold)
        {
            if (GetState().Phase != NpcPhase.Idle && GetState().Phase != NpcPhase.Possessed)
            {
                return currentPosition;
            }

            float pathLength = Vector2.Distance(pointA, pointB);
            if (pathLength <= 0.001f)
            {
                SetPosition(currentPosition);
                return currentPosition;
            }

            float delta = (walkSpeed * deltaTime) / pathLength;
            progress += movingToB ? delta : -delta;

            if (progress >= 1f)
            {
                progress = 1f;
                movingToB = false;
                SetPatrolDirection(false);
            }
            else if (progress <= 0f)
            {
                progress = 0f;
                movingToB = true;
                SetPatrolDirection(true);
            }

            Vector2 basePos = Vector2.Lerp(pointA, pointB, progress);
            Vector2 dir = (pointB - pointA).normalized;
            Vector2 perp = new Vector2(-dir.y, dir.x);
            float offset = Mathf.Sin(progress * Mathf.PI * 2f * curveCycles) * curveAmplitude;

            Vector2 newPosition = basePos + perp * offset;
            SetPosition(newPosition);
            return newPosition;
        }

        public override Vector2 PossessedAction(Vector2 targetPosition)
        {
            if (GetState().Phase != NpcPhase.Possessed)
            {
                return GetState().Position;
            }

            Vector2 current = GetState().Position;
            Vector2 newPosition = Vector2.MoveTowards(current, targetPosition, possessedMoveSpeed * Time.deltaTime);
            SetPosition(newPosition);
            return newPosition;
        }
    }
}
