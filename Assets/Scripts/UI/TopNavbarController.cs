using Assets.Scripts.UI;
using UnityEngine;
using UnityEngine.UIElements;
public class TopNavbarController : MonoBehaviour
{
    public UIDocument uiDocument;
    public int gold = 90; public float hp = 72, hpMax = 100, mana = 45, manaMax = 100, endu = 83, enduMax = 100;
    void OnEnable() { if (!uiDocument) uiDocument = GetComponent<UIDocument>(); Apply(); }
    public void Apply()
    {
        if (!uiDocument) return; var root = uiDocument.rootVisualElement; if (root == null) return;
        var goldPill = root.Q<GoldPill>(); if (goldPill != null) goldPill.SetAmount(gold);
        var hpBar = root.Q<HudBar>(null, new[] { "hudbar", "hudbar--hp" }); if (hpBar != null) hpBar.Set(hp, hpMax);
        var manaBar = root.Q<HudBar>(null, new[] { "hudbar", "hudbar--mana" }); if (manaBar != null) manaBar.Set(mana, manaMax);
        var enduBar = root.Q<HudBar>(null, new[] { "hudbar", "hudbar--endu" }); if (enduBar != null) enduBar.Set(endu, enduMax);
    }
}