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
        private Vector3 lastParentPosition;
        private Vector2 parentVelocity;
        private Rigidbody2D rb;
        private Collider2D maskCollider;
        private Renderer[] maskRenderers;

        #region Unity Lifecycle

        private void Awake()
        {
            originalParent = transform.parent;
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;

            rb = GetComponent<Rigidbody2D>();
            maskCollider = GetComponent<Collider2D>();
            maskRenderers = GetComponentsInChildren<Renderer>(true);
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

        private void LateUpdate()
        {
            if (MaskDomain.Instance.IsPossessing)
            {
                UpdateParentVelocity();
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
                lastParentPosition = targetNpc.transform.position;
                parentVelocity = Vector2.zero;
            }

            transform.localPosition = attachOffset;
        }

        private void UpdateParentVelocity()
        {
            Transform parent = transform.parent;
            if (parent == null) return;

            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f) return;

            Vector3 currentParentPosition = parent.position;
            Vector3 delta = currentParentPosition - lastParentPosition;
            parentVelocity = new Vector2(delta.x / deltaTime, delta.y / deltaTime);
            lastParentPosition = currentParentPosition;
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
            lastParentPosition = npcController.transform.position;
            parentVelocity = Vector2.zero;

            // Disable physics
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
            }
            if (maskCollider != null) maskCollider.enabled = false;
            if (maskRenderers != null)
            {
                for (int i = 0; i < maskRenderers.Length; i++)
                {
                    if (maskRenderers[i] != null)
                    {
                        maskRenderers[i].enabled = false;
                    }
                }
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
            Vector2 inheritedVelocity = rb != null ? rb.linearVelocity : Vector2.zero;
            Debug.Log($"[MaskController] Release self velocity: {inheritedVelocity}");

            if (!MaskDomain.Instance.Release())
            {
                Debug.Log("Domain rejected release.");
                return;
            }

            // Detach from NPC
            transform.SetParent(null);

            // Enable physics and preserve current velocity, then add upward velocity
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.linearVelocity = inheritedVelocity + Vector2.up * releaseUpwardForce;
            }
            if (maskCollider != null) maskCollider.enabled = true;
            if (maskRenderers != null)
            {
                for (int i = 0; i < maskRenderers.Length; i++)
                {
                    if (maskRenderers[i] != null)
                    {
                        maskRenderers[i].enabled = true;
                    }
                }
            }

            Debug.Log($"Mask released NPC: {targetId}");
        }

        #endregion
    }
}
