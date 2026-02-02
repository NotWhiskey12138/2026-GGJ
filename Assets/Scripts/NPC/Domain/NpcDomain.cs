using System;

namespace NPCSystem.Domain
{
    /// <summary>
    /// Simplified domain logic for NPC state management
    /// Only 3 states: Idle, Possessed, Stunned
    /// </summary>
    public class NpcDomain
    {
        #region Events

        public event EventHandler<NpcStateChangedEventArgs> OnStateChanged;
        public event EventHandler<string> OnPossessed;
        public event EventHandler<string> OnReleased;
        public event EventHandler<string> OnStunEnded;

        #endregion

        #region State

        private NpcStateData _state;

        #endregion

        public NpcDomain(NpcStateData initialState)
        {
            _state = initialState;
        }

        public NpcStateData GetState()
        {
            return _state;
        }

        public void SetPosition(UnityEngine.Vector2 position)
        {
            _state = _state.WithPosition(position);
        }

        public void SetPatrolDirection(bool movingToB)
        {
            _state = _state.WithPatrolDirection(movingToB);
        }

        private void SetState(NpcStateData newState)
        {
            NpcStateData previous = _state;
            _state = newState;
            OnStateChanged?.Invoke(this, new NpcStateChangedEventArgs(_state.NpcId, previous, newState));
        }

        public virtual UnityEngine.Vector2 IdleAction(
            UnityEngine.Vector2 currentPosition,
            UnityEngine.Vector2 pointA,
            UnityEngine.Vector2 pointB,
            float walkSpeed,
            float deltaTime,
            float reachThreshold)
        {
            if (_state.Phase != NpcPhase.Idle) return currentPosition;

            var patrol = _state.Patrol;
            UnityEngine.Vector2 targetPoint = patrol.MovingToB ? pointB : pointA;

            UnityEngine.Vector2 newPosition = UnityEngine.Vector2.MoveTowards(
                currentPosition,
                targetPoint,
                walkSpeed * deltaTime
            );

            float distance = UnityEngine.Vector2.Distance(newPosition, targetPoint);
            if (distance <= reachThreshold)
            {
                _state = _state.WithPatrolDirection(!patrol.MovingToB);
            }

            _state = _state.WithPosition(newPosition);
            return newPosition;
        }

        public virtual UnityEngine.Vector2 PossessedAction(UnityEngine.Vector2 targetPosition)
        {
            if (_state.Phase != NpcPhase.Possessed) return _state.Position;

            UnityEngine.Debug.Log($"[NpcDomain] PossessedAction: {_state.NpcId}");
            _state = _state.WithPosition(targetPosition);
            return targetPosition;
        }

        #region State Transition Methods

        /// <summary>
        /// Possess an NPC. Only works when Idle.
        /// </summary>
        public bool Possess()
        {
            if (!_state.IsSeducible) return false;

            var newState = _state.WithPhase(NpcPhase.Possessed);
            SetState(newState);
            OnPossessed?.Invoke(this, _state.NpcId);

            return true;
        }

        /// <summary>
        /// Release an NPC from possession. Enters stunned state.
        /// </summary>
        public bool Release(float stunDuration = 2f)
        {
            if (_state.Phase != NpcPhase.Possessed) return false;

            var newState = _state
                .WithPhase(NpcPhase.Stunned)
                .WithStunRemaining(stunDuration);

            SetState(newState);
            OnReleased?.Invoke(this, _state.NpcId);

            return true;
        }

        /// <summary>
        /// Update stun timer. Call from Update loop.
        /// </summary>
        public bool UpdateStun(float deltaTime)
        {
            if (_state.Phase != NpcPhase.Stunned) return false;

            float newStunTime = _state.StunRemaining - deltaTime;

            if (newStunTime <= 0f)
            {
                var newState = _state.Reset();
                SetState(newState);
                OnStunEnded?.Invoke(this, _state.NpcId);
                return true;
            }

            _state = _state.WithStunRemaining(newStunTime);
            return false;
        }

        /// <summary>
        /// Force reset to idle
        /// </summary>
        public void ForceReset()
        {
            var newState = _state.Reset();
            SetState(newState);
        }

        #endregion
    }
}
