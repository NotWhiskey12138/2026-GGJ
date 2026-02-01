using UnityEngine;
using DG.Tweening;
using NPCSystem.Abilities;

namespace NPCSystem.WallBat
{
    public class WallBatAbility : NpcAbility
    {
        [Header("Detect")]
        [SerializeField] private string targetTag = "WallBatFood";
        [SerializeField] private LayerMask targetLayer;
        [SerializeField] private float detectRadius = 4f;
        [SerializeField] private float maxHookDistance = 6f;

        [Header("Move")]
        [SerializeField] private float moveDuration = 0.3f;
        [SerializeField] private float reachThreshold = 0.1f;

        private Tween moveTween;
        private Collider2D currentTarget;
        private WallBatController wallBatController;

        protected override void Awake()
        {
            base.Awake();
            wallBatController = GetComponentInParent<WallBatController>();
        }

        private void Update()
        {
            if (!isActive) return;
            if (moveTween != null && moveTween.IsActive()) return;

            Vector2 origin = transform.position;
            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, detectRadius, targetLayer);
            if (hits == null || hits.Length == 0) return;

            Collider2D bestCol = null;
            float bestDist = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D h = hits[i];
                if (h == null) continue;
                if (!h.CompareTag(targetTag)) continue;

                Vector2 p = h.ClosestPoint(origin);
                float dist = Vector2.Distance(origin, p);
                if (dist > maxHookDistance) continue;

                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestCol = h;
                }
            }

            if (bestCol == null) return;

            currentTarget = bestCol;
            Vector2 targetPoint = bestCol.ClosestPoint(origin);

            wallBatController?.SetPatrolSuspended(true);

            moveTween?.Kill();
            moveTween = transform.DOMove(targetPoint, moveDuration)
                .SetEase(Ease.OutSine)
                .OnComplete(() =>
                {
                    if (currentTarget != null)
                    {
                        float dist = Vector2.Distance(transform.position, targetPoint);
                        if (dist <= reachThreshold)
                        {
                            var food = currentTarget.GetComponent<WallBatFood>();
                            if (food != null && wallBatController != null)
                            {
                                wallBatController.SetGravityDirection(food.GravityDirection);
                            }
                            Destroy(currentTarget.gameObject);
                        }
                    }

                    currentTarget = null;
                    wallBatController?.SetPatrolSuspended(false);
                });
        }

        public override void OnPossessedEnd()
        {
            moveTween?.Kill();
            currentTarget = null;
            wallBatController?.SetPatrolSuspended(false);
            base.OnPossessedEnd();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, detectRadius);
        }
    }
}
