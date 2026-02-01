using UnityEngine;
using DG.Tweening;
using NPCSystem.Abilities;

namespace NPCSystem.Bat
{
    public class BatAbility : NpcAbility
    {
        [Header("Detect")]
        [SerializeField] private string targetTag = "BatTarget";
        [SerializeField] private float detectRadius = 4f;
        [SerializeField] private float maxHookDistance = 6f;

        [Header("Move")]
        [SerializeField] private float moveDuration = 0.3f;
        [SerializeField] private float reachThreshold = 0.1f;

        private Tween moveTween;
        private Transform currentTarget;
        private Collider2D currentTargetCollider;

        public bool IsActing { get; private set; }
        public bool IsLatched { get; private set; }

        private void Update()
        {
            if (!isActive) return;
            if (IsLatched) return;
            if (moveTween != null && moveTween.IsActive()) return;

            Vector2 origin = transform.position;
            int platformLayer = LayerMask.GetMask("Platform");
            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, detectRadius, platformLayer);
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

            currentTarget = bestCol.transform;
            currentTargetCollider = bestCol;
            Vector2 targetPoint = bestCol.ClosestPoint(origin);

            moveTween?.Kill();
            IsActing = true;
            moveTween = transform.DOMove(targetPoint, moveDuration)
                .SetEase(Ease.OutSine)
                .OnComplete(() =>
                {
                    if (currentTargetCollider != null)
                    {
                        float dist = Vector2.Distance(transform.position, targetPoint);
                        if (dist <= reachThreshold)
                        {
                            IsLatched = true;
                            transform.position = targetPoint;
                            var rb = GetComponentInParent<Rigidbody2D>();
                            if (rb != null)
                            {
                                rb.linearVelocity = Vector2.zero;
                            }
                        }
                    }
                    IsActing = false;
                    currentTarget = null;
                    currentTargetCollider = null;
                });
        }

        public override void OnPossessedEnd()
        {
            moveTween?.Kill();
            currentTarget = null;
            currentTargetCollider = null;
            IsActing = false;
            IsLatched = false;
            base.OnPossessedEnd();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, detectRadius);
        }
    }
}
