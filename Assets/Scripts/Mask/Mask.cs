using UnityEngine;
using Mask.Controller;
using Mask.Domain;

namespace Mask
{
    /// <summary>
    /// Main Mask component - attach this to Mask GameObject
    /// </summary>
    [RequireComponent(typeof(MaskController))]
    public class Mask : MonoBehaviour
    {
        private MaskController _controller;

        public MaskController Controller => _controller;
        public MaskPhase CurrentPhase => MaskDomain.Instance.CurrentState.Phase;
        public string TargetNpcId => MaskDomain.Instance.CurrentState.TargetNpcId;
        public bool IsIdle => MaskDomain.Instance.IsIdle;
        public bool IsPossessing => MaskDomain.Instance.IsPossessing;
        public float LureDistance => _controller.LureDistance;

        private void Awake()
        {
            _controller = GetComponent<MaskController>();
        }

        public bool StartSeduce(NPC.NPC npc)
        {
            if (npc == null) return false;
            return _controller.StartSeduce(npc.Controller);
        }

        public void CancelSeduce()
        {
            _controller.CancelSeduce();
        }

        public void Drop()
        {
            _controller.Drop();
        }
    }
}
