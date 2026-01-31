using System;
using System.Collections.Generic;

namespace Mask.Domain
{
    /// <summary>
    /// Core domain logic for Mask state management
    /// Singleton pattern for easy global access
    /// Pure C# - no Unity dependencies for testability
    /// </summary>
    public class MaskDomain
    {
        #region Singleton

        private static MaskDomain _instance;
        private static readonly object _lock = new object();

        public static MaskDomain Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new MaskDomain();
                        }
                    }
                }
                return _instance;
            }
        }

        // Allow manual instance injection for testing
        public static void SetInstance(MaskDomain instance)
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
        /// Fired whenever the mask state changes
        /// Subscribe: MaskDomain.Instance.OnStateChanged += YourHandler;
        /// </summary>
        public event EventHandler<MaskStateChangedEventArgs> OnStateChanged;

        /// <summary>
        /// Fired when seduce action begins
        /// </summary>
        public event EventHandler<string> OnSeduceStarted; // passes target NPC id

        /// <summary>
        /// Fired when mask successfully attaches to NPC
        /// </summary>
        public event EventHandler<string> OnPossessionStarted; // passes possessed NPC id

        /// <summary>
        /// Fired when mask drops from NPC
        /// </summary>
        public event EventHandler<string> OnPossessionEnded; // passes released NPC id

        /// <summary>
        /// Fired when seduce fails (no target, interrupted, etc.)
        /// </summary>
        public event EventHandler<string> OnSeduceFailed; // passes reason

        #endregion

        #region State

        private MaskStateData _currentState;
        public MaskStateData CurrentState => _currentState;

        // Valid state transitions map
        private readonly Dictionary<MaskPhase, HashSet<MaskPhase>> _validTransitions;

        #endregion

        #region Constructor

        private MaskDomain()
        {
            _currentState = new MaskStateData(MaskPhase.Idle);
            _validTransitions = InitializeTransitions();
        }

        private Dictionary<MaskPhase, HashSet<MaskPhase>> InitializeTransitions()
        {
            return new Dictionary<MaskPhase, HashSet<MaskPhase>>
            {
                { MaskPhase.Idle, new HashSet<MaskPhase> { MaskPhase.Seducing } },
                { MaskPhase.Seducing, new HashSet<MaskPhase> { MaskPhase.Attaching, MaskPhase.Idle } },
                { MaskPhase.Attaching, new HashSet<MaskPhase> { MaskPhase.Possessed, MaskPhase.Idle } },
                { MaskPhase.Possessed, new HashSet<MaskPhase> { MaskPhase.Dropping } },
                { MaskPhase.Dropping, new HashSet<MaskPhase> { MaskPhase.Idle } }
            };
        }

        #endregion

        #region State Transition Methods

        /// <summary>
        /// Attempts to start seducing a target NPC
        /// </summary>
        /// <param name="targetNpcId">Unique identifier of the target NPC</param>
        /// <returns>True if transition successful</returns>
        public bool StartSeduce(string targetNpcId)
        {
            if (string.IsNullOrEmpty(targetNpcId))
            {
                OnSeduceFailed?.Invoke(this, "No target specified");
                return false;
            }

            if (!CanTransitionTo(MaskPhase.Seducing))
            {
                OnSeduceFailed?.Invoke(this, $"Cannot seduce from {_currentState.Phase} state");
                return false;
            }

            var newState = _currentState
                .WithPhase(MaskPhase.Seducing)
                .WithTarget(targetNpcId);

            TransitionTo(newState);
            OnSeduceStarted?.Invoke(this, targetNpcId);

            return true;
        }

        /// <summary>
        /// Cancels seduction and returns to idle
        /// </summary>
        public bool CancelSeduce()
        {
            if (_currentState.Phase != MaskPhase.Seducing)
            {
                return false;
            }

            var newState = _currentState.Reset();
            TransitionTo(newState);
            OnSeduceFailed?.Invoke(this, "Seduce cancelled");

            return true;
        }

        /// <summary>
        /// Begins attaching to the target NPC
        /// </summary>
        public bool BeginAttach()
        {
            if (!CanTransitionTo(MaskPhase.Attaching))
            {
                return false;
            }

            var newState = _currentState.WithPhase(MaskPhase.Attaching);
            TransitionTo(newState);

            return true;
        }

        /// <summary>
        /// Completes possession of the NPC
        /// </summary>
        public bool CompletePossession()
        {
            if (!CanTransitionTo(MaskPhase.Possessed))
            {
                return false;
            }

            var newState = _currentState
                .WithPhase(MaskPhase.Possessed)
                .WithDuration(0f);

            TransitionTo(newState);
            OnPossessionStarted?.Invoke(this, _currentState.TargetNpcId);

            return true;
        }

        /// <summary>
        /// Updates possession duration (call from Update loop)
        /// </summary>
        /// <param name="deltaTime">Time since last update</param>
        public void UpdatePossessionTime(float deltaTime)
        {
            if (_currentState.Phase != MaskPhase.Possessed)
            {
                return;
            }

            _currentState = _currentState.WithDuration(_currentState.PossessionDuration + deltaTime);
        }

        /// <summary>
        /// Initiates dropping the mask from NPC
        /// </summary>
        public bool StartDrop()
        {
            if (!CanTransitionTo(MaskPhase.Dropping))
            {
                return false;
            }

            string releasedNpcId = _currentState.TargetNpcId;
            var newState = _currentState.WithPhase(MaskPhase.Dropping);
            TransitionTo(newState);
            OnPossessionEnded?.Invoke(this, releasedNpcId);

            return true;
        }

        /// <summary>
        /// Completes drop and returns to idle
        /// </summary>
        public bool CompleteDrop()
        {
            if (!CanTransitionTo(MaskPhase.Idle))
            {
                return false;
            }

            var newState = _currentState.Reset();
            TransitionTo(newState);

            return true;
        }

        /// <summary>
        /// Force reset to idle state (for error recovery)
        /// </summary>
        public void ForceReset()
        {
            var previousNpc = _currentState.TargetNpcId;
            var wasInPossession = _currentState.Phase == MaskPhase.Possessed;

            var newState = _currentState.Reset();
            TransitionTo(newState);

            if (wasInPossession && !string.IsNullOrEmpty(previousNpc))
            {
                OnPossessionEnded?.Invoke(this, previousNpc);
            }
        }

        #endregion

        #region Validation & Helpers

        private bool CanTransitionTo(MaskPhase targetPhase)
        {
            if (!_validTransitions.TryGetValue(_currentState.Phase, out var validTargets))
            {
                return false;
            }

            return validTargets.Contains(targetPhase);
        }

        private void TransitionTo(MaskStateData newState)
        {
            var previousState = _currentState;
            _currentState = newState;

            OnStateChanged?.Invoke(this, new MaskStateChangedEventArgs(previousState, newState));
        }

        /// <summary>
        /// Check if mask is currently controlling an NPC
        /// </summary>
        public bool IsPossessing => _currentState.Phase == MaskPhase.Possessed;

        /// <summary>
        /// Check if mask is available for new actions
        /// </summary>
        public bool IsIdle => _currentState.Phase == MaskPhase.Idle;

        /// <summary>
        /// Check if mask is in any active state
        /// </summary>
        public bool IsBusy => _currentState.Phase != MaskPhase.Idle;

        /// <summary>
        /// Get current target NPC id (null if none)
        /// </summary>
        public string CurrentTargetId => _currentState.TargetNpcId;

        #endregion
    }
}
