using System;
using System.Collections;
using System.Collections.Generic;
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
    public bool IsAlive = true;

    // Événements
    [Header("Events")]
    public UnityEvent<float, float> OnHealthChanged;         // (current, max)
    public UnityEvent<float> OnDamaged;                      // (finalDamage)
    public UnityEvent<float> OnHealed;                       // (healAmount)
    public UnityEvent OnDeath;

    [Header("Respawn (pour FpsController uniquement)")]
    public Vector3 respawnPosition = new Vector3(1f, 3f, 1f);
    [Min(0f)] public float respawnDelaySeconds = 3f;
    [Tooltip("Masquer le mesh pendant le délai de respawn")]
    public bool hideRenderersDuringRespawn = true;
    [Tooltip("Composants à désactiver pendant le délai (si laissés vides, on essaie d’auto-détecter)")]
    public Behaviour[] disableDuringRespawn;

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
        if (!IsAlive)
        {
            Debug.Log("EntityController.Kill() called on already dead entity!");
            return;
        }
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

    private IEnumerator RespawnPlayerRoutine()
    {
        // Références utiles
        IsAlive = false;
        var cc = GetComponent<CharacterController>();
        var fps = GetComponent<FpsController>();

        // 1) Désactiver contrôles / CC
        List<Behaviour> toDisable = new List<Behaviour>();
        if (disableDuringRespawn != null && disableDuringRespawn.Length > 0)
            toDisable.AddRange(disableDuringRespawn);
        else
        {
            // Auto-détection minimaliste
            var b1 = GetComponent<FpsController>(); if (b1) toDisable.Add(b1);
            var b2 = GetComponentInChildren<FpsLook>(true); if (b2) toDisable.Add(b2);
            var b3 = GetComponentInChildren<FpsCameraEffects>(true); if (b3) toDisable.Add(b3);
            var b4 = GetComponentInChildren<FpsAudio>(true); if (b4) toDisable.Add(b4);
        }

        foreach (var b in toDisable) if (b) b.enabled = false;
        if (cc) cc.enabled = false;

        // 2) Masquer le rendu (optionnel)
        List<Renderer> hidden = null;
        if (hideRenderersDuringRespawn)
        {
            hidden = new List<Renderer>(GetComponentsInChildren<Renderer>(true));
            foreach (var r in hidden) r.enabled = false;
        }

        // 3) Attendre le délai
        float t = Mathf.Max(0f, respawnDelaySeconds);
        if (t > 0f) yield return new WaitForSeconds(t);
        IsAlive = true;
        // 4) Téléporter + reset santé
        transform.position = respawnPosition;
        // (facultatif) annuler toute vitesse physique externe si tu en utilises

        // Reset HP et notifier UI
        currentHealth = maxHealth;
        OnHealthChanged.Invoke(currentHealth, maxHealth);

        // 5) Réactiver rendu / CC / contrôles
        if (hidden != null) foreach (var r in hidden) r.enabled = true;
        if (cc) cc.enabled = true;
        foreach (var b in toDisable) if (b) b.enabled = true;

        // (facultatif) réinitialiser le headbob/FOV/états si nécessaire via des méthodes publiques
    }


    protected virtual void OnKilled()
    {
        // Joueur: respawn avec délai
        if (this is FpsController)
        {
            StartCoroutine(RespawnPlayerRoutine());
            return;
        }

        // Autres entités: supprimer
        Destroy(gameObject);
    }


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
