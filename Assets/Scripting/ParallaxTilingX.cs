using UnityEngine;

/// <summary>
/// 横向无限平铺：要求子物体里至少有一个 SpriteRenderer；
/// 如果只有一个，会自动复制成两份并排；相机移动时自动把“落后的一块”挪到前面。
/// </summary>
[DisallowMultipleComponent]
public class ParallaxTilingX : MonoBehaviour
{
    public Transform cameraTransform;

    [Tooltip("可选：对齐像素网格，减少像素风接缝抖动（配合你的 PPU）")]
    public bool pixelSnap = false;
    public int pixelsPerUnit = 100;

    private Transform _a;
    private Transform _b;
    private float _tileWidth;

    private void Awake()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void Start()
    {
        // 找第一个 SpriteRenderer
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr == null)
        {
            Debug.LogError($"{name}: No SpriteRenderer found in children.");
            enabled = false;
            return;
        }

        _a = sr.transform;

        // 计算图块宽度（世界单位）
        _tileWidth = sr.bounds.size.x;

        // 如果只有一个 tile，就自动复制一个
        SpriteRenderer[] srs = GetComponentsInChildren<SpriteRenderer>();
        if (srs.Length >= 2)
        {
            _b = srs[1].transform;
        }
        else
        {
            GameObject clone = Instantiate(_a.gameObject, transform);
            clone.name = _a.gameObject.name + "_B";
            _b = clone.transform;
        }

        // 把 B 放到 A 的右边一张宽度
        Vector3 pa = _a.localPosition;
        _a.localPosition = pa;

        Vector3 pb = _b.localPosition;
        pb.x = pa.x + _tileWidth;
        pb.y = pa.y;
        pb.z = pa.z;
        _b.localPosition = pb;
    }

    private void LateUpdate()
    {
        if (cameraTransform == null || _a == null || _b == null) return;

        // 保证 A 在左，B 在右（按世界坐标）
        if (_a.position.x > _b.position.x)
        {
            var t = _a; _a = _b; _b = t;
        }

        float camX = cameraTransform.position.x;

        // 如果相机已经超过左块的右边缘很多，就把左块挪到右块后面
        if (camX - _a.position.x > _tileWidth)
        {
            _a.position = new Vector3(_b.position.x + _tileWidth, _a.position.y, _a.position.z);
            // 交换引用，使得下一次判断仍然正确
            var t = _a; _a = _b; _b = t;
        }
        // 如果相机往左走超过右块的左边缘很多，就把右块挪到左块前面
        else if (_b.position.x - camX > _tileWidth)
        {
            _b.position = new Vector3(_a.position.x - _tileWidth, _b.position.y, _b.position.z);
            var t = _a; _a = _b; _b = t;
        }

        if (pixelSnap && pixelsPerUnit > 0)
        {
            float unit = 1f / pixelsPerUnit;
            SnapTransform(_a, unit);
            SnapTransform(_b, unit);
        }
    }

    private static void SnapTransform(Transform t, float unit)
    {
        Vector3 p = t.position;
        p.x = Mathf.Round(p.x / unit) * unit;
        p.y = Mathf.Round(p.y / unit) * unit;
        t.position = p;
    }
}
