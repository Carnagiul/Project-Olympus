using UnityEngine;

[DisallowMultipleComponent]
public class DamageReceiver : MonoBehaviour
{
    [Header("Refs")]
    public Health health;
    public EntityStats stats; // prescrit: lit Armor/Damage types depuis EntityStats

    [Header("Balance")]
    [Tooltip("Constante K de la mitigation lissée: armor / (armor + K)")]
    public float armorK = 100f;

    [Header("Debug")]
    [SerializeField] private EntityController lastDamager;
    [SerializeField] private float lastHitTime;

    private EntityController owner; // le controller de CET objet (pour OnKilledBy)

    void Awake()
    {
        if (!health) health = GetComponent<Health>();
        if (!stats) stats = GetComponent<EntityStats>();
        owner = GetComponent<EntityController>();

        // Assurer cohérence Health max = Stats.MaxHealth
        if (health && stats) health.SetMax(stats.MaxHealth, refill: health.Current <= 0f);
        if (health) health.OnDeath.AddListener(OnOwnerDeath);
    }

    void OnDestroy()
    {
        if (health) health.OnDeath.RemoveListener(OnOwnerDeath);
    }

    public void ApplyDamage(DamageInfo info)
    {
        if (health == null || !health.IsAlive) return;

        // Memoriser l'attaquant
        if (info.source != null && info.source != owner)
        {
            lastDamager = info.source;
            lastHitTime = Time.time;
        }

        // Mapper ArmorType depuis les stats
        var arm = stats ? MapArmor(stats.ArmorType) : ArmorType.None;

        // 1) Multiplicateur type vs armure
        float typeMult = GetTypeMultiplier(info.damageType, arm);

        // 2) Mitigation d’armure lissée
        float armorEff = stats ? stats.ArmorEff : 0f;
        float armorMitigation = Mathf.Clamp01(armorEff / (armorEff + Mathf.Max(1e-3f, armorK)));

        // 3) Dégâts finaux
        float raw = info.amount * typeMult;
        float final = raw * (1f - armorMitigation);
        if (info.isCritical) final *= 1.5f;

        // 4) Appliquer au Health
        health.ApplyFinalDamage(final);
    }

    // Notifié quand CETTE entité meurt → remonter le tueur
    private void OnOwnerDeath()
    {
        if (owner != null)
            owner.NotifyKilledBy(lastDamager);
    }

    // Table intégrée (mêmes valeurs que ta version précédente)
    private static float GetTypeMultiplier(DamageType dmg, ArmorType arm)
    {
        switch (dmg)
        {
            case DamageType.Kinetic:
                switch (arm)
                { case ArmorType.None: return 1f; case ArmorType.Light: return 1f; case ArmorType.Medium: return 0.9f; case ArmorType.Heavy: return 0.8f; case ArmorType.Shield: return 0.7f; case ArmorType.Energy: return 0.6f; }
                break;
            case DamageType.Piercing:
                switch (arm)
                { case ArmorType.None: return 1.1f; case ArmorType.Light: return 1.2f; case ArmorType.Medium: return 1f; case ArmorType.Heavy: return 0.9f; case ArmorType.Shield: return 0.7f; case ArmorType.Energy: return 0.6f; }
                break;
            case DamageType.Explosive:
                switch (arm)
                { case ArmorType.None: return 1.2f; case ArmorType.Light: return 1.1f; case ArmorType.Medium: return 1f; case ArmorType.Heavy: return 1.1f; case ArmorType.Shield: return 0.8f; case ArmorType.Energy: return 0.7f; }
                break;
            case DamageType.Fire:
                switch (arm)
                { case ArmorType.None: return 1.1f; case ArmorType.Light: return 1f; case ArmorType.Medium: return 0.9f; case ArmorType.Heavy: return 0.8f; case ArmorType.Shield: return 0.9f; case ArmorType.Energy: return 0.7f; }
                break;
            case DamageType.Electric:
                switch (arm)
                { case ArmorType.None: return 1f; case ArmorType.Light: return 0.9f; case ArmorType.Medium: return 0.9f; case ArmorType.Heavy: return 0.8f; case ArmorType.Shield: return 1.3f; case ArmorType.Energy: return 1.1f; }
                break;
            case DamageType.Toxic:
                switch (arm)
                { case ArmorType.None: return 1.2f; case ArmorType.Light: return 1.1f; case ArmorType.Medium: return 1f; case ArmorType.Heavy: return 0.9f; case ArmorType.Shield: return 0.6f; case ArmorType.Energy: return 0.8f; }
                break;
            case DamageType.Cold:
                switch (arm)
                { case ArmorType.None: return 1f; case ArmorType.Light: return 0.9f; case ArmorType.Medium: return 0.9f; case ArmorType.Heavy: return 0.8f; case ArmorType.Shield: return 0.8f; case ArmorType.Energy: return 1f; }
                break;
        }
        return 1f;
    }

    // ==== Mappers entre enums Asset et globaux ====
    private static ArmorType MapArmor(EntityStatsAsset.ArmorType src) => (ArmorType)(int)src;
    private static DamageType MapDamage(EntityStatsAsset.DamageType src) => (DamageType)(int)src;
}
