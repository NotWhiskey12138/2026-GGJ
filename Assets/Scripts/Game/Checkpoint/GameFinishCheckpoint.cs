using UnityEngine;
using MaskSystem;

namespace Game.Checkpoint
{
    [RequireComponent(typeof(Collider2D))]
    public class GameFinishCheckpoint : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject gameFinishPanel;

        [Header("Options")]
        [SerializeField] private bool deactivateOnTrigger = true;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null) return;

            var mask = other.GetComponentInParent<Mask>();
            if (mask == null) return;

            if (gameFinishPanel != null)
            {
                gameFinishPanel.SetActive(true);
            }

            if (deactivateOnTrigger)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
