using System;
using System.Collections.Generic;

namespace NPC.Domain
{
    /// <summary>
    /// Core domain logic for NPC state management
    /// Singleton pattern - manages multiple NPCs via registry
    /// Pure C# - no Unity dependencies for testability
    /// </summary>
    public class NpcDomain
    {
        #region Singleton

        private static NpcDomain _instance;
        private static readonly object _lock = new object();

        public static NpcDomain Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new NpcDomain();
                        }
                    }
                }
                return _instance;
            }
        }

        public static void SetInstance(NpcDomain instance)
        {
            lock (_lock)
            {
                _instance = instance;
            }
        }

        public static void ResetInstance()
        {
            lock (_lock)
            {
                _instance = null;
            }
        }

        #endregion

        #region Events - Broadcast System

        /// <summary>
        /// Fired whenever any NPC state changes
        /// </summary>
        public event EventHandler<NpcStateChangedEventArgs> OnStateChanged;

        /// <summary>
        /// Fired when NPC starts being lured
        /// </summary>
        public event EventHandler<string> OnLureStarted;

        /// <summary>
        /// Fired when NPC becomes fully possessed
        /// </summary>
        public event EventHandler<string> OnPossessed;

        /// <summary>
        /// Fired when NPC is released from possession
        /// </summary>
        public event EventHandler<string> OnReleased;

        /// <summary>
        /// Fired when NPC recovers from stun
        /// </summary>
        public event EventHandler<string> OnStunEnded;

        #endregion

        #region State

        // Registry of all NPC states
        private readonly Dictionary<string, NpcStateData> _npcStates;

        // Valid state transitions
        private readonly Dictionary<NpcPhase, HashSet<NpcPhase>> _validTransitions;

        #endregion

        #region Constructor

        private NpcDomain()
        {
            _npcStates = new Dictionary<string, NpcStateData>();
            _validTransitions = InitializeTransitions();
        }

        private Dictionary<NpcPhase, HashSet<NpcPhase>> InitializeTransitions()
        {
            return new Dictionary<NpcPhase, HashSet<NpcPhase>>
            {
                { NpcPhase.Idle, new HashSet<NpcPhase> { NpcPhase.Lured } },
                { NpcPhase.Lured, new HashSet<NpcPhase> { NpcPhase.Possessed, NpcPhase.Idle } },
                { NpcPhase.Possessed, new HashSet<NpcPhase> { NpcPhase.Stunned } },
                { NpcPhase.Stunned, new HashSet<NpcPhase> { NpcPhase.Idle } }
            };
        }

        #endregion

        #region Registry Methods

        /// <summary>
        /// Register a new NPC to the domain
        /// </summary>
        public void RegisterNpc(string npcId, float seduceResistance = 0.2f, bool canBePossessed = true)
        {
            if (string.IsNullOrEmpty(npcId))
            {
                return;
            }

            if (!_npcStates.ContainsKey(npcId))
            {
                _npcStates[npcId] = new NpcStateData(
                    npcId,
                    NpcPhase.Idle,
                    seduceResistance,
                    0f,
                    0f,
                    canBePossessed
                );
            }
        }

        /// <summary>
        /// Unregister an NPC from the domain
        /// </summary>
        public void UnregisterNpc(string npcId)
        {
            if (_npcStates.ContainsKey(npcId))
            {
                _npcStates.Remove(npcId);
            }
        }

        /// <summary>
        /// Get current state of an NPC
        /// </summary>
        public NpcStateData? GetState(string npcId)
        {
            if (_npcStates.TryGetValue(npcId, out var state))
            {
                return state;
            }
            return null;
        }

        /// <summary>
        /// Check if NPC is registered
        /// </summary>
        public bool IsRegistered(string npcId)
        {
            return _npcStates.ContainsKey(npcId);
        }

        #endregion

        #region State Transition Methods

        /// <summary>
        /// Start luring an NPC (called when mask begins seduction)
        /// </summary>
        public bool StartLure(string npcId)
        {
            if (!TryGetState(npcId, out var currentState))
            {
                return false;
            }

            if (!currentState.IsSeducible)
            {
                return false;
            }

            if (!CanTransitionTo(currentState.Phase, NpcPhase.Lured))
            {
                return false;
            }

            var newState = currentState
                .WithPhase(NpcPhase.Lured)
                .WithLuredProgress(0f);

            TransitionTo(npcId, currentState, newState);
            OnLureStarted?.Invoke(this, npcId);

            return true;
        }

        /// <summary>
        /// Update lure progress (call during lure process)
        /// </summary>
        /// <param name="npcId">NPC identifier</param>
        /// <param name="deltaProgress">Progress increment (affected by mask seduce power)</param>
        /// <returns>True if fully lured</returns>
        public bool UpdateLureProgress(string npcId, float deltaProgress)
        {
            if (!TryGetState(npcId, out var currentState))
            {
                return false;
            }

            if (currentState.Phase != NpcPhase.Lured)
            {
                return false;
            }

            // Apply resistance
            float effectiveProgress = deltaProgress * (1f - currentState.SeduceResistance);
            float newProgress = currentState.LuredProgress + effectiveProgress;

            var newState = currentState.WithLuredProgress(newProgress);
            _npcStates[npcId] = newState;

            // Check if fully lured
            return newState.IsFullyLured;
        }

        /// <summary>
        /// Cancel lure and return NPC to idle
        /// </summary>
        public bool CancelLure(string npcId)
        {
            if (!TryGetState(npcId, out var currentState))
            {
                return false;
            }

            if (currentState.Phase != NpcPhase.Lured)
            {
                return false;
            }

            var newState = currentState
                .WithPhase(NpcPhase.Idle)
                .ResetLuredProgress();

            TransitionTo(npcId, currentState, newState);

            return true;
        }

        /// <summary>
        /// Possess the NPC (called when mask attaches)
        /// </summary>
        public bool Possess(string npcId)
        {
            if (!TryGetState(npcId, out var currentState))
            {
                return false;
            }

            if (!CanTransitionTo(currentState.Phase, NpcPhase.Possessed))
            {
                return false;
            }

            var newState = currentState
                .WithPhase(NpcPhase.Possessed)
                .ResetLuredProgress();

            TransitionTo(npcId, currentState, newState);
            OnPossessed?.Invoke(this, npcId);

            return true;
        }

        /// <summary>
        /// Release NPC from possession (called when mask drops)
        /// </summary>
        /// <param name="npcId">NPC identifier</param>
        /// <param name="stunDuration">How long NPC is stunned after release</param>
        public bool Release(string npcId, float stunDuration = 2f)
        {
            if (!TryGetState(npcId, out var currentState))
            {
                return false;
            }

            if (!CanTransitionTo(currentState.Phase, NpcPhase.Stunned))
            {
                return false;
            }

            var newState = currentState
                .WithPhase(NpcPhase.Stunned)
                .WithStunRemaining(stunDuration);

            TransitionTo(npcId, currentState, newState);
            OnReleased?.Invoke(this, npcId);

            return true;
        }

        /// <summary>
        /// Update stun timer (call from Update loop)
        /// </summary>
        /// <param name="npcId">NPC identifier</param>
        /// <param name="deltaTime">Time since last update</param>
        /// <returns>True if stun ended this frame</returns>
        public bool UpdateStun(string npcId, float deltaTime)
        {
            if (!TryGetState(npcId, out var currentState))
            {
                return false;
            }

            if (currentState.Phase != NpcPhase.Stunned)
            {
                return false;
            }

            float newStunTime = currentState.StunRemaining - deltaTime;

            if (newStunTime <= 0f)
            {
                // Stun ended, return to idle
                var newState = currentState.Reset();
                TransitionTo(npcId, currentState, newState);
                OnStunEnded?.Invoke(this, npcId);
                return true;
            }

            // Update stun timer
            _npcStates[npcId] = currentState.WithStunRemaining(newStunTime);
            return false;
        }

        /// <summary>
        /// Force reset NPC to idle (for error recovery)
        /// </summary>
        public void ForceReset(string npcId)
        {
            if (!TryGetState(npcId, out var currentState))
            {
                return;
            }

            var newState = currentState.Reset();
            TransitionTo(npcId, currentState, newState);
        }

        #endregion

        #region Validation & Helpers

        private bool TryGetState(string npcId, out NpcStateData state)
        {
            if (_npcStates.TryGetValue(npcId, out state))
            {
                return true;
            }
            state = default;
            return false;
        }

        private bool CanTransitionTo(NpcPhase currentPhase, NpcPhase targetPhase)
        {
            if (!_validTransitions.TryGetValue(currentPhase, out var validTargets))
            {
                return false;
            }

            return validTargets.Contains(targetPhase);
        }

        private void TransitionTo(string npcId, NpcStateData previousState, NpcStateData newState)
        {
            _npcStates[npcId] = newState;
            OnStateChanged?.Invoke(this, new NpcStateChangedEventArgs(npcId, previousState, newState));
        }

        /// <summary>
        /// Get all registered NPC IDs
        /// </summary>
        public IEnumerable<string> GetAllNpcIds()
        {
            return _npcStates.Keys;
        }

        /// <summary>
        /// Get all NPCs that can be seduced
        /// </summary>
        public IEnumerable<string> GetSeducibleNpcs()
        {
            foreach (var kvp in _npcStates)
            {
                if (kvp.Value.IsSeducible)
                {
                    yield return kvp.Key;
                }
            }
        }

        /// <summary>
        /// Get currently possessed NPC (if any)
        /// </summary>
        public string GetPossessedNpcId()
        {
            foreach (var kvp in _npcStates)
            {
                if (kvp.Value.IsUnderControl)
                {
                    return kvp.Key;
                }
            }
            return null;
        }

        #endregion
    }
}
