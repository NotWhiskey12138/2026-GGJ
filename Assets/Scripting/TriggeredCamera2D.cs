using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class TriggeredCamera2D : MonoBehaviour
{
    public enum AxisConstraint
    {
        Free,         // 不限制，可前进可回退
        OnlyIncrease, // 只允许值变大（例如 X 只向右推进）
        OnlyDecrease  // 只允许值变小
    }

    [Header("References")]
    public Transform player;

    [Header("Default framing (player appears lower on screen)")]
    public Vector2 defaultOffset = new Vector2(2.0f, 1.5f);

    [Header("Movement")]
    public float defaultMoveTime = 0.25f;

    [Header("Axis constraints")]
    [Tooltip("横版常用：X=OnlyIncrease, Y=Free；如果你要能回退，就把 X 也设成 Free")]
    public AxisConstraint xConstraint = AxisConstraint.Free;

    [Tooltip("上平台/掉下来通常需要 Y 可上可下，所以建议 Free")]
    public AxisConstraint yConstraint = AxisConstraint.Free;

    [Header("Z")]
    public bool lockZ = true;
    public float fixedZ = -10f;

    private Coroutine _moveCo;

    private void Awake()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (lockZ)
        {
            Vector3 pos = transform.position;
            pos.z = fixedZ;
            transform.position = pos;
        }
    }

    public void SnapToPlayer(Vector2? offsetOverride = null)
    {
        if (player == null) return;

        Vector2 offset = offsetOverride ?? defaultOffset;

        Vector3 target = new Vector3(
            player.position.x + offset.x,
            player.position.y + offset.y,
            lockZ ? fixedZ : transform.position.z
        );

        target = ApplyConstraints(target);

        if (_moveCo != null) StopCoroutine(_moveCo);
        transform.position = target;
    }

    public void SlideToPlayer(Vector2? offsetOverride = null, float? moveTimeOverride = null)
    {
        if (player == null) return;

        Vector2 offset = offsetOverride ?? defaultOffset;
        float moveTime = Mathf.Max(0.01f, moveTimeOverride ?? defaultMoveTime);

        Vector3 target = new Vector3(
            player.position.x + offset.x,
            player.position.y + offset.y,
            lockZ ? fixedZ : transform.position.z
        );

        target = ApplyConstraints(target);
        SlideToPosition(target, moveTime);
    }

    public void SlideToPosition(Vector3 worldTarget, float moveTime)
    {
        if (lockZ) worldTarget.z = fixedZ;
        worldTarget = ApplyConstraints(worldTarget);

        if (_moveCo != null) StopCoroutine(_moveCo);
        _moveCo = StartCoroutine(CoSlide(worldTarget, Mathf.Max(0.01f, moveTime)));
    }

    private Vector3 ApplyConstraints(Vector3 target)
    {
        Vector3 cur = transform.position;

        // X
        if (xConstraint == AxisConstraint.OnlyIncrease) target.x = Mathf.Max(cur.x, target.x);
        else if (xConstraint == AxisConstraint.OnlyDecrease) target.x = Mathf.Min(cur.x, target.x);

        // Y
        if (yConstraint == AxisConstraint.OnlyIncrease) target.y = Mathf.Max(cur.y, target.y);
        else if (yConstraint == AxisConstraint.OnlyDecrease) target.y = Mathf.Min(cur.y, target.y);

        if (lockZ) target.z = fixedZ;
        return target;
    }

    private IEnumerator CoSlide(Vector3 target, float moveTime)
    {
        Vector3 start = transform.position;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / moveTime;

            // SmoothStep：开始/结束更顺滑，中间更快
            float s = t * t * (3f - 2f * t);
            Vector3 p = Vector3.Lerp(start, target, s);

            // 过程中也应用约束，避免滑动时越界
            p = ApplyConstraints(p);

            transform.position = p;
            yield return null;
        }

        transform.position = target;
        _moveCo = null;
    }
}
