using System.Collections;
using UnityEngine;
using DG.Tweening;

public class GuardPatrol2D_Range : MonoBehaviour
{

    private Tween idleTween;     // 等待时循环动画
    private Tween turnTween;     // 转身的小抖动
    private Quaternion baseRotation;
    public SpriteRenderer sprite;  // 子物体上的 SpriteRenderer
    private Vector3 baseVisualScale;
    [SerializeField] private Transform visualRoot;   // 拖 VisualRoot
    [SerializeField] private Transform lightCone;    // 可选：拖 LightCone
    private Vector3 visualBaseScale;

    [SerializeField] private Animator animator;

// 提前缓存 hash（性能更好）
    private static readonly int IsWaitingHash = Animator.StringToHash("IsWaiting");


    public enum StartDirection { Left, Right }

    [Header("Start Direction")]
    public StartDirection startDirection = StartDirection.Right;

    [Header("Range relative to spawn position (local offsets)")]
    [Tooltip("��Գ��������߽�ƫ�ƣ�ͨ��Ϊ������")]
    public float leftOffset = -3f;

    [Tooltip("��Գ�������ұ߽�ƫ�ƣ�ͨ��Ϊ������")]
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

    private Vector3 baseScale; // ��¼��ʼscale����תʱ���ı��С

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        // Animator：最好 Inspector 手动拖，自动找也行
        if (animator == null) animator = GetComponentInChildren<Animator>();

        // 找 VisualRoot
        if (visualRoot == null)
        {
            visualRoot = transform.Find("VisualRoot");
            if (visualRoot == null)
            {
                Debug.LogWarning("VisualRoot not found. Using self as visualRoot.");
                visualRoot = transform;
            }
        }

        // ✅ 关键：记录 visualRoot 的初始 scale，用来翻面/idle（否则是0）
        visualBaseScale = visualRoot.localScale;

        // ✅ 关键：rotation 也记录 visualRoot 的本地旋转（不要用 transform.rotation）
        baseRotation = visualRoot.localRotation;

        // ✅ 关键：找到 sprite，用于 turn anticipation（否则 sprite 为空）
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
        if (sprite != null) baseVisualScale = sprite.transform.localScale;

        // offsets 修正
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

        // ������ʼ���������һ������
        targetX = (startDirection == StartDirection.Right) ? maxX : minX;

        // ���̷�ת����Ӧ����
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

        if (!waiting && Mathf.Abs(newX - targetX) < 0.001f)
        {
            waiting = true;

// 通知 Animator：进入等待状态
            animator.SetBool(IsWaitingHash, true);

            StartCoroutine(WaitAndTurn());

        }

    }

    private IEnumerator WaitAndTurn()
    {
        PlayGuardIdle();
        yield return new WaitForSeconds(waitSeconds);
        StopGuardIdle();

        float nextX = Mathf.Approximately(targetX, minX) ? maxX : minX;

        PlayTurnAnticipation(() =>
        {
            targetX = nextX;
            FaceTarget(targetX);
        });

        yield return new WaitForSeconds(0.2f);
        waiting = false;

// 离开等待状态
        animator.SetBool(IsWaitingHash, false);

    }

    private void FaceTarget(float x)
    {
        float currentX = rb ? rb.position.x : transform.position.x;
        bool faceRight = x > currentX;
        float sign = faceRight ? 1f : -1f;

        // 只翻视觉节点，不动 root（避免影响 Rigidbody2D / 碰撞体）
        visualRoot.localScale = new Vector3(Mathf.Abs(visualBaseScale.x) * sign, visualBaseScale.y, visualBaseScale.z);
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
    
    private void PlayGuardIdle()
    {
        StopGuardIdle();

        float dir = Mathf.Sign(visualRoot.localScale.x);
        float absX = Mathf.Abs(visualBaseScale.x);

        var seq = DOTween.Sequence();

        // 只在视觉节点上做“压缩呼吸”，保留 dir
        seq.Append(visualRoot.DOScale(
            new Vector3(absX * dir, visualBaseScale.y * 0.95f, visualBaseScale.z), 0.18f));
        seq.Append(visualRoot.DOScale(
            new Vector3(absX * dir, visualBaseScale.y, visualBaseScale.z), 0.25f));

        // 摆动也用 LocalRotate，避免影响 root
        seq.Append(visualRoot.DOLocalRotate(new Vector3(0,0, 3f), 0.35f));
        seq.Append(visualRoot.DOLocalRotate(new Vector3(0,0,-3f), 0.70f));
        seq.Append(visualRoot.DOLocalRotate(Vector3.zero, 0.35f));

        seq.SetEase(Ease.InOutSine);
        seq.SetLoops(-1);

        idleTween = seq;
    }

    private void StopGuardIdle()
    {
        idleTween?.Kill();

        float dir = Mathf.Sign(visualRoot.localScale.x);
        float absX = Mathf.Abs(visualBaseScale.x);

        visualRoot.localScale = new Vector3(absX * dir, visualBaseScale.y, visualBaseScale.z);
        visualRoot.localRotation = baseRotation;
    }
    
    private void PlayTurnAnticipation(System.Action onFlip)
    {
        turnTween?.Kill();
        var v = sprite.transform;

        turnTween = DOTween.Sequence()
            .Append(v.DOScaleY(baseVisualScale.y * 0.9f, 0.12f).SetEase(Ease.OutQuad))
            .AppendCallback(() => onFlip?.Invoke())
            .Append(v.DOScaleY(baseVisualScale.y, 0.18f).SetEase(Ease.OutBack));
    }


}
