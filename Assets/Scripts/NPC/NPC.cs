using UnityEngine;
using NPCSystem.Controller;
using NPCSystem.Domain;

namespace NPCSystem
{
    /// <summary>
    /// Main NPC component - attach this to NPC GameObject
    /// </summary>
    [RequireComponent(typeof(NpcController))]
    public class NPC : MonoBehaviour
    {
        [Header("NPC Settings")]
        [SerializeField] private bool canBePossessed = true;

        private NpcController _controller;

        public NpcController Controller => _controller;
        public string NpcId => _controller.NpcId;
        public bool CanBePossessed => canBePossessed;

        public NpcPhase CurrentPhase
        {
            get
            {
                var state = NpcDomain.Instance.GetState(_controller.NpcId);
                return state?.Phase ?? NpcPhase.Idle;
            }
        }

        public bool IsSeducible => canBePossessed && CurrentPhase == NpcPhase.Idle;
        public bool IsPossessed => CurrentPhase == NpcPhase.Possessed;

        private void Awake()
        {
            _controller = GetComponent<NpcController>();
        }
    }
}
