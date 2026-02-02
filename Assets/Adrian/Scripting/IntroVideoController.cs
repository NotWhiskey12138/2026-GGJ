using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class IntroVideoController : MonoBehaviour
{
    [Header("References")]
    public VideoPlayer player;

    private const string NEXT_SCENE = "Level-Tutorial";

    [Header("Skip")]
    public bool allowSkip = true;

    private bool _loading;

    private void Awake()
    {
        if (player == null) player = GetComponent<VideoPlayer>();
        if (player != null) player.loopPointReached += OnVideoEnd;
    }

    private void OnDestroy()
    {
        if (player != null) player.loopPointReached -= OnVideoEnd;
    }

    private void Update()
    {
        if (!allowSkip || _loading) return;

        // 任意键/点击跳过
        if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
        {
            LoadNext();
        }
    }

    private void OnVideoEnd(VideoPlayer vp)
    {
        LoadNext();
    }

    private void LoadNext()
    {
        if (_loading) return;
        _loading = true;

        if (player != null) player.Stop();
        SceneManager.LoadScene(NEXT_SCENE);
    }
}

