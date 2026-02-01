using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal;

public class ThunderFlash : MonoBehaviour
{
    public Light2D lightningLight;
    public float flashIntensity = 1.5f;

    void Start()
    {
        StartCoroutine(FlashLoop());
    }

    IEnumerator FlashLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(4f, 8f));

            lightningLight.intensity = flashIntensity;
            yield return new WaitForSeconds(0.1f);

            lightningLight.intensity = 0;
        }
    }
}
