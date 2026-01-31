using System;

namespace MaskSystem.Domain
{
    /// <summary>
    /// Simplified mask phases - only 2 states needed
    /// </summary>
    public enum MaskPhase
    {
        Idle,       // Free, can possess a new NPC
        Possessed   // Attached to and controlling an NPC
    }

    /// <summary>
    /// Immutable state snapshot of the mask
    /// </summary>
    public struct MaskStateData
    {
        public MaskPhase Phase { get; }
        public string TargetNpcId { get; }
        public float PossessionDuration { get; }

        public MaskStateData(MaskPhase phase, string targetNpcId = null, float possessionDuration = 0f)
        {
            Phase = phase;
            TargetNpcId = targetNpcId;
            PossessionDuration = possessionDuration;
        }

        public MaskStateData WithPhase(MaskPhase newPhase)
        {
            return new MaskStateData(newPhase, TargetNpcId, PossessionDuration);
        }

        public MaskStateData WithTarget(string npcId)
        {
            return new MaskStateData(Phase, npcId, PossessionDuration);
        }

        public MaskStateData WithDuration(float duration)
        {
            return new MaskStateData(Phase, TargetNpcId, duration);
        }

        public MaskStateData Reset()
        {
            return new MaskStateData(MaskPhase.Idle, null, 0f);
        }

        public override string ToString()
        {
            return $"[MaskState] Phase: {Phase}, Target: {TargetNpcId ?? "None"}, Duration: {PossessionDuration:F2}s";
        }
    }

    /// <summary>
    /// Event arguments for state change broadcasts
    /// </summary>
    public class MaskStateChangedEventArgs : EventArgs
    {
        public MaskStateData PreviousState { get; }
        public MaskStateData CurrentState { get; }
        public MaskPhase FromPhase => PreviousState.Phase;
        public MaskPhase ToPhase => CurrentState.Phase;

        public MaskStateChangedEventArgs(MaskStateData previous, MaskStateData current)
        {
            PreviousState = previous;
            CurrentState = current;
        }
    }
}
