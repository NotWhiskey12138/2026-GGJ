using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class DraggablePlatform : MonoBehaviour
{
    [Header("拖拽设置")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float dragSpeed = 10f;
    [SerializeField] private bool is2D = false;
    
    [Header("边界限制")]
    [SerializeField] private MoveBounds moveBounds;
    [SerializeField] private float snapBackDuration = 0.5f; // 回弹动画时长
    [SerializeField] private Ease snapBackEase = Ease.OutBack; // 回弹曲线
    
    [Header("碰撞检测")]
    [SerializeField] private LayerMask platformLayer; // 平台所在图层
    [SerializeField] private float overlapThreshold = 0.3f; // 重叠阈值（30%）
    
    [Header("颜色设置")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color draggingColor = Color.cyan;
    [SerializeField] private Color invalidColor = Color.red; // 非法位置颜色
    [SerializeField] private float colorChangeDuration = 0.2f;
    
    [Header("动画设置")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float animDuration = 0.2f;
    
    private bool isDragging = false;
    private Vector3 offset;
    private Vector3 originalScale;
    private Vector3 lastValidPosition; // 记录最后合法位置
    private Tween scaleTween;
    private Tween colorTween;
    private Tween moveTween;
    private float zDistance;
    
    private Renderer platformRenderer;
    private MaterialPropertyBlock materialPropertyBlock;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
        
        originalScale = transform.localScale;
        lastValidPosition = transform.position;
        zDistance = mainCamera.WorldToScreenPoint(transform.position).z;
        
        // 获取渲染器
        platformRenderer = GetComponent<Renderer>();
        if (platformRenderer != null)
        {
            materialPropertyBlock = new MaterialPropertyBlock();
            platformRenderer.GetPropertyBlock(materialPropertyBlock);
            materialPropertyBlock.SetColor("_Color", normalColor);
            platformRenderer.SetPropertyBlock(materialPropertyBlock);
        }
        
        // 检查并自动添加Collider
        Collider col = GetComponent<Collider>();
        Collider2D col2D = GetComponent<Collider2D>();
        
        if (col == null && col2D == null)
        {
            if (is2D)
            {
                gameObject.AddComponent<BoxCollider2D>();
            }
            else
            {
                gameObject.AddComponent<BoxCollider>();
            }
        }
    }

    void OnMouseDown()
    {
        Vector3 mousePos = GetMouseWorldPos();
        offset = transform.position - mousePos;
        
        isDragging = true;
        
        // 停止可能正在进行的回弹动画
        moveTween?.Kill();
        
        // DOTween放大
        scaleTween?.Kill();
        scaleTween = transform.DOScale(originalScale * hoverScale, animDuration)
            .SetEase(Ease.OutBack);
        
        // 改变颜色为拖拽颜色
        ChangeColor(draggingColor);
    }

    void OnMouseDrag()
    {
        if (!isDragging) return;
        
        Vector3 mouseWorldPos = GetMouseWorldPos();
        Vector3 targetPos = mouseWorldPos + offset;
        
        // 应用边界限制
        if (moveBounds != null)
        {
            targetPos = moveBounds.ClampPosition(targetPos);
        }
        
        // 移动到目标位置
        transform.position = Vector3.Lerp(transform.position, targetPos, dragSpeed * Time.deltaTime);
        
        // 检查当前位置是否合法
        bool isValidPosition = IsPositionValid(transform.position);
        
        if (isValidPosition)
        {
            // 合法位置 - 更新最后合法位置，显示拖拽颜色
            lastValidPosition = transform.position;
            ChangeColor(draggingColor);
        }
        else
        {
            // 非法位置 - 显示红色警告
            ChangeColor(invalidColor);
        }
    }

    void OnMouseUp()
    {
        isDragging = false;
        
        // 恢复大小
        scaleTween?.Kill();
        scaleTween = transform.DOScale(originalScale, animDuration)
            .SetEase(Ease.OutBack);
        
        // 检查最终位置是否合法
        bool isFinalPositionValid = IsPositionValid(transform.position);
        
        if (!isFinalPositionValid)
        {
            // 非法位置 - 弹回最后合法位置
            moveTween?.Kill();
            moveTween = transform.DOMove(lastValidPosition, snapBackDuration)
                .SetEase(snapBackEase)
                .OnComplete(() => {
                    ChangeColor(normalColor);
                });
        }
        else
        {
            // 合法位置 - 直接恢复颜色
            ChangeColor(normalColor);
        }
    }

    private Vector3 GetMouseWorldPos()
    {
        Vector3 mousePos = Input.mousePosition;
        
        if (is2D)
        {
            mousePos.z = 10f;
        }
        else
        {
            mousePos.z = zDistance;
        }
        
        return mainCamera.ScreenToWorldPoint(mousePos);
    }

    // 检查位置是否合法（不超界且不过度重叠）
    private bool IsPositionValid(Vector3 position)
    {
        // 1. 检查是否超出边界
        if (moveBounds != null && !moveBounds.IsWithinBounds(position))
        {
            return false;
        }
        
        // 2. 检查是否与其他平台重叠过多
        if (IsOverlapping(position))
        {
            return false;
        }
        
        return true;
    }

    // 检查是否与其他平台重叠
    private bool IsOverlapping(Vector3 position)
    {
        if (is2D)
        {
            // 2D 物理检测
            Collider2D myCollider = GetComponent<Collider2D>();
            if (myCollider == null)
            {
                return false;
            }
            
            // 获取碰撞器的大小
            Bounds myBounds = myCollider.bounds;
            Vector2 size = new Vector2(myBounds.size.x, myBounds.size.y) * 0.9f;
            
            // 在目标位置检测重叠
            Collider2D[] overlaps = Physics2D.OverlapBoxAll(
                position,
                size,
                0f,
                platformLayer
            );
            
            foreach (Collider2D col in overlaps)
            {
                // 忽略自己
                if (col.gameObject == gameObject) continue;
                
                // 计算重叠面积比例
                Bounds otherBounds = col.bounds;
                Bounds testBounds = new Bounds(position, myBounds.size);
                
                if (testBounds.Intersects(otherBounds))
                {
                    // 计算重叠面积（2D用xy）
                    Vector2 overlapMin = Vector2.Max(
                        new Vector2(testBounds.min.x, testBounds.min.y),
                        new Vector2(otherBounds.min.x, otherBounds.min.y)
                    );
                    Vector2 overlapMax = Vector2.Min(
                        new Vector2(testBounds.max.x, testBounds.max.y),
                        new Vector2(otherBounds.max.x, otherBounds.max.y)
                    );
                    Vector2 overlapSize = overlapMax - overlapMin;
                    
                    float overlapArea = overlapSize.x * overlapSize.y;
                    float myArea = myBounds.size.x * myBounds.size.y;
                    
                    float overlapRatio = overlapArea / myArea;
                    
                    // 如果重叠超过阈值，返回true
                    if (overlapRatio > overlapThreshold)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
        else
        {
            // 3D 物理检测
            Collider myCollider = GetComponent<Collider>();
            if (myCollider == null)
            {
                return false;
            }
            
            // 获取碰撞器的大小
            Bounds myBounds = myCollider.bounds;
            Vector3 halfExtents = myBounds.extents;
            
            // 在目标位置检测重叠
            Collider[] overlaps = Physics.OverlapBox(
                position, 
                halfExtents * 0.9f,
                transform.rotation,
                platformLayer
            );
            
            foreach (Collider col in overlaps)
            {
                // 忽略自己
                if (col.gameObject == gameObject) continue;
                
                // 计算重叠面积比例
                Bounds otherBounds = col.bounds;
                Bounds testBounds = new Bounds(position, myBounds.size);
                
                if (testBounds.Intersects(otherBounds))
                {
                    // 计算重叠体积
                    Vector3 overlapMin = Vector3.Max(testBounds.min, otherBounds.min);
                    Vector3 overlapMax = Vector3.Min(testBounds.max, otherBounds.max);
                    Vector3 overlapSize = overlapMax - overlapMin;
                    
                    float overlapVolume = overlapSize.x * overlapSize.y * overlapSize.z;
                    float myVolume = myBounds.size.x * myBounds.size.y * myBounds.size.z;
                    
                    float overlapRatio = overlapVolume / myVolume;
                    
                    // 如果重叠超过阈值，返回true
                    if (overlapRatio > overlapThreshold)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
    }

    // 改变颜色
    private void ChangeColor(Color targetColor)
    {
        if (platformRenderer == null) return;
        
        colorTween?.Kill();
        
        Color currentColor = materialPropertyBlock.GetColor("_Color");
        
        colorTween = DOTween.To(
            () => currentColor,
            color => {
                materialPropertyBlock.SetColor("_Color", color);
                platformRenderer.SetPropertyBlock(materialPropertyBlock);
            },
            targetColor,
            colorChangeDuration
        );
    }

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

    // 在Scene视图中绘制调试信息
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        
        if (is2D)
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                // 绘制当前检测范围
                Gizmos.color = Color.yellow;
                Vector3 size = col.bounds.size * 0.9f;
                size.z = 0.1f; // 2D用薄的立方体
                Gizmos.DrawWireCube(transform.position, size);
            }
        }
        else
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                // 绘制当前检测范围
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(transform.position, col.bounds.size * 0.9f);
            }
        }
        
        // 绘制最后合法位置
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(lastValidPosition, 0.2f);
    }
}