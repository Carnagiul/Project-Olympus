using UnityEngine;

[DisallowMultipleComponent]
public class EntityGiantifier : MonoBehaviour, IGiantifiable
{
    [Header("Refs")]
    [SerializeField] private EntityStats stats;           // ton composant existant
    [SerializeField] private Transform scaleTarget;       // si null => this.transform

    [Header("Options")]
    [Tooltip("Renommer l'entité et/ou tagger quand elle devient géante.")]
    public bool annotateAsGiant = true;

    [Tooltip("Conserver le scale de base pour pouvoir faire un reset propre.")]
    public bool allowScaleReset = true;

    // Runtime
    private EntityStatsAsset _originalAsset;     // référence projet (non clonée)
    private EntityStatsAsset _giantClone;        // clone unique appliqué à cette instance
    private Vector3 _baseScale;                  // pour reset scale
    private bool _isGiantApplied = false;

    private void Awake()
    {
        if (!stats) stats = GetComponent<EntityStats>();
        if (!scaleTarget) scaleTarget = transform;
        if (allowScaleReset) _baseScale = scaleTarget.localScale;
    }

    // === IGiantifiable : compat PortalSpawner ===
    public void ApplyGiantMultipliers(float healthMult, float damageMult, float speedMult, float sizeMult)
        => ApplyGiantMultipliers(new GiantMultipliers
        {
            healthMult = healthMult,
            damageMult = damageMult,
            speedMult = speedMult,
            sizeMult = sizeMult
        });

    // === IGiantifiable : version structurée ===
    public void ApplyGiantMultipliers(GiantMultipliers m)
    {
        if (stats == null || stats.preset == null) return;

        // 1) Mémorise l’original au premier passage
        if (!_isGiantApplied)
        {
            _originalAsset = stats.preset; // asset projet (ne pas modifier)
            _isGiantApplied = true;
        }

        // 2) (Re)crée un clone propre (on peut recréer à chaque appel pour éviter cumul)
        _giantClone = ScriptableObject.Instantiate(_originalAsset);

        // 3) Applique les multiplicateurs au CLONE (local à l'instance)
        if (m.healthMult > 0f)
            _giantClone.maxHealth = Mathf.Max(1f, _giantClone.maxHealth * m.healthMult);

        if (m.damageMult > 0f)
            _giantClone.baseDamage = Mathf.Max(0f, _giantClone.baseDamage * m.damageMult);

        // Vitesse d’attaque : speedMult < 1 => plus lent => cooldown plus grand
        if (m.speedMult > 0f)
        {
            _giantClone.attackCooldown = Mathf.Max(0f, _giantClone.attackCooldown * (1f / m.speedMult));
            if (_giantClone.projectileSpeed > 0f)
                _giantClone.projectileSpeed = Mathf.Max(0f, _giantClone.projectileSpeed * m.speedMult);
        }

        // 4) Applique le clone à l’instance
        stats.SetPreset(_giantClone, invokeEvent: true);

        // 5) Scale visuel optionnel
        if (m.sizeMult > 0f)
        {
            if (!allowScaleReset && !_isGiantApplied)
                _baseScale = scaleTarget.localScale; // garde une fois, au cas où

            scaleTarget.localScale = _baseScale * m.sizeMult;
        }

        // 6) Annotation facultative
        if (annotateAsGiant)
        {
            if (gameObject.tag == "Untagged") gameObject.tag = "Giant";
            if (!gameObject.name.EndsWith(" [GIANT]")) gameObject.name += " [GIANT]";
        }
    }

    /// <summary>
    /// Revient au preset et au scale d’origine (utile quand l’ennemi “perd” l’état géant
    /// ou pour réutiliser le même objet via pooling).
    /// </summary>
    public void ResetToBase()
    {
        if (stats && _originalAsset)
            stats.SetPreset(_originalAsset, invokeEvent: true);

        if (allowScaleReset && scaleTarget)
            scaleTarget.localScale = _baseScale;

        // Reset d’étiquettes éventuelles (optionnel)
        if (annotateAsGiant && gameObject.name.EndsWith(" [GIANT]"))
            gameObject.name = gameObject.name.Replace(" [GIANT]", "");

        _giantClone = null;
        _isGiantApplied = false;
    }
}
