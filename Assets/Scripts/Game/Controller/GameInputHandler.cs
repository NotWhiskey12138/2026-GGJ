using UnityEngine;
using MaskSystem;
using MaskSystem.Domain;
using NPCSystem;
using NPCSystem.Controller;
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

        [Header("Slow Motion")]
        [SerializeField] private bool slowMoOnRelease = true;
        [SerializeField] private float slowMoScale = 0.2f;
        [SerializeField] private float slowMoDuration = 0.25f;

        private float defaultFixedDeltaTime;
        private Coroutine slowMoRoutine;

        private void Awake()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            defaultFixedDeltaTime = Time.fixedDeltaTime;
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
                    if (slowMoOnRelease)
                    {
                        TriggerSlowMo();
                    }
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
            Debug.Log($"Tap world position: {worldPosition}");

            // 2D check at tap position
            Collider2D hit = Physics2D.OverlapPoint(worldPosition);

            if (hit != null)
            {
                Debug.Log($"Tap hit: {hit.name}");
                if (mask != null && mask.IsPossessing)
                {
                    string targetNpcId = MaskDomain.Instance.CurrentTargetId;
                    NpcController controller = NpcController.GetById(targetNpcId);
                    if (controller != null)
                    {
                        Debug.Log($"Forwarding tap to possessed NPC: {targetNpcId}");
                        controller.HandlePossessedClick(hit.gameObject);
                    }
                    else
                    {
                        Debug.LogWarning("No possessed NPC controller found.");
                    }
                    return;
                }

                // Check if hit object has NPC
                NPC npc = hit.GetComponent<NPC>();
                if (npc != null)
                {
                    Debug.Log($"Tap gets npc {npc.NpcId}");
                    AttachMasktoNPC(npc);
                }
                else
                {
                    Debug.Log("Tap hit has no NPC component.");
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

        private void TriggerSlowMo()
        {
            if (slowMoRoutine != null)
            {
                StopCoroutine(slowMoRoutine);
            }
            slowMoRoutine = StartCoroutine(SlowMoCoroutine());
        }

        private System.Collections.IEnumerator SlowMoCoroutine()
        {
            Time.timeScale = slowMoScale;
            Time.fixedDeltaTime = defaultFixedDeltaTime * slowMoScale;

            float timer = 0f;
            while (timer < slowMoDuration)
            {
                timer += Time.unscaledDeltaTime;
                yield return null;
            }

            Time.timeScale = 1f;
            Time.fixedDeltaTime = defaultFixedDeltaTime;
            slowMoRoutine = null;
        }
    }
}
