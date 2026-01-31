using UnityEngine;
using Mask.Domain;
using NPC.Controller;

namespace Mask.Controller
{
    public class MaskController : MonoBehaviour
    {
        [Header("Seduce Settings")]
        [SerializeField] private float lureDistance = 5f;
        [SerializeField] private float seduceSpeed = 2f;
        [SerializeField] private float lureProgressPerSecond = 0.5f;
        [SerializeField] private float attachDistance = 0.5f;

        [Header("Attach Settings")]
        [SerializeField] private Vector3 attachOffset = new Vector3(0, 1.5f, 0.3f);

        public float LureDistance => lureDistance;

        #region Unity Lifecycle

        private void Update()
        {
            // Read state from domain
            MaskPhase currentPhase = MaskDomain.Instance.CurrentState.Phase;

            switch (currentPhase)
            {
                case MaskPhase.Idle:
                    break;

                case MaskPhase.Seducing:
                    UpdateSeducing();
                    break;

                case MaskPhase.Attaching:
                    break;

                case MaskPhase.Possessed:
                    UpdatePossessed();
                    break;

                case MaskPhase.Dropping:
                    break;
            }
        }

        #endregion

        #region State Update Methods

        private void UpdateSeducing()
        {
            // Read target ID from domain
            string targetId = MaskDomain.Instance.CurrentState.TargetNpcId;
            if (string.IsNullOrEmpty(targetId))
            {
                CancelSeduce();
                return;
            }

            // Lookup NPC by ID
            NpcController targetNpc = NpcController.GetById(targetId);
            if (targetNpc == null)
            {
                CancelSeduce();
                return;
            }

            // Move toward NPC
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetNpc.transform.position,
                seduceSpeed * Time.deltaTime
            );

            // Update lure progress
            bool fullyLured = targetNpc.OnLureProgress(lureProgressPerSecond * Time.deltaTime);

            // Check if close enough and fully lured
            float distance = Vector3.Distance(transform.position, targetNpc.transform.position);
            if (distance <= attachDistance && fullyLured)
            {
                AttachToNpc(targetNpc);
            }
        }

        private void UpdatePossessed()
        {
            // Update possession duration
            MaskDomain.Instance.UpdatePossessionTime(Time.deltaTime);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Start seducing a specific NPC
        /// </summary>
        public bool StartSeduce(NpcController npcController)
        {
            // Validate mask is idle
            if (!MaskDomain.Instance.IsIdle)
            {
                Debug.Log("Mask is busy.");
                return false;
            }

            // Validate NPC is seducible
            if (!npcController.IsSeducible())
            {
                Debug.Log($"NPC {npcController.NpcId} is not seducible.");
                return false;
            }

            // Update domain (stores TargetNpcId)
            if (!MaskDomain.Instance.StartSeduce(npcController.NpcId))
            {
                Debug.Log("Domain rejected seduce.");
                return false;
            }

            // Notify NPC
            npcController.OnLureStart();

            Debug.Log($"Mask seducing NPC: {npcController.NpcId}");
            return true;
        }

        /// <summary>
        /// Cancel current seduction
        /// </summary>
        public void CancelSeduce()
        {
            if (MaskDomain.Instance.CurrentState.Phase != MaskPhase.Seducing)
            {
                return;
            }

            // Notify NPC
            string targetId = MaskDomain.Instance.CurrentState.TargetNpcId;
            NpcController targetNpc = NpcController.GetById(targetId);
            if (targetNpc != null)
            {
                targetNpc.OnLureCancelled();
            }

            // Update domain
            MaskDomain.Instance.CancelSeduce();

            Debug.Log("Seduce cancelled.");
        }

        /// <summary>
        /// Drop mask from current NPC
        /// </summary>
        public void Drop()
        {
            if (!MaskDomain.Instance.IsPossessing)
            {
                Debug.Log("Mask is not possessing.");
                return;
            }

            // Get current target before dropping
            string targetId = MaskDomain.Instance.CurrentState.TargetNpcId;
            NpcController targetNpc = NpcController.GetById(targetId);

            // Update domain
            MaskDomain.Instance.StartDrop();

            // Notify NPC
            if (targetNpc != null)
            {
                targetNpc.OnReleased();
            }

            // Detach
            transform.SetParent(null);

            // Enable physics
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }

            // Complete drop
            MaskDomain.Instance.CompleteDrop();

            Debug.Log($"Mask dropped from NPC: {targetId}");
        }

        #endregion

        #region Private Methods

        private void AttachToNpc(NpcController targetNpc)
        {
            // Update domain
            MaskDomain.Instance.BeginAttach();
            MaskDomain.Instance.CompletePossession();

            // Notify NPC
            targetNpc.OnPossessed();

            // Parent to NPC
            transform.SetParent(targetNpc.transform);
            transform.localPosition = attachOffset;

            // Disable physics
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            Debug.Log($"Mask attached to NPC: {targetNpc.NpcId}");
        }

        #endregion

        #region Editor

        private void OnDrawGizmosSelected()
        {
            string targetId = MaskDomain.Instance?.CurrentState.TargetNpcId;
            if (!string.IsNullOrEmpty(targetId))
            {
                NpcController targetNpc = NpcController.GetById(targetId);
                if (targetNpc != null)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(transform.position, targetNpc.transform.position);
                }
            }
        }

        #endregion
    }
}
