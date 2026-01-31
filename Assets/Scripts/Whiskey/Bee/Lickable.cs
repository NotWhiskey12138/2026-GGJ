using UnityEngine;

public class Lickable : MonoBehaviour
{
    [Tooltip("被舔后是销毁，还是隐藏")]
    public bool destroyOnLick = true;

    public void OnLicked()
    {
        if (destroyOnLick) Destroy(gameObject);
        else gameObject.SetActive(false);
    }
}