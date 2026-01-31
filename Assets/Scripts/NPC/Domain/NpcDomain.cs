using System;
using System.Collections.Generic;

namespace NPCSystem.Domain
{
    /// <summary>
    /// Simplified domain logic for NPC state management
    /// Only 3 states: Idle, Possessed, Stunned
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

        public static void ResetInstance()
        {
            lock (_lock)
            {
                _instance = null;
            }
        }

        #endregion

        #region Events

        public event EventHandler<NpcStateChangedEventArgs> OnStateChanged;
        public event EventHandler<string> OnPossessed;
        public event EventHandler<string> OnReleased;
        public event EventHandler<string> OnStunEnded;

        #endregion

        #region State

        private readonly Dictionary<string, NpcStateData> _npcStates;

        #endregion

        #region Constructor

        private NpcDomain()
        {
            _npcStates = new Dictionary<string, NpcStateData>();
        }

        #endregion

        #region Registry Methods

        public void RegisterNpc(string npcId, bool canBePossessed = true, PatrolData patrol = default)
        {
            if (string.IsNullOrEmpty(npcId)) return;

            if (!_npcStates.ContainsKey(npcId))
            {
                _npcStates[npcId] = new NpcStateData(npcId, NpcPhase.Idle, 0f, canBePossessed, patrol);
            }
        }

        public void UnregisterNpc(string npcId)
        {
            if (_npcStates.ContainsKey(npcId))
            {
                _npcStates.Remove(npcId);
            }
        }

        public NpcStateData? GetState(string npcId)
        {
            if (_npcStates.TryGetValue(npcId, out var state))
            {
                return state;
            }
            return null;
        }

        public void SetPatrolDirection(string npcId, bool movingToB)
        {
            if (!_npcStates.TryGetValue(npcId, out var currentState)) return;
            _npcStates[npcId] = currentState.WithPatrolDirection(movingToB);
        }

        #endregion

        #region State Transition Methods

        /// <summary>
        /// Possess an NPC. Only works when Idle.
        /// </summary>
        public bool Possess(string npcId)
        {
            if (!_npcStates.TryGetValue(npcId, out var currentState)) return false;
            if (!currentState.IsSeducible) return false;

            var newState = currentState.WithPhase(NpcPhase.Possessed);
            TransitionTo(npcId, currentState, newState);
            OnPossessed?.Invoke(this, npcId);

            return true;
        }

        /// <summary>
        /// Release an NPC from possession. Enters stunned state.
        /// </summary>
        public bool Release(string npcId, float stunDuration = 2f)
        {
            if (!_npcStates.TryGetValue(npcId, out var currentState)) return false;
            if (currentState.Phase != NpcPhase.Possessed) return false;

            var newState = currentState
                .WithPhase(NpcPhase.Stunned)
                .WithStunRemaining(stunDuration);

            TransitionTo(npcId, currentState, newState);
            OnReleased?.Invoke(this, npcId);

            return true;
        }

        /// <summary>
        /// Update stun timer. Call from Update loop.
        /// </summary>
        public bool UpdateStun(string npcId, float deltaTime)
        {
            if (!_npcStates.TryGetValue(npcId, out var currentState)) return false;
            if (currentState.Phase != NpcPhase.Stunned) return false;

            float newStunTime = currentState.StunRemaining - deltaTime;

            if (newStunTime <= 0f)
            {
                var newState = currentState.Reset();
                TransitionTo(npcId, currentState, newState);
                OnStunEnded?.Invoke(this, npcId);
                return true;
            }

            _npcStates[npcId] = currentState.WithStunRemaining(newStunTime);
            return false;
        }

        /// <summary>
        /// Force reset to idle
        /// </summary>
        public void ForceReset(string npcId)
        {
            if (!_npcStates.TryGetValue(npcId, out var currentState)) return;
            var newState = currentState.Reset();
            TransitionTo(npcId, currentState, newState);
        }

        #endregion

        #region Private Methods

        private void TransitionTo(string npcId, NpcStateData previousState, NpcStateData newState)
        {
            _npcStates[npcId] = newState;
            OnStateChanged?.Invoke(this, new NpcStateChangedEventArgs(npcId, previousState, newState));
        }

        #endregion

        #region Query Methods

        public IEnumerable<string> GetAllNpcIds()
        {
            return _npcStates.Keys;
        }

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
