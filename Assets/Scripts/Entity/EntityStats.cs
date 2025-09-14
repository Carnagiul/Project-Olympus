using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class EntityStats : MonoBehaviour
{
    [Header("Preset (Base Data)")]
    public EntityStatsAsset preset;

    [Header("Runtime Modifiers")]
    [Tooltip("Multiplicateur global des dégâts (ex: buffs). 1 = neutre.")]
    public float damageMultiplier = 1f;

    [Tooltip("Bonus d'armure à ajouter runtime (ex: aura, buff).")]
    public float armorBonus = 0f;

    [Tooltip("Multiplicateur de portée (ex: upgrade tour). 1 = neutre.")]
    public float rangeMultiplier = 1f;

    [Tooltip("Multiplicateur de cadence (appliqué au cooldown). <1 = plus rapide.")]
    public float cooldownMultiplier = 1f;

    // (Optionnel) notifie quand le preset change
    public UnityEvent<EntityStatsAsset> OnPresetChanged;

    // ======= Propriétés “prêtes à l’emploi” =======

    // Défense
    public float MaxHealth => preset ? Mathf.Max(1f, preset.maxHealth) : 100f;
    public float ArmorBase => preset ? Mathf.Max(0f, preset.armor) : 0f;
    public float ArmorEff => Mathf.Max(0f, ArmorBase + Mathf.Max(0f, armorBonus));
    public EntityStatsAsset.ArmorType ArmorType => preset ? preset.armorType : EntityStatsAsset.ArmorType.None;

    // Offense
    public float BaseDamage => preset ? Mathf.Max(0f, preset.baseDamage) : 15f;
    public float DamageEff => Mathf.Max(0f, BaseDamage) * Mathf.Max(0f, damageMultiplier);
    public EntityStatsAsset.DamageType DamageType => preset ? preset.damageType : EntityStatsAsset.DamageType.Kinetic;

    // Portée & cadence
    public float AttackRangeBase => preset ? Mathf.Max(0f, preset.attackRange) : 10f;
    public float AttackRangeEff => Mathf.Max(0f, AttackRangeBase * Mathf.Max(0f, rangeMultiplier));
    public float AttackCooldownBase => preset ? Mathf.Max(0f, preset.attackCooldown) : 1f;
    public float AttackCooldownEff => Mathf.Max(0f, AttackCooldownBase * Mathf.Max(0f, cooldownMultiplier));
    public float ProjectileSpeedBase => preset ? Mathf.Max(0f, preset.projectileSpeed) : 0f;
    public float ProjectileSpeedEff => Mathf.Max(0f, ProjectileSpeedBase); // ajoute un multiplier si besoin

    // Helpers
    public void SetPreset(EntityStatsAsset newPreset, bool invokeEvent = true)
    {
        preset = newPreset;
        if (invokeEvent) OnPresetChanged?.Invoke(preset);
    }
}
