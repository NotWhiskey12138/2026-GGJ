using UnityEngine;
using UnityEngine.SceneManagement;
using NPCSystem;
using MaskSystem.Domain;
using MaskSystem;

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
        NPC npc = other.GetComponent<NPC>();
        if (npc != null)
        {
            if (!npc.IsPossessed) return;
        }
        else if (!other.CompareTag(playerTag))
        {
            return;
        }

        triggered = true;

        // TODO: 你也可以在这里播放音效/动画
        Debug.Log("GAME OVER: hit by guard/light");

        if (reloadSceneOnGameOver)
        {
            // Reset mask state so it won't stay attached after reload
            MaskDomain.Instance.ForceReset();
            var mask = FindObjectOfType<Mask>();
            if (mask != null)
            {
                mask.ResetToSpawn();
            }
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
