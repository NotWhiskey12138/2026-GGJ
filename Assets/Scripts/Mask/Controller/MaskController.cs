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

        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private Transform originalParent;
        private RigidbodyType2D originalBodyType;

        #region Unity Lifecycle

        private void Awake()
        {
            originalParent = transform.parent;
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;

            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                originalBodyType = rb.bodyType;
            }
        }

        private void Update()
        {
            if (MaskDomain.Instance.IsPossessing)
            {
                FollowPossessedTarget();
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

        public void ResetToSpawn()
        {
            MaskDomain.Instance.ForceReset();

            transform.SetParent(originalParent);
            transform.SetPositionAndRotation(spawnPosition, spawnRotation);

            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = originalBodyType;
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }

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
            Rigidbody2D parentRb = transform.parent != null ? transform.parent.GetComponent<Rigidbody2D>() : null;
            Vector2 inheritedVelocity = parentRb != null ? parentRb.linearVelocity : Vector2.zero;

            if (!MaskDomain.Instance.Release())
            {
                Debug.Log("Domain rejected release.");
                return;
            }

            // Detach from NPC
            transform.SetParent(null);

            // Enable physics and preserve current velocity, then add upward velocity
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.linearVelocity = inheritedVelocity + Vector2.up * releaseUpwardForce;
            }

            Debug.Log($"Mask released NPC: {targetId}");
        }

        #endregion
    }
}
