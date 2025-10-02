using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class TopbarHUD : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private UIDocument ui;

    // Caches UI
    private VisualElement root;

    private VisualElement hpFill, manaFill, staFill;
    private Label hpVal, manaVal, staVal;

    private Label goldAmount;
    private Label serverTime, serverPing;
    private Label zoneTitle, zoneSub;

    // Coroutines d’animation (une par barre)
    private Coroutine hpAnim, manaAnim, staAnim;

    private void OnEnable()
    {
        if (ui == null) ui = GetComponent<UIDocument>();
        root = ui != null ? ui.rootVisualElement : null;

        if (root == null)
        {
            Debug.LogError("[TopbarHUD] UIDocument/root introuvable.");
            return;
        }

        // --- Query par name (selon les noms qu’on a utilisés en UXML) ---
        hpFill = root.Q<VisualElement>("hp-fill");
        manaFill = root.Q<VisualElement>("mana-fill");
        staFill = root.Q<VisualElement>("sta-fill");

        hpVal = root.Q<Label>("hp-val");
        manaVal = root.Q<Label>("mana-val");
        staVal = root.Q<Label>("sta-val");

        goldAmount = root.Q<Label>("gold-amount");

        serverTime = root.Q<Label>("server-time");
        serverPing = root.Q<Label>("server-ping");

        zoneTitle = root.Q<Label>("zone-title");
        zoneSub = root.Q<Label>("zone-sub");
    }

    // =========================
    // === Setters unitaires ===
    // =========================

    /// <summary>Met à jour la barre de HP (0..1). Option d’animation lissée.</summary>
    public void SetHp(float pct, float animationDuration = 0f)
    {
        hpAnim = SetPctInternal(hpFill, hpVal, pct, animationDuration, hpAnim);
    }

    /// <summary>Met à jour la barre de Mana (0..1). Option d’animation lissée.</summary>
    public void SetMana(float pct, float animationDuration = 0f)
    {
        manaAnim = SetPctInternal(manaFill, manaVal, pct, animationDuration, manaAnim);
    }

    /// <summary>Met à jour la barre d’Endurance (0..1). Option d’animation lissée.</summary>
    public void SetSta(float pct, float animationDuration = 0f)
    {
        staAnim = SetPctInternal(staFill, staVal, pct, animationDuration, staAnim);
    }

    /// <summary>Met à jour l’or.</summary>
    public void SetGold(int amount)
    {
        if (goldAmount != null) goldAmount.text = amount.ToString();
    }

    /// <summary>Met à jour l’heure/phase serveur.</summary>
    public void SetServerTime(string text)
    {
        if (serverTime != null) serverTime.text = text;
    }

    /// <summary>Met à jour le ping.</summary>
    public void SetPing(int ms)
    {
        if (serverPing != null) serverPing.text = ms + " ms";
    }

    /// <summary>Met à jour la zone (titre + sous-titre).</summary>
    public void SetZone(string title, string sub)
    {
        if (zoneTitle != null) zoneTitle.text = title;
        if (zoneSub != null) zoneSub.text = sub;
    }

    // =========================
    // === Helpers internes  ===
    // =========================

    private Coroutine SetPctInternal(VisualElement fill, Label percentLabel, float pct, float duration, Coroutine running)
    {
        if (fill == null) return null;

        pct = Mathf.Clamp01(pct);

        // Stop une anim en cours pour cette barre
        if (running != null) StopCoroutine(running);

        if (duration <= 0f)
        {
            // Update direct
            fill.style.width = new Length(pct * 100f, LengthUnit.Percent);
            if (percentLabel != null) percentLabel.text = Mathf.RoundToInt(pct * 100f) + "%";
            return null;
        }
        else
        {
            // Anim lissée
            return StartCoroutine(AnimateBar(fill, percentLabel, pct, duration));
        }
    }

    private IEnumerator AnimateBar(VisualElement fill, Label percentLabel, float targetPct, float duration)
    {
        // Récupère la largeur actuelle (en %) si possible
        float start = 0f;

        // UI Toolkit ne renvoie pas toujours la valeur en pourcentage; on stocke en cache via style.width.value
        // Si la valeur n’est pas exploitable, on part de 0.
        var currentWidth = fill.resolvedStyle.width; // pixels résolus
        var parentWidth = fill.parent != null ? fill.parent.resolvedStyle.width : 0f;

        if (parentWidth > 0f)
            start = Mathf.Clamp01(currentWidth / parentWidth);
        else
            start = 0f;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            // Lissage (easeInOut)
            float v = Mathf.SmoothStep(start, targetPct, k);

            fill.style.width = new Length(v * 100f, LengthUnit.Percent);
            if (percentLabel != null) percentLabel.text = Mathf.RoundToInt(v * 100f) + "%";

            yield return null;
        }

        // Snap final
        fill.style.width = new Length(targetPct * 100f, LengthUnit.Percent);
        if (percentLabel != null) percentLabel.text = Mathf.RoundToInt(targetPct * 100f) + "%";
    }

    // =========================
    // === Exemples de test  ===
    // =========================

    [ContextMenu("Demo: valeurs de départ")]
    private void DemoInit()
    {
        SetGold(90);
        SetZone("Forêt des Ombres", "Zone 3");
        SetServerTime("12:34 | J2 - Crépuscule");
        SetPing(42);

        SetHp(0.72f);
        SetMana(0.45f);
        SetSta(0.83f);
    }

    [ContextMenu("Demo: animations")]
    private void DemoAnim()
    {
        SetHp(Random.Range(0.2f, 1f), 0.35f);
        SetMana(Random.Range(0.2f, 1f), 0.35f);
        SetSta(Random.Range(0.2f, 1f), 0.35f);
    }
}
