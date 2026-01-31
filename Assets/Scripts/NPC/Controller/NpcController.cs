using UnityEngine;
using System.Collections.Generic;
using NPCSystem.Domain;
using MaskSystem.Domain;

namespace NPCSystem.Controller
{
    public class NpcController : MonoBehaviour
    {
        #region Static Registry

        private static Dictionary<string, NpcController> _registry = new Dictionary<string, NpcController>();

        public static NpcController GetById(string npcId)
        {
            if (string.IsNullOrEmpty(npcId)) return null;
            _registry.TryGetValue(npcId, out var controller);
            return controller;
        }

        #endregion

        [Header("NPC Settings")]
        [SerializeField] private string npcId;
        [SerializeField] private bool canBePossessed = true;
        [SerializeField] private float stunDuration = 2f;
        [SerializeField] private bool stunOnRelease = true;

        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 2f;

        [Header("Patrol Settings")]
        [SerializeField] private Transform patrolPointAObj;
        [SerializeField] private Transform patrolPointBObj;
        [SerializeField] private float reachThreshold = 0.1f;

        public string NpcId => npcId;
        public bool CanBePossessed => canBePossessed;

        private void Awake()
        {
            if (string.IsNullOrEmpty(npcId))
            {
                npcId = $"npc_{GetInstanceID()}";
            }
        }

        private void OnEnable()
        {
            _registry[npcId] = this;

            NpcDomain.Instance.RegisterNpc(npcId, canBePossessed);

            NpcDomain.Instance.OnStateChanged += HandleStateChanged;
            MaskDomain.Instance.OnPossessionStarted += HandlePossessionStarted;
            MaskDomain.Instance.OnPossessionEnded += HandlePossessionEnded;

            Vector2 startPoint = GetPatrolPointA();
            transform.position = startPoint;
        }

        private void OnDisable()
        {
            NpcDomain.Instance.OnStateChanged -= HandleStateChanged;
            MaskDomain.Instance.OnPossessionStarted -= HandlePossessionStarted;
            MaskDomain.Instance.OnPossessionEnded -= HandlePossessionEnded;

            NpcDomain.Instance.UnregisterNpc(npcId);
            _registry.Remove(npcId);
        }

        private void Update()
        {
            var state = NpcDomain.Instance.GetState(npcId);

            if (!state.HasValue || state.Value.Phase != NpcPhase.Stunned)
            {
                UpdateMovement();
            }
            else if (state.Value.Phase == NpcPhase.Stunned)
            {
                NpcDomain.Instance.UpdateStun(npcId, Time.deltaTime);
            }
        }

        private void UpdateMovement()
        {
            var state = NpcDomain.Instance.GetState(npcId);
            if (!state.HasValue)
            {
                Debug.Log($"[{npcId}] No state found");
                return;
            }

            var patrol = state.Value.Patrol;
            Vector2 pointA = GetPatrolPointA();
            Vector2 pointB = GetPatrolPointB();

            Vector2 targetPoint = patrol.MovingToB ? pointB : pointA;

            transform.position = Vector2.MoveTowards(
                transform.position,
                targetPoint,
                walkSpeed * Time.deltaTime
            );

            float distance = Vector2.Distance(transform.position, targetPoint);
            if (distance <= reachThreshold)
            {
                NpcDomain.Instance.SetPatrolDirection(npcId, !patrol.MovingToB);
            }
        }

        private Vector2 GetPatrolPointA()
        {
            return patrolPointAObj != null ? (Vector2)patrolPointAObj.position : transform.position;
        }

        private Vector2 GetPatrolPointB()
        {
            return patrolPointBObj != null ? (Vector2)patrolPointBObj.position : transform.position;
        }

        public bool IsSeducible()
        {
            var state = NpcDomain.Instance.GetState(npcId);
            if (!state.HasValue) return false;
            return state.Value.IsSeducible;
        }

        #region Event Handlers

        private void HandleStateChanged(object sender, NpcStateChangedEventArgs e)
        {
            if (e.NpcId != npcId) return;

            Debug.Log($"[{npcId}] State: {e.FromPhase} → {e.ToPhase}");

            if (e.FromPhase == NpcPhase.Stunned && e.ToPhase == NpcPhase.Idle)
            {
                Debug.Log($"[{npcId}] Recovered from stun.");
            }
        }

        private void HandlePossessionStarted(object sender, string targetNpcId)
        {
            if (targetNpcId != npcId) return;

            NpcDomain.Instance.Possess(npcId);
            Debug.Log($"[{npcId}] Possessed by mask.");
        }

        private void HandlePossessionEnded(object sender, string targetNpcId)
        {
            if (targetNpcId != npcId) return;

            if (stunOnRelease)
            {
                NpcDomain.Instance.Release(npcId, stunDuration);
                Debug.Log($"[{npcId}] Released, stunned for {stunDuration}s.");
            }
            else
            {
                NpcDomain.Instance.Release(npcId, 0f);
                Debug.Log($"[{npcId}] Released, no stun.");
            }
        }

        #endregion

        #region Input Detection

        private void OnMouseDown()
        {
            var mask = FindObjectOfType<MaskSystem.Mask>();
            if (mask != null)
            {
                mask.TryPossessNpc(GetComponent<NPCSystem.NPC>());
            }
        }

        #endregion

        #region Editor

        private void OnDrawGizmosSelected()
        {
            var state = Application.isPlaying ? NpcDomain.Instance.GetState(npcId) : null;

            if (state.HasValue)
            {
                switch (state.Value.Phase)
                {
                    case NpcPhase.Idle:
                        Gizmos.color = Color.green;
                        break;
                    case NpcPhase.Possessed:
                        Gizmos.color = Color.red;
                        break;
                    case NpcPhase.Stunned:
                        Gizmos.color = Color.gray;
                        break;
                }
            }
            else
            {
                Gizmos.color = Color.white;
            }

            Gizmos.DrawWireSphere(transform.position, 0.5f);

            Gizmos.color = Color.cyan;
            Vector2 pointA = GetPatrolPointA();
            Vector2 pointB = GetPatrolPointB();
            Gizmos.DrawSphere(pointA, 0.2f);
            Gizmos.DrawSphere(pointB, 0.2f);
            Gizmos.DrawLine(pointA, pointB);
        }

        #endregion
    }
}
