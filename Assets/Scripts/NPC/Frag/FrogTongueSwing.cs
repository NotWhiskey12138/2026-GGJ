using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Rigidbody2D))]
public class FrogTonguePullImpulse : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform mouth;
    [SerializeField] private LineRenderer tongueLine;

    [Header("Detect")]
    [SerializeField] private LayerMask hookLayer;          // 你可以用平台layer 或 SwingPoint layer
    [SerializeField] private float detectRadius = 4f;
    [SerializeField] private float maxHookDistance = 5f;
    [SerializeField] private float forwardBias = 0.8f;     // 更倾向前方目标

    [Header("Auto Trigger")]
    [SerializeField] private float cooldown = 1.0f;
    [SerializeField] private bool requireAirborne = false; // 你要地面也能拉就关掉
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.12f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Pull + Impulse")]
    [SerializeField] private float pullDuration = 0.10f;   // 拉一下的时间
    [SerializeField] private float pullDistance = 1.2f;    // 往目标方向拉多远（不是拉到目标）
    [SerializeField] private float impulse = 10f;          // 释放时给的冲量
    [SerializeField] private float impulseUpBonus = 0.0f;  // 可选：额外向上加一点

    private Rigidbody2D rb;
    private float cdTimer;
    private bool busy;
    private Tween moveTween;
    private Vector2 hookPoint;
    
    //bee
    private Collider2D lastHookCollider;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        cdTimer -= Time.deltaTime;
        if (busy || cdTimer > 0f) return;

        if (requireAirborne && IsGrounded()) return;

        if (TryFindHook(out hookPoint))
        {
            AutoTonguePull(hookPoint);
            
        }
    }

    bool TryFindHook(out Vector2 best)
    {
        Vector2 origin = mouth ? (Vector2)mouth.position : rb.position;
        var hits = Physics2D.OverlapCircleAll(origin, detectRadius, hookLayer);

        best = default;
        if (hits == null || hits.Length == 0) return false;

        float facing = transform.localScale.x >= 0 ? 1f : -1f;
        Vector2 forward = new Vector2(facing, 0f);

        Collider2D bestCol = null;
        float bestScore = float.MinValue;

        foreach (var h in hits)
        {
            Vector2 p = h.ClosestPoint(origin); // 平台上取最近点（比transform.position稳）
            float dist = Vector2.Distance(origin, p);
            if (dist > maxHookDistance) continue;

            Vector2 dir = (p - origin).normalized;
            float dot = Vector2.Dot(dir, forward);
            float score = -dist + dot * forwardBias;

            if (score > bestScore)
            {
                bestScore = score;
                bestCol = h;
                best = p;
            }
            
        }

        //bee
        lastHookCollider = bestCol;

        return bestCol != null;
    }

    void AutoTonguePull(Vector2 point)
    {
        busy = true;
        cdTimer = cooldown;

        // 停止现有速度，让“拉一下”更可控（如果你希望保留惯性就删掉这行）
        rb.linearVelocity = Vector2.zero;

        ShowTongue(true, point);

        Vector2 origin = mouth ? (Vector2)mouth.position : rb.position;
        Vector2 dir = (point - origin).normalized;
        Vector2 targetPos = rb.position + dir * pullDistance;

        // Tween 把青蛙短距离拉近（视觉“被拽”）
        moveTween?.Kill();
        moveTween = rb.DOMove(targetPos, pullDuration)
            .SetEase(Ease.OutSine)
            .OnComplete(() =>
            {
                // 释放瞬间给冲量：惯性飞出去
                Vector2 impulseDir = dir;
                Vector2 imp = impulseDir * impulse + Vector2.up * impulseUpBonus;
                rb.AddForce(imp, ForceMode2D.Impulse);

                var lickable = lastHookCollider != null ? lastHookCollider.GetComponentInParent<Lickable>() : null;
                lickable?.OnLicked();
                lastHookCollider = null;

                
                ShowTongue(false, point);
                busy = false;
                
                
            });
    }

    void ShowTongue(bool show, Vector2 point)
    {
        if (tongueLine == null) return;

        tongueLine.enabled = show;
        if (!show) return;

        tongueLine.positionCount = 2;
        Vector3 start = mouth ? mouth.position : (Vector3)rb.position;
        tongueLine.SetPosition(0, start);
        tongueLine.SetPosition(1, (Vector3)point);
    }

    bool IsGrounded()
    {
        if (groundCheck == null) return false;
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer) != null;
    }

    void OnDisable()
    {
        moveTween?.Kill();
    }

    void OnDrawGizmosSelected()
    {
        Vector3 origin = mouth ? mouth.position : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(origin, detectRadius);
    }
}
