using UnityEngine;
using MaskSystem.Domain;
using NPCSystem.Controller;

namespace MaskSystem.Controller
{
    public class MaskController : MonoBehaviour
    {
        [Header("Attach Settings")]
        [SerializeField] private Vector3 attachOffset = new Vector3(0, 1.5f, 0.3f);

        [Header("Release Settings")]
        [SerializeField] private float releaseUpwardForce = 5f;

        #region Unity Lifecycle

        private void Update()
        {
            if (MaskDomain.Instance.IsPossessing)
            {
                FollowPossessedTarget();
                MaskDomain.Instance.UpdatePossessionTime(Time.deltaTime);
            }
        }

        #endregion

        private void FollowPossessedTarget()
        {
            string targetId = MaskDomain.Instance.CurrentTargetId;
            if (string.IsNullOrEmpty(targetId))
            {
                return;
            }

            NpcController targetNpc = NpcController.GetById(targetId);
            if (targetNpc == null)
            {
                return;
            }

            if (transform.parent != targetNpc.transform)
            {
                transform.SetParent(targetNpc.transform);
            }

            transform.localPosition = attachOffset;
        }

        #region Public Methods

        /// <summary>
        /// Possess an NPC immediately
        /// </summary>
        public bool Possess(NpcController npcController)
        {
            if (npcController == null)
            {
                Debug.Log("No NPC to possess.");
                return false;
            }

            if (!MaskDomain.Instance.IsIdle)
            {
                Debug.Log($"Mask is already possessing: {MaskDomain.Instance.CurrentTargetId}");
                return false;
            }

            if (!MaskDomain.Instance.Possess(npcController.NpcId))
            {
                Debug.Log("Domain rejected possession.");
                return false;
            }

            // Attach to NPC
            transform.SetParent(npcController.transform);
            transform.localPosition = attachOffset;

            // Disable physics
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
            }

            Debug.Log($"Mask possessed NPC: {npcController.NpcId}");
            return true;
        }

        /// <summary>
        /// Release current NPC
        /// </summary>
        public void Release()
        {
            if (!MaskDomain.Instance.IsPossessing)
            {
                Debug.Log("Mask is not possessing anyone.");
                return;
            }

            string targetId = MaskDomain.Instance.CurrentTargetId;

            if (!MaskDomain.Instance.Release())
            {
                Debug.Log("Domain rejected release.");
                return;
            }

            // Detach from NPC
            transform.SetParent(null);

            // Enable physics and apply upward force
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.linearVelocity = Vector2.up * releaseUpwardForce;
            }

            Debug.Log($"Mask released NPC: {targetId}");
        }

        #endregion
    }
}
