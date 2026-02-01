using UnityEngine;

/// <summary>
/// 2D Deadzone camera:
/// - ����� deadZone �ڣ��������
/// - ���� deadZone���������ҡ��ƻص� deadZone ��Ե��
/// - �����ٶȻ��桰�������� + ����뿪�ٶȡ�����Ӧ���
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class AdaptiveDeadzoneCamera2D : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    [Tooltip("��ѡ������� Rigidbody2D���ٶȻ��׼ȷ")]
    public Rigidbody2D targetRb;

    [Header("Framing")]
    [Tooltip("��ͷ��ͼƫ�ƣ�y>0 ������ҿ�������ƫ�£����������Ϸ���")]
    public Vector2 offset = new Vector2(0f, 1.5f);

    [Header("Dead Zone (world units)")]
    [Tooltip("deadZone.x / deadZone.y �ǡ��뾶�������� x=2 ��ʾ���Ҹ� 2 �����絥λ������")]
    public Vector2 deadZone = new Vector2(2.0f, 1.2f);

    [Header("Follow Axes")]
    public bool followX = true;
    public bool followY = true;

    [Header("Speed Adaptation")]
    [Tooltip("����ʱ��ƽ��ʱ�䣨��=���ϡ������������ϡ���")]
    public float maxSmoothTime = 0.35f;

    [Tooltip("���ʱ��ƽ��ʱ�䣨С=����������׷�ϣ�")]
    public float minSmoothTime = 0.08f;

    [Tooltip("���� deadZone �ľ���Խ�󣬸���Խ�죨���� 1~4��")]
    public float distanceResponse = 2.0f;

    [Tooltip("��ҡ�Զ�뾵ͷ���ġ����ٶ�Խ�󣬸���Խ�죨���� 0.05~0.3��")]
    public float velocityResponse = 0.12f;

    [Tooltip("���� SmoothDamp ������ƶ��ٶȣ���ֹ���˶���")]
    public float maxFollowSpeed = 60f;

    [Header("Z / Pixel")]
    public bool lockZ = true;
    public float fixedZ = -10f;

    [Tooltip("���ط��ѡ�������� PPU ����������꣬�������ض���")]
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
        Vector3 aim = (Vector3)offset + target.position;   // ����ϣ������ס���ĵ㣨����ͼƫ�ƣ�
        Vector3 delta = aim - camPos;

        // ���㳬�� deadZone ������ֻ�������ƶ������
        float ex = Mathf.Max(0f, Mathf.Abs(delta.x) - deadZone.x);
        float ey = Mathf.Max(0f, Mathf.Abs(delta.y) - deadZone.y);

        // Ŀ��λ�ã��� target �ƻ� deadZone ��Ե������ǿ�о��У���˸��ȣ�
        Vector3 desired = camPos;
        if (followX && ex > 0f) desired.x += Mathf.Sign(delta.x) * ex;
        if (followY && ey > 0f) desired.y += Mathf.Sign(delta.y) * ey;

        if (lockZ) desired.z = fixedZ;

        float excessDist = Mathf.Sqrt(ex * ex + ey * ey);

        // ��������ٶȣ��� Rigidbody2D ��׼��������λ�ƹ��㣩
        Vector2 v;
        if (targetRb != null) v = targetRb.linearVelocity;
        else
        {
            float dt = Mathf.Max(0.0001f, Time.deltaTime);
            v = (Vector2)((target.position - _lastTargetPos) / dt);
        }
        _lastTargetPos = target.position;

        // ֻ���ġ�Զ�뾵ͷ���ķ��򡱵��ٶȷ�����ԽԶ��ԽҪ׷��
        float speedAway = 0f;
        if (excessDist > 0.0001f)
        {
            Vector2 dir = new Vector2(delta.x, delta.y).normalized;
            speedAway = Mathf.Max(0f, Vector2.Dot(v, dir));
        }

        // ����Ӧ���٣�����Խ���뿪Խ�� �� smoothTime ԽС������׷�ϣ�
        float boost = excessDist * distanceResponse + speedAway * velocityResponse;

        // �� boost ӳ�䵽 0~1�������� 0~6 ���ã���Ҳ���Ե���
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
        // ���� deadZone�����絥λ�����������
        Gizmos.color = new Color(0.2f, 0.9f, 1.0f, 0.25f);
        Vector3 c = transform.position;
        Vector3 size = new Vector3(deadZone.x * 2f, deadZone.y * 2f, 0.1f);
        Gizmos.DrawCube(new Vector3(c.x, c.y, 0f), size);

        Gizmos.color = new Color(0.2f, 0.9f, 1.0f, 0.9f);
        Gizmos.DrawWireCube(new Vector3(c.x, c.y, 0f), size);
    }
#endif
}
