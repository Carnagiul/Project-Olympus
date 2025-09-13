using UnityEngine;

[DisallowMultipleComponent]
public class EntityStats : MonoBehaviour
{
    [Header("Preset (Base Data)")]
    public EntityStatsAsset preset;

    [Header("Runtime Modifiers")]
    [Tooltip("Multiplicateur global des d�g�ts (ex: buffs). 1 = neutre.")]
    public float damageMultiplier = 1f;
    [Tooltip("Bonus d'armure � ajouter runtime (ex: aura, buff).")]
    public float armorBonus = 0f;

    // ======= Propri�t�s �pr�tes � l�emploi� pour le code =======

    public float MaxHealth => preset ? Mathf.Max(1f, preset.maxHealth) : 100f;
    public float ArmorBase => preset ? Mathf.Max(0f, preset.armor) : 0f;
    public float ArmorEff => Mathf.Max(0f, ArmorBase + Mathf.Max(0f, armorBonus));

    public float BaseDamage => preset ? Mathf.Max(0f, preset.baseDamage) : 15f;
    public float DamageEff => Mathf.Max(0f, BaseDamage) * Mathf.Max(0f, damageMultiplier);

    public EntityStatsAsset.ArmorType ArmorType => preset ? preset.armorType : EntityStatsAsset.ArmorType.None;
    public EntityStatsAsset.DamageType DamageType => preset ? preset.damageType : EntityStatsAsset.DamageType.Kinetic;

    // Helpers pour changer de preset � chaud (ex: �volution d�ennemi)
    public void SetPreset(EntityStatsAsset newPreset)
    {
        preset = newPreset;
    }
}
