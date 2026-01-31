using System;

namespace MaskSystem.Domain
{
    /// <summary>
    /// Simplified domain logic for Mask state management
    /// Only 2 states: Idle and Possessed
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

        public static void ResetInstance()
        {
            lock (_lock)
            {
                _instance = null;
            }
        }

        #endregion

        #region Events

        public event EventHandler<MaskStateChangedEventArgs> OnStateChanged;
        public event EventHandler<string> OnPossessionStarted;
        public event EventHandler<string> OnPossessionEnded;

        #endregion

        #region State

        private MaskStateData _currentState;
        public MaskStateData CurrentState => _currentState;

        public bool IsIdle => _currentState.Phase == MaskPhase.Idle;
        public bool IsPossessing => _currentState.Phase == MaskPhase.Possessed;
        public string CurrentTargetId => _currentState.TargetNpcId;

        #endregion

        #region Constructor

        private MaskDomain()
        {
            _currentState = new MaskStateData(MaskPhase.Idle);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Possess an NPC. Only works when Idle.
        /// </summary>
        public bool Possess(string npcId)
        {
            if (string.IsNullOrEmpty(npcId))
            {
                return false;
            }

            if (!IsIdle)
            {
                return false;
            }

            var newState = new MaskStateData(MaskPhase.Possessed, npcId, 0f);
            TransitionTo(newState);
            OnPossessionStarted?.Invoke(this, npcId);

            return true;
        }

        /// <summary>
        /// Release current NPC. Only works when Possessed.
        /// </summary>
        public bool Release()
        {
            if (!IsPossessing)
            {
                return false;
            }

            string releasedNpcId = _currentState.TargetNpcId;
            var newState = _currentState.Reset();
            TransitionTo(newState);
            OnPossessionEnded?.Invoke(this, releasedNpcId);

            return true;
        }

        /// <summary>
        /// Update possession duration (call from Update loop)
        /// </summary>
        public void UpdatePossessionTime(float deltaTime)
        {
            if (!IsPossessing)
            {
                return;
            }

            _currentState = _currentState.WithDuration(_currentState.PossessionDuration + deltaTime);
        }

        /// <summary>
        /// Force reset to idle (for error recovery)
        /// </summary>
        public void ForceReset()
        {
            string previousNpc = _currentState.TargetNpcId;
            bool wasPossessing = IsPossessing;

            var newState = _currentState.Reset();
            TransitionTo(newState);

            if (wasPossessing && !string.IsNullOrEmpty(previousNpc))
            {
                OnPossessionEnded?.Invoke(this, previousNpc);
            }
        }

        #endregion

        #region Private Methods

        private void TransitionTo(MaskStateData newState)
        {
            var previousState = _currentState;
            _currentState = newState;
            OnStateChanged?.Invoke(this, new MaskStateChangedEventArgs(previousState, newState));
        }

        #endregion
    }
}
