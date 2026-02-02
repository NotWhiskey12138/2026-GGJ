using DG.Tweening;
using UnityEngine;

public class FloatUpDown : MonoBehaviour
{
    public float floatHeight = 0.3f;
    public float duration = 1.2f;

    Vector3 startPos;

    void Start()
    {
        startPos = transform.localPosition;

        transform.DOLocalMoveY(startPos.y + floatHeight, duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }
}