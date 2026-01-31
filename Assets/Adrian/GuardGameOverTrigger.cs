using UnityEngine;
using UnityEngine.SceneManagement;

public class GuardGameOverTrigger : MonoBehaviour
{
    [Tooltip("必须与玩家的Tag完全一致（大小写敏感）")]
    public string playerTag = "Player";

    [Tooltip("触发后是否立刻重载当前场景（当作GameOver）")]
    public bool reloadSceneOnGameOver = true;

    private bool triggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag(playerTag)) return;

        triggered = true;

        // TODO: 你也可以在这里播放音效/动画
        Debug.Log("GAME OVER: hit by guard/light");

        if (reloadSceneOnGameOver)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
