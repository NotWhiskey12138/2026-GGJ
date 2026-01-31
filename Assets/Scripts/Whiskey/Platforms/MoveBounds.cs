using UnityEngine;

public class MoveBounds : MonoBehaviour
{
    [Header("边界设置")]
    [SerializeField] private Vector3 minBounds = new Vector3(-10f, -5f, -10f);
    [SerializeField] private Vector3 maxBounds = new Vector3(10f, 5f, 10f);
    
    [Header("可视化")]
    [SerializeField] private Color boundsColor = Color.green;
    [SerializeField] private bool showBounds = true;

    // 限制位置在边界内
    public Vector3 ClampPosition(Vector3 position)
    {
        return new Vector3(
            Mathf.Clamp(position.x, minBounds.x, maxBounds.x),
            Mathf.Clamp(position.y, minBounds.y, maxBounds.y),
            Mathf.Clamp(position.z, minBounds.z, maxBounds.z)
        );
    }

    // 检查位置是否在边界内
    public bool IsWithinBounds(Vector3 position)
    {
        return position.x >= minBounds.x && position.x <= maxBounds.x &&
               position.y >= minBounds.y && position.y <= maxBounds.y &&
               position.z >= minBounds.z && position.z <= maxBounds.z;
    }

    // 在Scene视图中绘制边界框
    void OnDrawGizmos()
    {
        if (showBounds)
        {
            Gizmos.color = boundsColor;
            Vector3 center = (minBounds + maxBounds) / 2f;
            Vector3 size = maxBounds - minBounds;
            Gizmos.DrawWireCube(center, size);
        }
    }
}