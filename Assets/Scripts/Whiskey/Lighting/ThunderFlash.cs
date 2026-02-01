using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class ThunderFlash : MonoBehaviour
{
    public Light2D lightningLight;
    public float flashIntensity = 1.5f;
    public AudioSource thunderSource;   // AudioSource组件
    public AudioClip thunderClip;       // 雷声音频

    private void Awake()
    {
        // 兜底：没拖就自动在自己身上找
        if (thunderSource == null)
            thunderSource = GetComponent<AudioSource>();

        if (lightningLight != null)
            lightningLight.intensity = 0;
    }

    void Start()
    {
        StartCoroutine(FlashLoop());
    }

    IEnumerator FlashLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(4f, 8f));

            // 闪一下
            if (lightningLight != null)
                lightningLight.intensity = flashIntensity;

            yield return new WaitForSeconds(0.1f);

            if (lightningLight != null)
                lightningLight.intensity = 0;

            // 播放雷声
            if (thunderSource != null && thunderClip != null)
                thunderSource.PlayOneShot(thunderClip);
            else
                Debug.LogWarning("ThunderFlash: thunderSource 或 thunderClip 没有绑定");
        }
    }
}