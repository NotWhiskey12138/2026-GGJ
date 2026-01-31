using System;

namespace NPC.Domain
{
    /// <summary>
    /// Represents the current behavior phase of an NPC
    /// </summary>
    public enum NpcPhase
    {
        Idle,       // Default state, running normal AI behavior
        Lured,      // Being seduced by mask, moving toward it
        Possessed,  // Under mask control, player controls this NPC
        Stunned     // Just released from possession, temporarily disabled
    }

    /// <summary>
    /// Immutable state snapshot of an NPC at any given moment
    /// </summary>
    public struct NpcStateData
    {
        public string NpcId { get; }
        public NpcPhase Phase { get; }
        public float SeduceResistance { get; }      // 0-1, higher = harder to seduce
        public float LuredProgress { get; }         // 0-1, when reaches 1 = fully lured
        public float StunRemaining { get; }         // Remaining stun time after release
        public bool CanBePossessed { get; }         // Some NPCs are immune

        public NpcStateData(
            string npcId,
            NpcPhase phase = NpcPhase.Idle,
            float seduceResistance = 0.2f,
            float luredProgress = 0f,
            float stunRemaining = 0f,
            bool canBePossessed = true)
        {
            NpcId = npcId;
            Phase = phase;
            SeduceResistance = seduceResistance;
            LuredProgress = luredProgress;
            StunRemaining = stunRemaining;
            CanBePossessed = canBePossessed;
        }

        #region Builder Methods

        public NpcStateData WithPhase(NpcPhase newPhase)
        {
            return new NpcStateData(NpcId, newPhase, SeduceResistance, LuredProgress, StunRemaining, CanBePossessed);
        }

        public NpcStateData WithLuredProgress(float progress)
        {
            float clamped = Math.Max(0f, Math.Min(1f, progress));
            return new NpcStateData(NpcId, Phase, SeduceResistance, clamped, StunRemaining, CanBePossessed);
        }

        public NpcStateData WithStunRemaining(float duration)
        {
            return new NpcStateData(NpcId, Phase, SeduceResistance, LuredProgress, duration, CanBePossessed);
        }

        public NpcStateData ResetLuredProgress()
        {
            return new NpcStateData(NpcId, Phase, SeduceResistance, 0f, StunRemaining, CanBePossessed);
        }

        public NpcStateData Reset()
        {
            return new NpcStateData(NpcId, NpcPhase.Idle, SeduceResistance, 0f, 0f, CanBePossessed);
        }

        #endregion

        #region Helper Properties

        /// <summary>
        /// Check if NPC is available for seduction
        /// </summary>
        public bool IsSeducible => CanBePossessed && Phase == NpcPhase.Idle;

        /// <summary>
        /// Check if NPC is currently under mask control
        /// </summary>
        public bool IsUnderControl => Phase == NpcPhase.Possessed;

        /// <summary>
        /// Check if lure is complete
        /// </summary>
        public bool IsFullyLured => LuredProgress >= 1f;

        #endregion

        public override string ToString()
        {
            return $"[NPC:{NpcId}] Phase: {Phase}, LuredProgress: {LuredProgress:P0}";
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
