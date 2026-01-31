using System;
using DG.Tweening;
using UnityEngine;

public class Move : MonoBehaviour
{
    public Transform target;

    void Start()
    {
        transform.DOMove(target.position, 3f)
            .SetEase(Ease.OutQuad)
            .OnComplete(()=> Destroy(gameObject));
    }
    
}
