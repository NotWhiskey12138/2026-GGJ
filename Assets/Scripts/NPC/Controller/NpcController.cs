using UnityEngine;
using System.Collections.Generic;
using NPC.Domain;

namespace NPC.Controller
{
    public class NpcController : MonoBehaviour
    {
        #region Static Registry

        private static Dictionary<string, NpcController> _registry = new Dictionary<string, NpcController>();

        /// <summary>
        /// Find NpcController by ID
        /// </summary>
        public static NpcController GetById(string npcId)
        {
            if (string.IsNullOrEmpty(npcId)) return null;
            _registry.TryGetValue(npcId, out var controller);
            return controller;
        }

        #endregion

        [Header("NPC Settings")]
        [SerializeField] private string npcId;
        [SerializeField] private float seduceResistance = 0.2f;
        [SerializeField] private bool canBePossessed = true;
        [SerializeField] private float stunDuration = 2f;

        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 2f;
        [SerializeField] private Vector3 walkDirection = Vector3.right;

        [Header("References")]
        // TODO: Add reference to NpcView for visual effects

        public string NpcId => npcId;
        public bool CanBePossessed => canBePossessed;
        public float SeduceResistance => seduceResistance;

        private void Awake()
        {
            // Generate ID if not set
            if (string.IsNullOrEmpty(npcId))
            {
                npcId = $"npc_{GetInstanceID()}";
            }
        }

        private void OnEnable()
        {
            // Register to static registry
            _registry[npcId] = this;

            // Register to domain
            NpcDomain.Instance.RegisterNpc(npcId, seduceResistance, canBePossessed);

            // Subscribe to domain events
            NpcDomain.Instance.OnStateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            NpcDomain.Instance.OnStateChanged -= HandleStateChanged;

            // Unregister from domain
            NpcDomain.Instance.UnregisterNpc(npcId);

            // Unregister from static registry
            _registry.Remove(npcId);
        }

        private void Update()
        {
            var state = NpcDomain.Instance.GetState(npcId);
            if (!state.HasValue) return;

            switch (state.Value.Phase)
            {
                case NpcPhase.Idle:
                    // Walk in direction when idle
                    UpdateMovement();
                    break;

                case NpcPhase.Stunned:
                    // Update stun timer
                    NpcDomain.Instance.UpdateStun(npcId, Time.deltaTime);
                    break;

                case NpcPhase.Lured:
                case NpcPhase.Possessed:
                    // No movement - controlled by mask or waiting
                    break;
            }
        }

        private void UpdateMovement()
        {
            // Move in walk direction
            transform.position += walkDirection.normalized * walkSpeed * Time.deltaTime;

            // TODO: Add animation
            // TODO: Add boundary check / patrol logic
        }

        /// <summary>
        /// Check if this NPC can be seduced right now
        /// </summary>
        public bool IsSeducible()
        {
            var state = NpcDomain.Instance.GetState(npcId);
            if (!state.HasValue) return false;

            return state.Value.IsSeducible;
        }

        #region Public Methods - Called by MaskController

        /// <summary>
        /// Called when mask starts seducing this NPC
        /// </summary>
        public void OnLureStart()
        {
            if (!NpcDomain.Instance.StartLure(npcId))
            {
                Debug.Log($"[{npcId}] Cannot be lured.");
                return;
            }

            // TODO: Stop current AI behavior
            // TODO: Turn to face the mask
            // TODO: Play lured animation

            Debug.Log($"[{npcId}] Being lured by mask...");
        }

        /// <summary>
        /// Called during lure process
        /// </summary>
        /// <param name="deltaProgress">Progress increment from mask</param>
        /// <returns>True if fully lured</returns>
        public bool OnLureProgress(float deltaProgress)
        {
            bool fullyLured = NpcDomain.Instance.UpdateLureProgress(npcId, deltaProgress);

            if (fullyLured)
            {
                Debug.Log($"[{npcId}] Fully lured!");
                // TODO: Play success effect
            }

            return fullyLured;
        }

        /// <summary>
        /// Called when lure is cancelled
        /// </summary>
        public void OnLureCancelled()
        {
            NpcDomain.Instance.CancelLure(npcId);

            // TODO: Resume AI behavior
            // TODO: Play confused animation

            Debug.Log($"[{npcId}] Lure cancelled, resuming normal behavior.");
        }

        /// <summary>
        /// Called when mask attaches and takes control
        /// </summary>
        public void OnPossessed()
        {
            if (!NpcDomain.Instance.Possess(npcId))
            {
                Debug.Log($"[{npcId}] Failed to possess.");
                return;
            }

            // TODO: Disable AI completely
            // TODO: Enable player input for this NPC
            // TODO: Play possession effect
            // TODO: Change NPC appearance (glowing eyes, etc.)

            Debug.Log($"[{npcId}] Now under mask control!");
        }

        /// <summary>
        /// Called when mask drops and releases control
        /// </summary>
        public void OnReleased()
        {
            if (!NpcDomain.Instance.Release(npcId, stunDuration))
            {
                Debug.Log($"[{npcId}] Failed to release.");
                return;
            }

            // TODO: Disable player input
            // TODO: Play stun animation
            // TODO: Restore normal appearance

            Debug.Log($"[{npcId}] Released from possession, stunned for {stunDuration}s.");
        }

        #endregion

        #region Event Handlers

        private void HandleStateChanged(object sender, NpcStateChangedEventArgs e)
        {
            // Only handle events for this NPC
            if (e.NpcId != npcId) return;

            Debug.Log($"[{npcId}] State: {e.FromPhase} → {e.ToPhase}");

            // Handle stun ended
            if (e.FromPhase == NpcPhase.Stunned && e.ToPhase == NpcPhase.Idle)
            {
                OnStunEnded();
            }
        }

        private void OnStunEnded()
        {
            // TODO: Resume AI behavior
            // TODO: Play recovery animation

            Debug.Log($"[{npcId}] Recovered from stun, resuming normal behavior.");
        }

        #endregion

        #region Input Detection

        /// <summary>
        /// Called when player clicks/taps on this NPC
        /// Requires Collider on this GameObject
        /// </summary>
        private void OnMouseDown()
        {
            // Find mask and try to seduce this NPC
            var maskController = FindObjectOfType<Mask.Controller.MaskController>();
            if (maskController != null)
            {
                maskController.StartSeduce(this);
            }
        }

        #endregion

        #region Editor

        private void OnDrawGizmosSelected()
        {
            // Visualize NPC state in editor
            var state = Application.isPlaying ? NpcDomain.Instance.GetState(npcId) : null;

            if (state.HasValue)
            {
                switch (state.Value.Phase)
                {
                    case NpcPhase.Idle:
                        Gizmos.color = Color.green;
                        break;
                    case NpcPhase.Lured:
                        Gizmos.color = Color.yellow;
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
        }

        #endregion
    }
}
