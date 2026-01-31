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
    }
}
