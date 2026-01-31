using UnityEngine;
using MaskSystem;
using NPCSystem;
namespace Game.Controller
{
    /// <summary>
    /// Handles player input for tapping on NPCs
    /// Attach this to a GameObject in the scene (e.g., GameManager)
    /// </summary>
    public class GameInputHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Mask mask;
        [SerializeField] private Camera mainCamera;

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

            // Handle space key to release possession
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (mask != null && mask.IsPossessing)
                {
                    mask.Release();
                    Debug.Log("Released possession with Space key.");
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

            // Convert screen position to world position for 2D
            Vector2 worldPosition = mainCamera.ScreenToWorldPoint(screenPosition);

            // 2D check at tap position
            Collider2D hit = Physics2D.OverlapPoint(worldPosition);

            if (hit != null)
            {
                // Check if hit object has NPC
                NPC npc = hit.GetComponent<NPC>();

                if (npc != null)
                {
                    Debug.Log($"Tap gets npc {npc.NpcId}");
                    AttachMasktoNPC(npc);
                }
            }
            else
            {
                Debug.Log("No hit");
            }
        }

        private void AttachMasktoNPC(NPC npc)
        {
            bool success = mask.TryPossessNpc(npc);

            if (success)
            {
                Debug.Log($"Successfully possessed NPC: {npc.NpcId}");
            }
        }
    }
}
