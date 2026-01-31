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

        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 2f;

        [Header("Patrol Settings")]
        [SerializeField] private Vector2 patrolPointA = new Vector2(-3f, 0f);
        [SerializeField] private Vector2 patrolPointB = new Vector2(3f, 0f);
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

            // Always move right for simple testing (even when possessed)
            if (!state.HasValue || state.Value.Phase != NpcPhase.Stunned)
            {
                transform.position += Vector3.right * walkSpeed * Time.deltaTime;
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
            Vector2 pointA = new Vector2(patrol.PointAX, patrol.PointAY);
            Vector2 pointB = new Vector2(patrol.PointBX, patrol.PointBY);

            // Use serialized values if domain has zero values
            if (pointA == Vector2.zero && pointB == Vector2.zero)
            {
                pointA = patrolPointA;
                pointB = patrolPointB;
            }

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

            NpcDomain.Instance.Release(npcId, stunDuration);
            Debug.Log($"[{npcId}] Released, stunned for {stunDuration}s.");
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
            Gizmos.DrawSphere(patrolPointA, 0.2f);
            Gizmos.DrawSphere(patrolPointB, 0.2f);
            Gizmos.DrawLine(patrolPointA, patrolPointB);
        }

        #endregion
    }
}
