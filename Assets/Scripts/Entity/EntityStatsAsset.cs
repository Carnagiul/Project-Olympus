using UnityEngine;

[CreateAssetMenu(fileName = "EntityStats", menuName = "FPS Tower Def/Entity Stats Asset")]
public class EntityStatsAsset : ScriptableObject
{
    public enum ArmorType { None, Light, Medium, Heavy, Shield, Energy }
    public enum DamageType { Kinetic, Piercing, Explosive, Fire, Electric, Toxic, Cold }

    [Header("Defense")]
    [Min(1f)] public float maxHealth = 100f;
    [Min(0f)] public float armor = 0f;
    public ArmorType armorType = ArmorType.None;

    [Header("Offense")]
    [Min(0f)] public float baseDamage = 15f;
    public DamageType damageType = DamageType.Kinetic;

    [Header("Range & Attack")]
    [Tooltip("Distance maximale d’attaque (mètres).")]
    [Min(0f)] public float attackRange = 10f;

    [Tooltip("Délai (secondes) entre deux attaques.")]
    [Min(0f)] public float attackCooldown = 1f;

    [Tooltip("Vitesse des projectiles si utilisé (m/s). 0 = instantané (hitscan).")]
    [Min(0f)] public float projectileSpeed = 0f;
}
