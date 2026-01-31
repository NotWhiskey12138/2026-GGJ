using UnityEngine;
using NPC.Controller;
using NPC.Domain;

namespace NPC
{
    /// <summary>
    /// Main NPC component - attach this to NPC GameObject
    /// </summary>
    [RequireComponent(typeof(NpcController))]
    public class NPC : MonoBehaviour
    {
        private NpcController _controller;

        public NpcController Controller => _controller;
        public string NpcId => _controller.NpcId;
        public NpcPhase CurrentPhase
        {
            get
            {
                var state = NpcDomain.Instance.GetState(_controller.NpcId);
                return state?.Phase ?? NpcPhase.Idle;
            }
        }
        public bool IsSeducible => _controller.IsSeducible();
        public bool IsPossessed => CurrentPhase == NpcPhase.Possessed;

        private void Awake()
        {
            _controller = GetComponent<NpcController>();
        }
    }
}
