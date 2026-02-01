using UnityEngine;
using NPCSystem.Abilities;

namespace NPCSystem.Frog
{
    public class FrogAbility : NpcAbility
    {
        [Header("Refs")]
        [SerializeField] private FrogTonguePullImpulse tongue;
        [SerializeField] private Transform mouth;

        [Header("Detect")]
        [SerializeField] private LayerMask targetLayer;
        [SerializeField] private string targetTag = "FrogTarget";
        [SerializeField] private float detectRadius = 4f;
        [SerializeField] private float maxHookDistance = 5f;
        [SerializeField] private float forwardBias = 0.8f;

        [Header("Behavior")]
        [SerializeField] private bool disableAutoScanOnPossess = true;
        [SerializeField] private float pauseBeforePull = 1.0f;

        private bool isWaiting;
        private Coroutine waitRoutine;
        public bool IsActing { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            if (tongue == null)
            {
                tongue = GetComponentInParent<FrogTonguePullImpulse>();
            }
            if (tongue != null)
            {
                tongue.enabled = false;
            }
            if (mouth == null && tongue != null)
            {
                var mouthField = typeof(FrogTonguePullImpulse).GetField("mouth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (mouthField != null)
                {
                    mouth = mouthField.GetValue(tongue) as Transform;
                }
            }
        }

        public override void OnPossessedStart()
        {
            base.OnPossessedStart();
            if (tongue != null)
            {
                tongue.enabled = true;
                if (disableAutoScanOnPossess)
                {
                    tongue.AutoScanEnabled = false;
                }
            }
            Debug.Log("[FrogAbility] Possessed start: scanning enabled.");
        }

        public override void OnPossessedEnd()
        {
            if (tongue != null)
            {
                tongue.enabled = false;
                tongue.AutoScanEnabled = true;
            }
            if (waitRoutine != null)
            {
                StopCoroutine(waitRoutine);
                waitRoutine = null;
            }
            isWaiting = false;
            IsActing = false;
            base.OnPossessedEnd();
            Debug.Log("[FrogAbility] Possessed end: scanning disabled.");
        }

        private void Update()
        {
            if (!isActive) return;
            if (tongue == null) return;
            if (isWaiting) return;

            Vector2 origin = mouth ? (Vector2)mouth.position : (Vector2)transform.position;
            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, detectRadius, targetLayer);
            if (hits == null || hits.Length == 0) return;

            float facing = transform.localScale.x >= 0 ? 1f : -1f;
            Vector2 forward = new Vector2(facing, 0f);

            Collider2D bestCol = null;
            float bestScore = float.MinValue;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D h = hits[i];
                if (h == null) continue;
                if (!h.CompareTag(targetTag)) continue;

                Vector2 p = h.ClosestPoint(origin);
                float dist = Vector2.Distance(origin, p);
                if (dist > maxHookDistance) continue;

                Vector2 dir = (p - origin).normalized;
                float dot = Vector2.Dot(dir, forward);
                float score = -dist + dot * forwardBias;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCol = h;
                }
            }

            if (bestCol != null)
            {
                Debug.Log($"[FrogAbility] Target found: {bestCol.name} at {bestCol.transform.position}");
                waitRoutine = StartCoroutine(DelayedPull(bestCol));
            }
        }

        private System.Collections.IEnumerator DelayedPull(Collider2D target)
        {
            isWaiting = true;
            IsActing = true;
            if (pauseBeforePull > 0f)
            {
                yield return new WaitForSeconds(pauseBeforePull);
            }

            if (isActive && tongue != null && target != null)
            {
                Debug.Log($"[FrogAbility] Pull after delay: {target.name}");
                tongue.TryManualPull(target);
            }

            if (tongue != null)
            {
                while (tongue.IsBusy)
                {
                    yield return null;
                }
            }

            isWaiting = false;
            IsActing = false;
            waitRoutine = null;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = mouth ? mouth.position : transform.position;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(origin, detectRadius);
        }
    }
}
