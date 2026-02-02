using UnityEngine;

/// <summary>
/// Stable infinite tiling in X (no drift, no tearing):
/// - Uses sprite pixel width / PPU to compute exact step
/// - Positions tiles in LOCAL space using integer tile indices (no accumulation)
/// - Uses 3 tiles to avoid gaps during fast camera moves
/// - Optional 1px overlap to eliminate thin seams
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(50)] // run after parallax (optional but nice)
public class ParallaxTilingX : MonoBehaviour
{
    public Transform cameraTransform;

    [Header("Seam fix")]
    [Tooltip("Overlap in pixels to kill thin seams (usually 1 or 2)")]
    public int overlapPixels = 1;

    [Header("Optional pixel snap")]
    public bool pixelSnap = false;
    public int pixelsPerUnit = 100;

    [Header("Tiles")]
    [Tooltip("How many tiles to keep around (2 works, 3 is safer)")]
    [Range(2, 5)] public int tileCount = 3;

    private Transform[] _tiles;
    private float _stepLocal;     // spacing in THIS object's local units
    private float _unitLocal;     // local snap unit

    private void Awake()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void Start()
    {
        // Find a SpriteRenderer in children as the prototype tile
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr == null || sr.sprite == null)
        {
            Debug.LogError($"{name}: No SpriteRenderer/Sprite found in children.");
            enabled = false;
            return;
        }

        // Ensure we have exactly tileCount tiles
        // First tile uses existing SR transform; others are clones
        _tiles = new Transform[tileCount];
        _tiles[0] = sr.transform;

        for (int i = 1; i < tileCount; i++)
        {
            GameObject clone = Instantiate(_tiles[0].gameObject, transform);
            clone.name = _tiles[0].gameObject.name + $"_{i}";
            _tiles[i] = clone.transform;
        }

        // Compute exact width using sprite pixel width / PPU (stable!)
        float ppu = sr.sprite.pixelsPerUnit;
        float pixelWidth = sr.sprite.rect.width;
        float widthWorld = pixelWidth / ppu; // width in world units before scaling

        // Convert to LOCAL spacing:
        // localSpacing * parentLossyScaleX == worldWidth * tileLossyScaleX
        float parentScaleX = transform.lossyScale.x;
        float tileScaleX = _tiles[0].lossyScale.x;

        float widthLocal = (widthWorld * tileScaleX) / Mathf.Max(0.0001f, parentScaleX);

        float overlapWorld = overlapPixels / ppu; // in world units (before scaling)
        float overlapLocal = (overlapWorld * tileScaleX) / Mathf.Max(0.0001f, parentScaleX);

        _stepLocal = widthLocal - overlapLocal;

        // Initial placement (local): centered around 0..(tileCount-1)
        // We'll re-place correctly in LateUpdate anyway.
        for (int i = 0; i < tileCount; i++)
        {
            SetLocalX(_tiles[i], i * _stepLocal);
        }

        _unitLocal = (pixelsPerUnit > 0) ? (1f / pixelsPerUnit) : 0f;
    }

    private void LateUpdate()
    {
        if (cameraTransform == null || _tiles == null || _tiles.Length == 0) return;

        // Camera X in this layer's local space
        float camLocalX = transform.InverseTransformPoint(cameraTransform.position).x;

        // Which tile index is just left of camera?
        int idx = Mathf.FloorToInt(camLocalX / _stepLocal);

        // Place tiles at idx, idx+1, idx+2... (exact positions, no drift)
        for (int i = 0; i < _tiles.Length; i++)
        {
            float x = (idx + i) * _stepLocal;
            SetLocalX(_tiles[i], x);

            if (pixelSnap && _unitLocal > 0f)
                SnapLocal(_tiles[i], _unitLocal);
        }
    }

    private static void SetLocalX(Transform t, float x)
    {
        Vector3 p = t.localPosition;
        p.x = x;
        t.localPosition = p;
    }

    private static void SnapLocal(Transform t, float unit)
    {
        Vector3 p = t.localPosition;
        p.x = Mathf.Round(p.x / unit) * unit;
        p.y = Mathf.Round(p.y / unit) * unit;
        t.localPosition = p;
    }
}
