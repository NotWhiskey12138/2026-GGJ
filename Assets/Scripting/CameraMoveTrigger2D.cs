using UnityEngine;

[DisallowMultipleComponent]
public class CameraMoveTrigger2D : MonoBehaviour
{
    public enum Axis { X, Y }

    [Header("Camera")]
    public TriggeredCamera2D cameraController;

    [Header("Trigger axis")]
    [Tooltip("竖杠一般选 X（左右穿过）；横杠一般选 Y（上下穿过）")]
    public Axis axis = Axis.X;

    [Tooltip("当玩家刚好在触发条中线附近时，用速度判断方向的阈值")]
    public float deadZone = 0.02f;

    [Header("Bidirectional offsets")]
    [Tooltip("从负方向进入：X=从左进；Y=从下进")]
    public bool useOffsetFromNegativeSide = true;
    public Vector2 offsetFromNegativeSide = new Vector2(2.0f, 1.5f);

    [Tooltip("从正方向进入：X=从右进；Y=从上进")]
    public bool useOffsetFromPositiveSide = false;
    public Vector2 offsetFromPositiveSide = new Vector2(2.0f, 1.5f);

    [Header("Movement")]
    public bool snapInsteadOfSlide = false;

    [Tooltip("若 >= 0，则覆盖相机默认 moveTime（两边共用）")]
    public float moveTimeOverride = -1f;

    [Header("Behavior")]
    [Tooltip("允许重复触发（你要双向就不要 oneShot）")]
    public bool oneShot = false;

    [Tooltip("防止玩家有多个碰撞体导致一帧触发多次")]
    public float retriggerCooldown = 0.08f;

    [Header("Player detection")]
    public string playerTag = "Player";

    private bool _used;
    private float _nextAllowedTime;

    private void Awake()
    {
        if (cameraController == null)
        {
            Camera cam = Camera.main;
            if (cam != null) cameraController = cam.GetComponent<TriggeredCamera2D>();
        }

        BoxCollider2D bc = GetComponent<BoxCollider2D>();
        if (bc != null) bc.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (oneShot && _used) return;
        if (!other.CompareTag(playerTag)) return;
        if (cameraController == null) return;
        if (Time.time < _nextAllowedTime) return;

        _nextAllowedTime = Time.time + retriggerCooldown;

        bool enterFromNegative = DecideEnterFromNegative(other);

        Vector2? offset = null;
        if (enterFromNegative)
        {
            if (useOffsetFromNegativeSide) offset = offsetFromNegativeSide;
        }
        else
        {
            if (useOffsetFromPositiveSide) offset = offsetFromPositiveSide;
        }

        float? mt = moveTimeOverride >= 0f ? moveTimeOverride : (float?)null;

        if (snapInsteadOfSlide) cameraController.SnapToPlayer(offset);
        else cameraController.SlideToPlayer(offset, mt);

        if (oneShot)
        {
            _used = true;
            gameObject.SetActive(false);
        }
    }

    private bool DecideEnterFromNegative(Collider2D other)
    {
        Vector3 c = transform.position; // 触发条中心（够用；如果你有 offset 需求可以扩展）
        Vector3 p = other.bounds.center;

        float delta = (axis == Axis.X) ? (p.x - c.x) : (p.y - c.y);

        // 远离中线：直接用位置判断
        if (delta < -deadZone) return true;   // 从负方向进入
        if (delta > deadZone) return false;   // 从正方向进入

        // 太靠近中线：用速度判断（更稳定）
        Rigidbody2D rb = other.attachedRigidbody;
        if (rb != null)
        {
            float v = (axis == Axis.X) ? rb.linearVelocity.x : rb.linearVelocity.y;
            if (v > 0f) return true;   // 向正方向走 -> 认为从负侧进
            if (v < 0f) return false;  // 向负方向走 -> 认为从正侧进
        }

        // 实在判断不了：默认按负方向处理
        return true;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 1.0f, 0.35f);
        BoxCollider2D bc = GetComponent<BoxCollider2D>();
        if (bc == null) return;

        Vector3 size = new Vector3(bc.size.x, bc.size.y, 0.1f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(bc.offset, size);
        Gizmos.color = new Color(0.2f, 0.9f, 1.0f, 0.9f);
        Gizmos.DrawWireCube(bc.offset, size);
    }
#endif
}
