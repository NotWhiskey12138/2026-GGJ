using UnityEngine;

/// <summary>
/// Simple 2D parallax for SpriteRenderer layers.
/// Parameter "parallax" is intuitive:
///  - 0   = very far (almost stuck to camera, moves slow on screen)
///  - 1   = very near (almost world-locked, moves fast on screen)
/// Works best when the camera follows the player.
/// </summary>
[ExecuteAlways]
public class ParallaxLayer2D : MonoBehaviour
{
    [Header("References")]
    public Transform cameraTransform;

    [Header("Parallax Amount (0 = far, 1 = near)")]
    [Range(0f, 1f)] public float parallax = 0.3f;

    [Header("Axis")]
    public bool affectX = true;
    public bool affectY = false;

    [Header("Optional smoothing")]
    [Tooltip("0 = no smoothing; higher = smoother but laggier")]
    public float smoothing = 0f;

    private Vector3 _startPos;
    private Vector3 _camStartPos;

    private void OnEnable()
    {
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        CacheStart();
    }

    private void CacheStart()
    {
        _startPos = transform.position;
        _camStartPos = (cameraTransform != null) ? cameraTransform.position : Vector3.zero;
    }

    private void LateUpdate()
    {
        if (cameraTransform == null) return;

        // "follow" is how much the layer follows the camera in world space.
        // far (parallax small) => follow close to 1 => screen movement small
        float follow = 1f - parallax;

        Vector3 camDelta = cameraTransform.position - _camStartPos;

        float dx = affectX ? camDelta.x * follow : 0f;
        float dy = affectY ? camDelta.y * follow : 0f;

        Vector3 target = new Vector3(_startPos.x + dx, _startPos.y + dy, _startPos.z);

        if (smoothing > 0f && Application.isPlaying)
        {
            transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-smoothing * Time.deltaTime));
        }
        else
        {
            transform.position = target;
        }
    }

#if UNITY_EDITOR
    // If you move the layer in the editor while not playing, re-cache.
    private void OnValidate()
    {
        if (!Application.isPlaying) CacheStart();
    }
#endif
}
