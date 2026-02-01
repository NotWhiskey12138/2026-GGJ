using UnityEngine;

/// <summary>
/// 2D Deadzone camera:
/// - 玩家在 deadZone 内：相机不动
/// - 超出 deadZone：相机把玩家“推回到 deadZone 边缘”
/// - 跟随速度会随“超出距离 + 玩家离开速度”自适应变快
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class AdaptiveDeadzoneCamera2D : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    [Tooltip("可选：如果有 Rigidbody2D，速度会更准确")]
    public Rigidbody2D targetRb;

    [Header("Framing")]
    [Tooltip("镜头构图偏移：y>0 会让玩家看起来更偏下（相机在玩家上方）")]
    public Vector2 offset = new Vector2(0f, 1.5f);

    [Header("Dead Zone (world units)")]
    [Tooltip("deadZone.x / deadZone.y 是‘半径’：例如 x=2 表示左右各 2 个世界单位的死区")]
    public Vector2 deadZone = new Vector2(2.0f, 1.2f);

    [Header("Follow Axes")]
    public bool followX = true;
    public bool followY = true;

    [Header("Speed Adaptation")]
    [Tooltip("最慢时的平滑时间（大=更拖、更“缓缓跟上”）")]
    public float maxSmoothTime = 0.35f;

    [Tooltip("最快时的平滑时间（小=更紧、更快追上）")]
    public float minSmoothTime = 0.08f;

    [Tooltip("超出 deadZone 的距离越大，跟随越快（建议 1~4）")]
    public float distanceResponse = 2.0f;

    [Tooltip("玩家‘远离镜头中心’的速度越大，跟随越快（建议 0.05~0.3）")]
    public float velocityResponse = 0.12f;

    [Tooltip("限制 SmoothDamp 的最大移动速度，防止极端抖动")]
    public float maxFollowSpeed = 60f;

    [Header("Z / Pixel")]
    public bool lockZ = true;
    public float fixedZ = -10f;

    [Tooltip("像素风可选：开启后按 PPU 对齐相机坐标，减少像素抖动")]
    public bool pixelSnap = false;
    public int pixelsPerUnit = 100;

    private Vector3 _smoothDampVel;
    private Vector3 _lastTargetPos;

    private void Awake()
    {
        if (target == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
        }

        if (targetRb == null && target != null)
            targetRb = target.GetComponent<Rigidbody2D>();

        if (lockZ)
        {
            var pos = transform.position;
            pos.z = fixedZ;
            transform.position = pos;
        }

        if (target != null) _lastTargetPos = target.position;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 camPos = transform.position;
        Vector3 aim = (Vector3)offset + target.position;   // 我们希望“盯住”的点（含构图偏移）
        Vector3 delta = aim - camPos;

        // 计算超出 deadZone 的量（只超出才推动相机）
        float ex = Mathf.Max(0f, Mathf.Abs(delta.x) - deadZone.x);
        float ey = Mathf.Max(0f, Mathf.Abs(delta.y) - deadZone.y);

        // 目标位置：把 target 推回 deadZone 边缘（不是强行居中，因此更稳）
        Vector3 desired = camPos;
        if (followX && ex > 0f) desired.x += Mathf.Sign(delta.x) * ex;
        if (followY && ey > 0f) desired.y += Mathf.Sign(delta.y) * ey;

        if (lockZ) desired.z = fixedZ;

        float excessDist = Mathf.Sqrt(ex * ex + ey * ey);

        // 计算玩家速度（用 Rigidbody2D 更准；否则用位移估算）
        Vector2 v;
        if (targetRb != null) v = targetRb.linearVelocity;
        else
        {
            float dt = Mathf.Max(0.0001f, Time.deltaTime);
            v = (Vector2)((target.position - _lastTargetPos) / dt);
        }
        _lastTargetPos = target.position;

        // 只关心“远离镜头中心方向”的速度分量（越远离越要追）
        float speedAway = 0f;
        if (excessDist > 0.0001f)
        {
            Vector2 dir = new Vector2(delta.x, delta.y).normalized;
            speedAway = Mathf.Max(0f, Vector2.Dot(v, dir));
        }

        // 自适应加速：距离越大、离开越快 → smoothTime 越小（更快追上）
        float boost = excessDist * distanceResponse + speedAway * velocityResponse;

        // 把 boost 映射到 0~1（经验上 0~6 够用，你也可以调）
        float t = Mathf.Clamp01(boost / 6f);
        float smoothTime = Mathf.Lerp(maxSmoothTime, minSmoothTime, t);

        Vector3 newPos = Vector3.SmoothDamp(
            camPos,
            desired,
            ref _smoothDampVel,
            smoothTime,
            maxFollowSpeed,
            Time.deltaTime
        );

        if (lockZ) newPos.z = fixedZ;

        if (pixelSnap && pixelsPerUnit > 0)
        {
            float unit = 1f / pixelsPerUnit;
            newPos.x = Mathf.Round(newPos.x / unit) * unit;
            newPos.y = Mathf.Round(newPos.y / unit) * unit;
        }

        transform.position = newPos;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // 画出 deadZone（世界单位）方便你调参
        Gizmos.color = new Color(0.2f, 0.9f, 1.0f, 0.25f);
        Vector3 c = transform.position;
        Vector3 size = new Vector3(deadZone.x * 2f, deadZone.y * 2f, 0.1f);
        Gizmos.DrawCube(new Vector3(c.x, c.y, 0f), size);

        Gizmos.color = new Color(0.2f, 0.9f, 1.0f, 0.9f);
        Gizmos.DrawWireCube(new Vector3(c.x, c.y, 0f), size);
    }
#endif
}
