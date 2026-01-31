using UnityEngine;

namespace Game.Controller
{
    /// <summary>
    /// Handles player input for tapping on NPCs
    /// Attach this to a GameObject in the scene (e.g., GameManager)
    /// </summary>
    public class GameInputHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Mask.Mask mask;
        [SerializeField] private Camera mainCamera;

        [Header("Input Settings")]
        [SerializeField] private LayerMask npcLayer;

        private void Awake()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }

        private void Update()
        {
            // Handle tap/click input
            if (Input.GetMouseButtonDown(0))
            {
                HandleTap(Input.mousePosition);
            }

            // Handle touch input (mobile)
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    HandleTap(touch.position);
                }
            }
        }

        private void HandleTap(Vector2 screenPosition)
        {
            if (mask == null)
            {
                Debug.LogWarning("Mask not assigned to GameInputHandler.");
                return;
            }

            if (mainCamera == null)
            {
                Debug.LogWarning("Camera not assigned to GameInputHandler.");
                return;
            }

            // Raycast from camera through tap position
            Ray ray = mainCamera.ScreenPointToRay(screenPosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, npcLayer))
            {
                // Check if hit object has NPC
                NPC.NPC npc = hit.collider.GetComponent<NPC.NPC>();

                if (npc != null)
                {
                    TryTargetNpc(npc);
                }
            }
        }

        private void TryTargetNpc(NPC.NPC npc)
        {
            // Check distance between Mask and NPC
            float distance = Vector3.Distance(mask.transform.position, npc.transform.position);
            if (distance > mask.LureDistance)
            {
                Debug.Log($"NPC {npc.NpcId} is too far. Distance: {distance}, LureDistance: {mask.LureDistance}");
                return;
            }

            // Start seduce
            bool success = mask.StartSeduce(npc);

            if (success)
            {
                Debug.Log($"Successfully started seducing NPC: {npc.NpcId}");
            }
        }
    }
}
