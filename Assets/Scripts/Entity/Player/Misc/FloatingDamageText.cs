// FloatingDamageText.cs
using UnityEngine;
using TMPro;
using System.Collections;

public class FloatingDamageText : MonoBehaviour
{
    public TMP_Text label;
    public float lifetime = 0.8f;
    public Vector3 movePerSecond = new Vector3(0f, 1.2f, 0f);
    public float startScale = 1f;
    public float endScale = 0.9f;

    private float t;
    private Color baseColor;

    public void Init(float damage)
    {
        if (!label) label = GetComponentInChildren<TMP_Text>();
        if (!label) return;

        label.text = Mathf.RoundToInt(damage).ToString();
        baseColor = label.color;
        label.alpha = 1f;
        transform.localScale = Vector3.one * startScale;
        t = 0f;
    }

    void Update()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / lifetime);

        transform.position += movePerSecond * Time.deltaTime;

        float s = Mathf.Lerp(startScale, endScale, k);
        transform.localScale = Vector3.one * s;

        if (label)
        {
            var c = baseColor;
            c.a = 1f - k;
            label.color = c;
        }

        if (t >= lifetime) Destroy(gameObject);
    }
}
