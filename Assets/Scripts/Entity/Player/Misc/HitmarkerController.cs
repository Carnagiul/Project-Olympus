// HitmarkerController.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[DisallowMultipleComponent]
public class HitmarkerController : MonoBehaviour
{
    public CanvasGroup group;          // si null: on l'ajoute
    public float showAlpha = 1f;
    public float fadeTime = 0.15f;
    public float holdTime = 0.05f;

    void Awake()
    {
        if (!group)
        {
            group = GetComponent<CanvasGroup>();
            if (!group) group = gameObject.AddComponent<CanvasGroup>();
        }
        group.alpha = 0f;
    }

    public void Ping()
    {
        StopAllCoroutines();
        StartCoroutine(PingRoutine());
    }

    IEnumerator PingRoutine()
    {
        // up
        group.alpha = showAlpha;
        yield return new WaitForSeconds(holdTime);

        // fade
        float t = 0f;
        float start = group.alpha;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(start, 0f, t / fadeTime);
            yield return null;
        }
        group.alpha = 0f;
    }
}
