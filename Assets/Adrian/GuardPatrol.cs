using System.Collections;
using UnityEngine;

public class GuardPatrol2D_Range : MonoBehaviour
{
    public enum StartDirection { Left, Right }

    [Header("Start Direction")]
    public StartDirection startDirection = StartDirection.Right;

    [Header("Range relative to spawn position (local offsets)")]
    [Tooltip("相对出生点的左边界偏移（通常为负数）")]
    public float leftOffset = -3f;

    [Tooltip("相对出生点的右边界偏移（通常为正数）")]
    public float rightOffset = 3f;

    [Header("Movement")]
    public float speed = 2f;
    public float waitSeconds = 3f;

    [Header("Physics (recommended)")]
    public Rigidbody2D rb;

    private float startX;
    private float minX, maxX;
    private float targetX;
    private bool waiting;

    private Vector3 baseScale; // 记录初始scale，翻转时不改变大小

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        baseScale = transform.localScale;

        // 防呆：如果你填反了，自动交换
        if (leftOffset > rightOffset)
        {
            float t = leftOffset;
            leftOffset = rightOffset;
            rightOffset = t;
        }
    }

    private void Start()
    {
        startX = rb ? rb.position.x : transform.position.x;

        minX = startX + leftOffset;
        maxX = startX + rightOffset;

        // 根据起始方向决定第一段走向
        targetX = (startDirection == StartDirection.Right) ? maxX : minX;

        // 立刻翻转到对应方向
        FaceTarget(targetX);
    }

    private void FixedUpdate()
    {
        if (waiting) return;

        Vector2 pos = rb ? rb.position : (Vector2)transform.position;

        float newX = Mathf.MoveTowards(pos.x, targetX, speed * Time.fixedDeltaTime);
        Vector2 newPos = new Vector2(newX, pos.y);

        if (rb) rb.MovePosition(newPos);
        else transform.position = new Vector3(newX, transform.position.y, transform.position.z);

        if (Mathf.Abs(newX - targetX) < 0.001f)
        {
            StartCoroutine(WaitAndTurn());
        }
    }

    private IEnumerator WaitAndTurn()
    {
        waiting = true;
        yield return new WaitForSeconds(waitSeconds);

        targetX = Mathf.Approximately(targetX, minX) ? maxX : minX;
        FaceTarget(targetX);

        waiting = false;
    }

    private void FaceTarget(float x)
    {
        bool faceRight = x > transform.position.x;
        float sign = faceRight ? 1f : -1f;

        // 用初始scale的绝对值，保证不会越翻越小/越大
        transform.localScale = new Vector3(Mathf.Abs(baseScale.x) * sign, baseScale.y, baseScale.z);
    }

    private void OnDrawGizmosSelected()
    {
        float curX = Application.isPlaying
            ? startX
            : (rb ? rb.position.x : transform.position.x);

        float a = curX + leftOffset;
        float b = curX + rightOffset;

        Gizmos.color = Color.yellow;
        Vector3 p1 = new Vector3(a, transform.position.y, transform.position.z);
        Vector3 p2 = new Vector3(b, transform.position.y, transform.position.z);
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawSphere(p1, 0.08f);
        Gizmos.DrawSphere(p2, 0.08f);
    }
}
