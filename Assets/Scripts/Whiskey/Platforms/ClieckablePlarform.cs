using System;
using UnityEngine;
using DG.Tweening;
using TMPro;
using UnityEngine.Rendering.Universal;

public class ClickablePlatform : MonoBehaviour
{
    [Header("颜色设置")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color activeColor = Color.red;
    [SerializeField] private float colorChangeDuration = 0.3f;
    
    [Header("动画设置")]
    [SerializeField] private float clickScale = 0.9f; // 点击时缩小
    [SerializeField] private float animDuration = 0.2f;
    
    [Header("状态")]
    [SerializeField] private bool isActive = false; // 是否处于激活（红色）状态

    [Header("蜜蜂")] [SerializeField] private GameObject bee;

    [Header("间隔参数")] 
    [SerializeField] private float resetTime;
    [SerializeField] private bool canPutBee;
    private float currentTime;

    [Header("倒计时显示")] 
    [SerializeField] private TextMeshPro countdownText;
    [SerializeField] private bool showDecimal = false;
    
    [Header("Light 2D")]
    [SerializeField] private Light2D light2D;
    
    private Renderer platformRenderer;
    private MaterialPropertyBlock materialPropertyBlock;
    private Vector3 originalScale;
    private Tween colorTween;
    private Tween scaleTween;
    
    // 公开属性供外部检测
    public bool IsActive => isActive;

    void Start()
    {
        originalScale = transform.localScale;
        canPutBee = true;
        currentTime = resetTime;
        
        // 获取渲染器
        platformRenderer = GetComponent<Renderer>();
        if (platformRenderer != null)
        {
            materialPropertyBlock = new MaterialPropertyBlock();
            platformRenderer.GetPropertyBlock(materialPropertyBlock);
            
            // 设置初始颜色
            Color initialColor = isActive ? activeColor : normalColor;
            materialPropertyBlock.SetColor("_Color", initialColor);
            platformRenderer.SetPropertyBlock(materialPropertyBlock);
        }
        
        // 确保有Collider
        if (GetComponent<Collider>() == null && GetComponent<Collider2D>() == null)
        {
            gameObject.AddComponent<BoxCollider2D>();
        }
        
        if (countdownText != null)
        {
            countdownText.text = "";
            countdownText.gameObject.SetActive(false);
        }

    }

    private int lastShown = -999;

    private void Update()
    {
        if (!canPutBee)
        {
            light2D.intensity = 5f;
            
            currentTime -= Time.deltaTime;

            // 夹住，避免变成负数
            if (currentTime < 0f) currentTime = 0f;

            // 计算要显示的整数秒：5,4,3,2,1
            int show = Mathf.CeilToInt(currentTime);

            // 只有当数字变化时才刷新文本（防止每帧改字符串）
            if (show != lastShown)
            {
                lastShown = show;

                if (countdownText != null)
                {
                    countdownText.gameObject.SetActive(true);
                    countdownText.text = show.ToString();
                }
            }

            if (currentTime <= 0f)
            {
                canPutBee = true;
                currentTime = resetTime;
                lastShown = -999;

                if (countdownText != null)
                {
                    countdownText.text = "";
                    countdownText.gameObject.SetActive(false);
                }
            }
        }
    }


    void OnMouseDown()
    {
        if (canPutBee)
        {
            light2D.intensity = 1f;
            
            canPutBee = false;
            // 切换状态
            ToggleState();

            Vector3 spawnPos = transform.position + Vector3.down * 1f;
            GameObject b = Instantiate(bee, spawnPos, Quaternion.identity);
            b.tag = "FrogTarget";

            // 从更下面弹到 spawnPos
            b.transform.DOMoveY(spawnPos.y, 0.3f)
                .From(spawnPos.y - 0.6f)
                .SetEase(Ease.OutBack);

            
            // 点击动画 - 缩小
            scaleTween?.Kill();
            scaleTween = transform.DOScale(originalScale * clickScale, animDuration * 0.5f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => {
                    // 恢复大小
                    transform.DOScale(originalScale, animDuration * 0.5f)
                        .SetEase(Ease.OutBack);
                });

            
        }
    }

    // 切换激活状态
    public void ToggleState()
    {
        isActive = !isActive;
        ChangeColor(isActive ? activeColor : normalColor);
    }

    // 设置激活状态
    public void SetActive(bool active)
    {
        if (isActive != active)
        {
            isActive = active;
            ChangeColor(isActive ? activeColor : normalColor);
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
        ).SetEase(Ease.InOutQuad);
    }

    // 鼠标悬停效果（可选）
    void OnMouseEnter()
    {
        scaleTween?.Kill();
        scaleTween = transform.DOScale(originalScale * 1.05f, animDuration);
    }

    void OnMouseExit()
    {
        scaleTween?.Kill();
        scaleTween = transform.DOScale(originalScale, animDuration);
    }

    // 清理Tween
    void OnDestroy()
    {
        colorTween?.Kill();
        scaleTween?.Kill();
    }
}
