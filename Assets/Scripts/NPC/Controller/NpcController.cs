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

        public string NpcId => npcId;
        public bool CanBePossessed => canBePossessed;
        public NpcStateData State => domain != null
            ? domain.GetState()
            : new NpcStateData(npcId, NpcPhase.Idle, 0f, canBePossessed);

        protected NpcDomain domain;
        protected Rigidbody2D rb;
        private Vector2 possessedTargetPosition;

        private void Awake()
        {
            if (string.IsNullOrEmpty(npcId))
            {
                npcId = $"npc_{GetInstanceID()}";
            }

            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                Debug.LogWarning($"[{npcId}] Missing Rigidbody2D. Movement will use transform.");
            }
        }

        protected virtual void OnEnable()
        {
            _registry[npcId] = this;

            domain = CreateDomain(transform.position);
            domain.OnStateChanged += HandleStateChanged;
            MaskDomain.Instance.OnPossessionStarted += HandlePossessionStarted;
            MaskDomain.Instance.OnPossessionEnded += HandlePossessionEnded;
        }

        protected virtual void OnDisable()
        {
            if (domain != null)
            {
                domain.OnStateChanged -= HandleStateChanged;
            }
            MaskDomain.Instance.OnPossessionStarted -= HandlePossessionStarted;
            MaskDomain.Instance.OnPossessionEnded -= HandlePossessionEnded;

            _registry.Remove(npcId);
        }

        protected virtual void FixedUpdate()
        {
            if (domain == null) return;
            var state = domain.GetState();
            if (ShouldPatrol(state.Phase))
            {
                UpdateMovement();
            }
            else if (state.Phase == NpcPhase.Stunned)
            {
                domain.UpdateStun(Time.deltaTime);
            }
        }

        protected virtual void UpdateMovement()
        {
            if (domain == null) return;
            Vector2 newPosition = IdleAction();
            MoveTo(newPosition);
        }

        public bool IsSeducible()
        {
            return domain != null && domain.GetState().IsSeducible;
        }

        protected virtual NpcDomain CreateDomain(Vector2 startPoint)
        {
            return new NpcDomain(new NpcStateData(npcId, NpcPhase.Idle, 0f, canBePossessed, default, startPoint));
        }

        public virtual void HandlePossessedClick(GameObject target)
        {
            if (target == null) return;
            if (domain == null) return;

            if (domain.GetState().Phase != NpcPhase.Possessed) return;

            if (!IsValidPossessedTarget(target)) return;

            possessedTargetPosition = target.transform.position;
            Vector2 newPosition = PossessedAction();
            MoveTo(newPosition);
        }

        protected virtual bool IsValidPossessedTarget(GameObject target)
        {
            return true;
        }

        protected virtual Vector2 IdleAction()
        {
            return GetCurrentPosition();
        }

        protected virtual Vector2 PossessedAction()
        {
            return domain.PossessedAction(possessedTargetPosition);
        }

        protected virtual bool ShouldPatrol(NpcPhase phase)
        {
            return phase == NpcPhase.Idle;
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

        protected virtual void HandlePossessionStarted(object sender, string targetNpcId)
        {
            if (targetNpcId != npcId) return;
            if (domain == null) return;

            if (!domain.GetState().IsSeducible) return;
            domain.Possess();
            Debug.Log($"[{npcId}] Possessed by mask.");
        }

        protected virtual void HandlePossessionEnded(object sender, string targetNpcId)
        {
            if (targetNpcId != npcId) return;
            if (domain == null) return;

            if (stunOnRelease)
            {
                domain.Release(stunDuration);
                Debug.Log($"[{npcId}] Released, stunned for {stunDuration}s.");
            }
            else
            {
                domain.Release(0f);
                Debug.Log($"[{npcId}] Released, no stun.");
            }
        }

        #endregion

        protected Vector2 GetCurrentPosition()
        {
            return rb != null ? rb.position : (Vector2)transform.position;
        }

        protected void MoveTo(Vector2 position)
        {
            if (rb != null)
            {
                if (rb.bodyType != RigidbodyType2D.Dynamic)
                {
                    rb.bodyType = RigidbodyType2D.Dynamic;
                }
                float dt = Time.fixedDeltaTime;
                if (dt > 0f)
                {
                    rb.linearVelocity = (position - rb.position) / dt;
                }
            }
            else
            {
                transform.position = position;
            }
        }

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

        protected virtual void OnDrawGizmosSelected()
        {
            if (Application.isPlaying && domain != null)
            {
                switch (domain.GetState().Phase)
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
            Gizmos.DrawSphere(transform.position, 0.2f);
        }

        #endregion
    }
}
