// CrosshairHighlighter.cs
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CrosshairHighlighter : MonoBehaviour
{
    [Header("Refs")]
    public AimHoverDetector hover;
    public Graphic crosshairGraphic; // Image/Text/etc.

    [Header("Colors")]
    public Color normal = Color.white;
    public Color onEnemy = Color.red;

    void Awake()
    {
        if (!hover) hover = FindObjectOfType<AimHoverDetector>();
    }

    void OnEnable()
    {
        if (!hover) return;
        hover.HoverEnemy += OnHoverEnemy;
        hover.LostEnemy += OnLostEnemy;
    }

    void OnDisable()
    {
        if (!hover) return;
        hover.HoverEnemy -= OnHoverEnemy;
        hover.LostEnemy -= OnLostEnemy;
    }

    void OnHoverEnemy(EntityController ec, RaycastHit hit)
    {
        if (crosshairGraphic) crosshairGraphic.color = onEnemy;
    }

    void OnLostEnemy()
    {
        if (crosshairGraphic) crosshairGraphic.color = normal;
    }
}
