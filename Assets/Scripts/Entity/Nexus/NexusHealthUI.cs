using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class NexusHealthUI : MonoBehaviour
{
    [Header("Nexus (auto si vide)")]
    public Health nexusHealth;
    public string nexusTag = "Nexus";

    [Header("UI Refs")]
    public Slider slider;           // UI → Slider (min=0, max=1)
    public Image fillImage;         // Optionnel : l’Image à colorer (handle Fill / Image du Slider)
    public TMP_Text label;          // Optionnel : “HP 850 / 1000” etc.

    [Header("Style")]
    public Gradient colorByHealth;  // Optionnel : vert→jaune→rouge
    public bool smoothLerp = true;
    public float lerpSpeed = 8f;

    float target01 = 1f;
    float current01 = 1f;
    float max = 1f;

    void Awake()
    {
        // Auto-find nexus si pas assigné
        if (!nexusHealth)
        {
            var go = GameObject.FindGameObjectWithTag(nexusTag);
            if (go) nexusHealth = go.GetComponentInParent<Health>();
        }
        if (!slider)
        {
            slider = GetComponentInChildren<Slider>();
        }
    }

    void OnEnable()
    {
        if (nexusHealth)
        {
            // init
            max = Mathf.Max(1f, nexusHealth.Max);
            current01 = target01 = nexusHealth.Current / max;
            ApplyUI(force: true);

            nexusHealth.OnHealthChanged.AddListener(OnHealthChanged);
        }
        else
        {
            Debug.LogWarning("[NexusHealthUI] Aucune référence Health trouvée pour le Nexus.");
        }
    }

    void OnDisable()
    {
        if (nexusHealth)
            nexusHealth.OnHealthChanged.RemoveListener(OnHealthChanged);
    }

    void OnHealthChanged(float current, float maxHp)
    {
        max = Mathf.Max(1f, maxHp);
        target01 = Mathf.Clamp01(current / max);
        if (!smoothLerp) { current01 = target01; ApplyUI(force: true); }
    }

    void Update()
    {
        if (!smoothLerp) return;
        current01 = Mathf.MoveTowards(current01, target01, lerpSpeed * Time.deltaTime);
        ApplyUI(force: false);
    }

    void ApplyUI(bool force)
    {
        if (slider)
        {
            if (slider.minValue != 0f) slider.minValue = 0f;
            if (slider.maxValue != 1f) slider.maxValue = 1f;
            slider.value = current01;
        }

        if (fillImage && colorByHealth != null)
            fillImage.color = colorByHealth.Evaluate(current01);

        if (label)
        {
            // Affiche valeurs arrondies
            int cur = Mathf.RoundToInt(current01 * max);
            int m = Mathf.RoundToInt(max);
            label.text = $"Nexus HP {cur} / {m}";
        }
    }
}
