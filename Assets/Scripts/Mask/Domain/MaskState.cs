using System;

namespace Mask.Domain
{
    /// <summary>
    /// Represents the current phase of the mask lifecycle
    /// </summary>
    public enum MaskPhase
    {
        Idle,       // On ground, waiting to be activated
        Seducing,   // Actively luring an NPC
        Attaching,  // In process of attaching to NPC face
        Possessed,  // Controlling an NPC
        Dropping    // Falling off NPC, transitioning back to Idle
    }

    /// <summary>
    /// Immutable state snapshot of the mask at any given moment
    /// </summary>
    public struct MaskStateData
    {
        public MaskPhase Phase { get; }
        public string TargetNpcId { get; }
        public float PossessionDuration { get; }
        public float SeducePower { get; }
        public float ControlStrength { get; }

        public MaskStateData(
            MaskPhase phase,
            string targetNpcId = null,
            float possessionDuration = 0f,
            float seducePower = 1f,
            float controlStrength = 1f)
        {
            Phase = phase;
            TargetNpcId = targetNpcId;
            PossessionDuration = possessionDuration;
            SeducePower = seducePower;
            ControlStrength = controlStrength;
        }

        /// <summary>
        /// Creates a new state with updated phase
        /// </summary>
        public MaskStateData WithPhase(MaskPhase newPhase)
        {
            return new MaskStateData(newPhase, TargetNpcId, PossessionDuration, SeducePower, ControlStrength);
        }

        /// <summary>
        /// Creates a new state with updated target
        /// </summary>
        public MaskStateData WithTarget(string npcId)
        {
            return new MaskStateData(Phase, npcId, PossessionDuration, SeducePower, ControlStrength);
        }

        /// <summary>
        /// Creates a new state with updated possession duration
        /// </summary>
        public MaskStateData WithDuration(float duration)
        {
            return new MaskStateData(Phase, TargetNpcId, duration, SeducePower, ControlStrength);
        }

        /// <summary>
        /// Clears target and resets to idle state
        /// </summary>
        public MaskStateData Reset()
        {
            return new MaskStateData(MaskPhase.Idle, null, 0f, SeducePower, ControlStrength);
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
