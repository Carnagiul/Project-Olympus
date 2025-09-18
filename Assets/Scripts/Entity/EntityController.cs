// EntityController.cs
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class EntityController : MonoBehaviour
{
    // ——— Réfs composées
    public Health Health { get; private set; }
    public DamageReceiver DamageReceiver { get; private set; }
    public EntityStats Stats { get; private set; }

    // ——— Events
    [Header("Events")]
    public UnityEvent<float, float> OnHealthChanged; // proxy de Health si tu veux
    public UnityEvent OnDeath;
    [Header("Events (Combat)")]
    public UnityEvent<EntityController> OnKilledBy; // tueur (peut être null)

    // ——— Respawn (pour joueur)
    [Header("Respawn (Fps Only)")]
    public Vector3 respawnPosition = new Vector3(1f, 3f, 1f);
    [Min(0f)] public float respawnDelaySeconds = 3f;
    public bool hideRenderersDuringRespawn = true;
    public Behaviour[] disableDuringRespawn;

    // ---- Proxies Health
    public bool IsAlive => Health != null && Health.IsAlive;
    public float CurrentHealth => Health ? Health.Current : 0f;
    public float MaxHealth => Health ? Health.Max : 0f;

    // ---- NEW: Proxies Stats (range / cadence / projectiles / dégâts)
    public float AttackRange => Stats ? Stats.AttackRangeEff : 10f;   // depuis EntityStats
    public float AttackCooldown => Stats ? Stats.AttackCooldownEff : 1f;    // idem
    public float ProjectileSpeed => Stats ? Stats.ProjectileSpeedEff : 0f;    // 0 = hitscan
    public float DamageBase => Stats ? Stats.BaseDamage : 15f;
    public float DamageEffective => Stats ? Stats.DamageEff : DamageBase;
    public ArmorType ArmorType => Stats ? (ArmorType)(int)Stats.ArmorType : ArmorType.None;
    public DamageType DamageType => Stats ? (DamageType)(int)Stats.DamageType : DamageType.Kinetic;

    protected virtual void Awake()
    {
        OnHealthChanged ??= new UnityEvent<float, float>();
        OnDeath ??= new UnityEvent();
        OnKilledBy ??= new UnityEvent<EntityController>();

        Health = GetComponent<Health>();
        DamageReceiver = GetComponent<DamageReceiver>();
        Stats = GetComponent<EntityStats>();

        // Wiring optionnel : repropage les events
        if (Health)
        {
            Health.OnHealthChanged.AddListener((c, m) => OnHealthChanged.Invoke(c, m));
            Health.OnDeath.AddListener(() => { OnDeath.Invoke(); OnKilled(); });
        }
    }

    // Appelé par DamageReceiver quand CETTE entité meurt
    public void NotifyKilledBy(EntityController killer)
    {
        OnKilledBy.Invoke(killer);
    }

    // API pratique : déléguer un tir à la cible sans exposer ta mécanique
    public void DealDamage(EntityController target, float amount, DamageType type, Vector3 hitPoint = default, Vector3 hitNormal = default, bool crit = false)
    {
        if (!target) return;
        var receiver = target.GetComponent<DamageReceiver>();
        if (!receiver) return;
        receiver.ApplyDamage(new DamageInfo(this, Mathf.Max(0f, amount), type, hitPoint, hitNormal, crit));
    }

    // ——— Respawn joueur / destruction
    private IEnumerator RespawnPlayerRoutine()
    {
        var cc = GetComponent<CharacterController>();

        // 1) désactiver contrôles
        List<Behaviour> toDisable = new List<Behaviour>();
        if (disableDuringRespawn != null && disableDuringRespawn.Length > 0) toDisable.AddRange(disableDuringRespawn);
        else
        {
            var b1 = GetComponent<FpsController>(); if (b1) toDisable.Add(b1);
            var b2 = GetComponentInChildren<FpsLook>(true); if (b2) toDisable.Add(b2);
            var b3 = GetComponentInChildren<FpsCameraEffects>(true); if (b3) toDisable.Add(b3);
            var b4 = GetComponentInChildren<FpsAudio>(true); if (b4) toDisable.Add(b4);
        }
        foreach (var b in toDisable) if (b) b.enabled = false;
        if (cc) cc.enabled = false;

        // 2) masquer rendu
        List<Renderer> hidden = null;
        if (hideRenderersDuringRespawn)
        {
            hidden = new List<Renderer>(GetComponentsInChildren<Renderer>(true));
            foreach (var r in hidden) r.enabled = false;
        }

        // 3) attendre
        float t = Mathf.Max(0f, respawnDelaySeconds);
        if (t > 0f) yield return new WaitForSeconds(t);

        // 4) téléporter + reset santé
        transform.position = respawnPosition;
        if (Health && Stats)
        {
            Health.SetMax(Stats.MaxHealth, refill: true);
        }

        // 5) réactiver
        if (hidden != null) foreach (var r in hidden) r.enabled = true;
        if (cc) cc.enabled = true;
        foreach (var b in toDisable) if (b) b.enabled = true;
    }

    protected virtual void OnKilled()
    {
        // Joueur: respawn
        if (this is FpsController)
        {
            StartCoroutine(RespawnPlayerRoutine());
            return;
        }
        // Autres: destroy
        Destroy(gameObject);
    }
}

// ReadOnly attribute pour l’inspecteur si besoin
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
