using DG.Tweening;
using UnityEngine;

public class SwingPoint : MonoBehaviour
{
    Tween idleFloatTween;
    Tween idleRotateTween;

    public void PlayIdleBuzz()
    {
        transform.DOKill(); // 防止残留

        // 上下漂浮
        idleFloatTween = transform.DOMoveY(transform.position.y + 0.08f, 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        // 左右轻微摆头
        idleRotateTween = transform.DORotate(new Vector3(0, 0, 3f), 0.3f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

}
