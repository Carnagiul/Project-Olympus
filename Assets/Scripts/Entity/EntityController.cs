using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class EntityController : MonoBehaviour
{
    // --- Types génériques et infos de coup ---
    public enum EntityArmorType { None, Light, Medium, Heavy, Shield, Energy }
    public enum EntityDamageType { Kinetic, Piercing, Explosive, Fire, Electric, Toxic, Cold }

    [Serializable]
    public struct DamageInfo
    {
        public EntityController source;
        public float amount;
        public EntityDamageType damageType;
        public Vector3 hitPoint;
        public Vector3 hitNormal;
        public bool isCritical;

        public DamageInfo(EntityController src, float amt, EntityDamageType type, Vector3 hp, Vector3 hn, bool crit = false)
        {
            source = src; amount = amt; damageType = type; hitPoint = hp; hitNormal = hn; isCritical = crit;
        }
    }

    // --- Stats de base ---
    [Header("Entity / Stats")]
    [Min(1f)] public float maxHealth = 100f;
    [Min(0f)] public float armor = 0f; // 0..∞ (mitigation par formule lissée)
    public EntityArmorType armorType = EntityArmorType.None;

    [Header("Offense")]
    [Min(0f)] public float baseDamage = 15f;
    public EntityDamageType damageType = EntityDamageType.Kinetic;

    // Santé runtime
    [SerializeField, ReadOnly] private float currentHealth;
    public float CurrentHealth => currentHealth;
    public bool IsAlive => currentHealth > 0f;

    // Événements
    [Header("Events")]
    public UnityEvent<float, float> OnHealthChanged;         // (current, max)
    public UnityEvent<float> OnDamaged;                      // (finalDamage)
    public UnityEvent<float> OnHealed;                       // (healAmount)
    public UnityEvent OnDeath;

    // --- Cycle ---
    protected virtual void Awake()
    {
        currentHealth = Mathf.Clamp(currentHealth <= 0f ? maxHealth : currentHealth, 0f, maxHealth);
        OnHealthChanged ??= new UnityEvent<float, float>();
        OnDamaged ??= new UnityEvent<float>();
        OnHealed ??= new UnityEvent<float>();
        OnDeath ??= new UnityEvent();
    }

    // --- API publique ---
    public virtual void Heal(float amount)
    {
        if (!IsAlive || amount <= 0f) return;
        float before = currentHealth;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        float healed = currentHealth - before;
        if (healed > 0f)
        {
            OnHealed.Invoke(healed);
            OnHealthChanged.Invoke(currentHealth, maxHealth);
        }
    }

    public virtual void Kill()
    {
        if (!IsAlive) return;
        currentHealth = 0f;
        OnHealthChanged.Invoke(currentHealth, maxHealth);
        OnDeath.Invoke();
        OnKilled(); // hook d’extension
    }

    /// <summary>Inflige des dégâts à une cible (utilise baseDamage/damageType de cette entité).</summary>
    public virtual void DealDamage(EntityController target, float? overrideAmount = null, EntityDamageType? overrideType = null, Vector3 hitPoint = default, Vector3 hitNormal = default, bool crit = false)
    {
        if (target == null || !IsAlive) return;
        float amt = Mathf.Max(0f, overrideAmount ?? baseDamage);
        var type = overrideType ?? damageType;
        var info = new DamageInfo(this, amt, type, hitPoint, hitNormal, crit);
        target.ApplyDamage(info);
    }

    /// <summary>Reçoit un DamageInfo et applique la mitigation, multipliers…</summary>
    public virtual void ApplyDamage(DamageInfo info)
    {
        if (!IsAlive || info.amount <= 0f) return;

        // 1) Multiplicateur type vs armure
        float typeMult = GetTypeMultiplier(info.damageType, armorType);

        // 2) Mitigation d’armure (formule lissée : armor / (armor + K))
        // K fixe l’échelle : 100 → 50% à 100 d’armure, 200 → 33% etc.
        const float K = 100f;
        float armorMitigation = Mathf.Clamp01(armor / (armor + K));

        // 3) Dégâts finaux
        float raw = info.amount * typeMult;
        float final = raw * (1f - armorMitigation);
        if (info.isCritical) final *= 1.5f;

        // 4) Applique
        float before = currentHealth;
        currentHealth = Mathf.Max(0f, currentHealth - final);

        OnDamaged.Invoke(final);
        OnHealthChanged.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
            Kill();
        else
            OnHit(info, final, before); // hook d’extension
    }

    // --- Tables de type : simple, claire, modifiable ---
    // >1 = efficace ; <1 = peu efficace ; 1 = neutre
    protected virtual float GetTypeMultiplier(EntityDamageType dmg, EntityArmorType arm)
    {
        switch (dmg)
        {
            case EntityDamageType.Kinetic:
                switch (arm)
                {
                    case EntityArmorType.None: return 1.0f;
                    case EntityArmorType.Light: return 1.0f;
                    case EntityArmorType.Medium: return 0.9f;
                    case EntityArmorType.Heavy: return 0.8f;
                    case EntityArmorType.Shield: return 0.7f;
                    case EntityArmorType.Energy: return 0.6f;
                }
                break;

            case EntityDamageType.Piercing:
                switch (arm)
                {
                    case EntityArmorType.None: return 1.1f;
                    case EntityArmorType.Light: return 1.2f;
                    case EntityArmorType.Medium: return 1.0f;
                    case EntityArmorType.Heavy: return 0.9f;
                    case EntityArmorType.Shield: return 0.7f;
                    case EntityArmorType.Energy: return 0.6f;
                }
                break;

            case EntityDamageType.Explosive:
                switch (arm)
                {
                    case EntityArmorType.None: return 1.2f;
                    case EntityArmorType.Light: return 1.1f;
                    case EntityArmorType.Medium: return 1.0f;
                    case EntityArmorType.Heavy: return 1.1f; // souffle
                    case EntityArmorType.Shield: return 0.8f;
                    case EntityArmorType.Energy: return 0.7f;
                }
                break;

            case EntityDamageType.Fire:
                switch (arm)
                {
                    case EntityArmorType.None: return 1.1f;
                    case EntityArmorType.Light: return 1.0f;
                    case EntityArmorType.Medium: return 0.9f;
                    case EntityArmorType.Heavy: return 0.8f;
                    case EntityArmorType.Shield: return 0.9f;
                    case EntityArmorType.Energy: return 0.7f; // shields/energy absorb
                }
                break;

            case EntityDamageType.Electric:
                switch (arm)
                {
                    case EntityArmorType.None: return 1.0f;
                    case EntityArmorType.Light: return 0.9f;
                    case EntityArmorType.Medium: return 0.9f;
                    case EntityArmorType.Heavy: return 0.8f;
                    case EntityArmorType.Shield: return 1.3f; // très efficace vs shield
                    case EntityArmorType.Energy: return 1.1f;
                }
                break;

            case EntityDamageType.Toxic:
                switch (arm)
                {
                    case EntityArmorType.None: return 1.2f;
                    case EntityArmorType.Light: return 1.1f;
                    case EntityArmorType.Medium: return 1.0f;
                    case EntityArmorType.Heavy: return 0.9f;
                    case EntityArmorType.Shield: return 0.6f; // shield protège bien
                    case EntityArmorType.Energy: return 0.8f;
                }
                break;

            case EntityDamageType.Cold:
                switch (arm)
                {
                    case EntityArmorType.None: return 1.0f;
                    case EntityArmorType.Light: return 0.9f;
                    case EntityArmorType.Medium: return 0.9f;
                    case EntityArmorType.Heavy: return 0.8f;
                    case EntityArmorType.Shield: return 0.8f;
                    case EntityArmorType.Energy: return 1.0f;
                }
                break;
        }
        return 1.0f;
    }

    // --- Hooks d’extension (override dans des sous-classes si besoin) ---
    protected virtual void OnHit(DamageInfo info, float finalDamage, float healthBefore) { }
    protected virtual void OnKilled() { }
}


// --- Petit attribut ReadOnly pour exposer currentHealth sans édition ---
[AttributeUsage(AttributeTargets.Field)] public class ReadOnlyAttribute : PropertyAttribute { }
#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false;
        EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = true;
    }
}
#endif
