// PlayerCombatEvents.cs
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerCombatEvents : MonoBehaviour
{
    [Header("Refs (auto si laissé vide)")]
    public FpsController player;          // doit porter playerCamera + equippedWeapon
    public HitscanWeapon weapon;          // si null, on prend player.equippedWeapon

    [Header("UX hooks (optionnels)")]
    public HitmarkerController hitmarker; // UI hitmarker
    public FloatingDamageSpawner damageTextSpawner; // spawner de textes dommages
    public AudioSource sfx;               // source pour jouer des sons
    public AudioClip hitClip;
    public AudioClip missClip;
    public AudioClip cooldownClip;

    void Awake()
    {
        if (!player) player = GetComponent<FpsController>();
        if (!weapon && player) weapon = player.equippedWeapon;
    }

    void OnEnable()
    {
        if (weapon == null) return;
        weapon.Fired += OnFired;
        weapon.Hit += OnHit;
        weapon.Miss += OnMiss;
        weapon.CooldownBlocked += OnCooldownBlocked;
    }

    void OnDisable()
    {
        if (weapon == null) return;
        weapon.Fired -= OnFired;
        weapon.Hit -= OnHit;
        weapon.Miss -= OnMiss;
        weapon.CooldownBlocked -= OnCooldownBlocked;
    }

    void OnFired(HitscanWeapon.FireResult r)
    {
        // Un tir est parti (cooldown OK).
    }

    void OnHit(HitscanWeapon.FireResult r)
    {
        if (hitmarker) hitmarker.Ping();
        if (damageTextSpawner) damageTextSpawner.Spawn(r.point, r.damageApplied);

        if (sfx && hitClip) sfx.PlayOneShot(hitClip, 1f);
    }

    void OnMiss(HitscanWeapon.FireResult r)
    {
        if (sfx && missClip) sfx.PlayOneShot(missClip, 0.9f);
    }

    void OnCooldownBlocked(HitscanWeapon.FireResult r)
    {
        if (sfx && cooldownClip) sfx.PlayOneShot(cooldownClip, 0.6f);
    }
}
