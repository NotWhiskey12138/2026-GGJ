using UnityEngine;
using DG.Tweening;

public class DraggablePlatform : MonoBehaviour
{
    [Header("拖拽设置")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float dragSpeed = 1f;
    
    [Header("边界限制")]
    [SerializeField] private MoveBounds moveBounds;
    
    [Header("动画设置")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float animDuration = 0.2f;
    
    private bool isDragging = false;
    private Vector3 offset;
    private Vector3 originalScale;
    private Tween scaleTween;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
        
        originalScale = transform.localScale;
    }

    void OnMouseDown()
    {
        // 计算鼠标点击位置与物体的偏移
        Vector3 mousePos = GetMouseWorldPos();
        offset = transform.position - mousePos;
        
        isDragging = true;
        
        // 使用DOTween放大效果
        scaleTween?.Kill();
        scaleTween = transform.DOScale(originalScale * hoverScale, animDuration)
            .SetEase(Ease.OutBack);
    }

    void OnMouseDrag()
    {
        if (isDragging)
        {
            Vector3 targetPos = GetMouseWorldPos() + offset;
            
            // 如果有边界限制，应用限制
            if (moveBounds != null)
            {
                targetPos = moveBounds.ClampPosition(targetPos);
            }
            
            transform.position = Vector3.Lerp(transform.position, targetPos, dragSpeed * Time.deltaTime * 10f);
        }
    }

    void OnMouseUp()
    {
        isDragging = false;
        
        // 恢复原始大小
        scaleTween?.Kill();
        scaleTween = transform.DOScale(originalScale, animDuration)
            .SetEase(Ease.OutBack);
    }

    private Vector3 GetMouseWorldPos()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = mainCamera.WorldToScreenPoint(transform.position).z;
        return mainCamera.ScreenToWorldPoint(mousePos);
    }

    // 鼠标悬停效果（可选）
    void OnMouseEnter()
    {
        if (!isDragging)
        {
            scaleTween?.Kill();
            scaleTween = transform.DOScale(originalScale * 1.05f, animDuration);
        }
    }

    void OnMouseExit()
    {
        if (!isDragging)
        {
            scaleTween?.Kill();
            scaleTween = transform.DOScale(originalScale, animDuration);
        }
    }
}