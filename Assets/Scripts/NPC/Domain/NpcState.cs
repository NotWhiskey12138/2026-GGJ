using System;

namespace NPCSystem.Domain
{
    /// <summary>
    /// Simplified NPC phases - only 3 states needed
    /// </summary>
    public enum NpcPhase
    {
        Idle,       // Default state, patrolling
        Possessed,  // Under mask control
        Stunned     // Just released, temporarily disabled
    }

    /// <summary>
    /// Patrol data for NPC movement
    /// </summary>
    public struct PatrolData
    {
        public float PointAX { get; }
        public float PointAY { get; }
        public float PointBX { get; }
        public float PointBY { get; }
        public bool MovingToB { get; }

        public PatrolData(float pointAX, float pointAY, float pointBX, float pointBY, bool movingToB = true)
        {
            PointAX = pointAX;
            PointAY = pointAY;
            PointBX = pointBX;
            PointBY = pointBY;
            MovingToB = movingToB;
        }

        public PatrolData WithMovingToB(bool movingToB)
        {
            return new PatrolData(PointAX, PointAY, PointBX, PointBY, movingToB);
        }
    }

    /// <summary>
    /// Immutable state snapshot of an NPC
    /// </summary>
    public struct NpcStateData
    {
        public string NpcId { get; }
        public NpcPhase Phase { get; }
        public float StunRemaining { get; }
        public bool CanBePossessed { get; }
        public PatrolData Patrol { get; }

        public NpcStateData(
            string npcId,
            NpcPhase phase = NpcPhase.Idle,
            float stunRemaining = 0f,
            bool canBePossessed = true,
            PatrolData patrol = default)
        {
            NpcId = npcId;
            Phase = phase;
            StunRemaining = stunRemaining;
            CanBePossessed = canBePossessed;
            Patrol = patrol;
        }

        public NpcStateData WithPhase(NpcPhase newPhase)
        {
            return new NpcStateData(NpcId, newPhase, StunRemaining, CanBePossessed, Patrol);
        }

        public NpcStateData WithStunRemaining(float duration)
        {
            return new NpcStateData(NpcId, Phase, duration, CanBePossessed, Patrol);
        }

        public NpcStateData WithPatrolDirection(bool movingToB)
        {
            return new NpcStateData(NpcId, Phase, StunRemaining, CanBePossessed, Patrol.WithMovingToB(movingToB));
        }

        public NpcStateData Reset()
        {
            return new NpcStateData(NpcId, NpcPhase.Idle, 0f, CanBePossessed, Patrol);
        }

        public bool IsSeducible => CanBePossessed && Phase == NpcPhase.Idle;
        public bool IsUnderControl => Phase == NpcPhase.Possessed;

        public override string ToString()
        {
            return $"[NPC:{NpcId}] Phase: {Phase}";
        }
    }

    /// <summary>
    /// Event arguments for NPC state changes
    /// </summary>
    public class NpcStateChangedEventArgs : EventArgs
    {
        public string NpcId { get; }
        public NpcStateData PreviousState { get; }
        public NpcStateData CurrentState { get; }
        public NpcPhase FromPhase => PreviousState.Phase;
        public NpcPhase ToPhase => CurrentState.Phase;

        public NpcStateChangedEventArgs(string npcId, NpcStateData previous, NpcStateData current)
        {
            NpcId = npcId;
            PreviousState = previous;
            CurrentState = current;
        }
    }
}
