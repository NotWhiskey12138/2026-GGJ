using UnityEngine;
using MaskSystem.Controller;
using MaskSystem.Domain;

namespace MaskSystem
{
    /// <summary>
    /// Main Mask component - attach this to Mask GameObject
    /// </summary>
    [RequireComponent(typeof(MaskController))]
    [RequireComponent(typeof(Collider2D))]
    public class Mask : MonoBehaviour
    {
        private MaskController _controller;
        private Collider2D _collider;

        [Header("Ground Check")]
        [SerializeField] private bool requireAirborne = false;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.12f;
        [SerializeField] private LayerMask groundLayer;

        public MaskController Controller => _controller;
        public MaskPhase CurrentPhase => MaskDomain.Instance.CurrentState.Phase;
        public string TargetNpcId => MaskDomain.Instance.CurrentState.TargetNpcId;
        public bool IsIdle => MaskDomain.Instance.IsIdle;
        public bool IsPossessing => MaskDomain.Instance.IsPossessing;

        private void Awake()
        {
            _controller = GetComponent<MaskController>();
            _collider = GetComponent<Collider2D>();
        }

        /// <summary>
        /// Try to possess an NPC - checks collider overlap first
        /// </summary>
        public bool TryPossessNpc(NPCSystem.NPC npc)
        {
            if (npc == null) return false;

            if (requireAirborne && IsGrounded())
            {
                Debug.Log("Mask is grounded; possession blocked by ground check.");
                return false;
            }

            Collider2D npcCollider = npc.GetComponent<Collider2D>();
            if (npcCollider == null)
            {
                Debug.Log($"NPC {npc.NpcId} has no Collider2D.");
                return false;
            }

            bool isOverlapping = _collider.IsTouching(npcCollider);
            if (!isOverlapping)
            {
                Debug.Log($"NPC {npc.NpcId} is not touching Mask collider.");
                return false;
            }

            Debug.Log($"Possessing {npc.NpcId}");
            return _controller.Possess(npc.Controller);
        }

        /// <summary>
        /// Release current NPC
        /// </summary>
        public void Release()
        {
            _controller.Release();
        }

        public void ResetToSpawn()
        {
            _controller.ResetToSpawn();
        }

        private bool IsGrounded()
        {
            if (groundCheck == null) return false;
            return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer) != null;
        }
    }
}
