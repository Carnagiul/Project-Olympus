using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class HitscanWeapon : MonoBehaviour
{
    [Header("Owner / Refs")]
    public EntityController owner;
    public Camera aimCamera;

    [Header("Fire")]
    public float damage = 20f;
    public EntityController.EntityDamageType damageType = EntityController.EntityDamageType.Kinetic;
    public float cooldownSeconds = 0.25f;
    public float range = 20f;
    [Range(0f, 10f)] public float spreadDegrees = 0.5f;

    [Tooltip("Couches touchables. Excluez la couche Player pour plus de sécurité.")]
    public LayerMask hittableLayers = ~0;

    [Header("FX (optional)")]
    public ParticleSystem muzzleFlash;
    public GameObject hitDecalPrefab;
    public GameObject hitImpactVfxPrefab;
    public AudioSource audioSource;
    public AudioClip fireClip;

    [Header("Physics (optional)")]
    public float impactForce = 10f;

    // --- Events & report ---
    public struct FireResult
    {
        public bool fired;
        public bool hit;
        public EntityController target;
        public Vector3 point;
        public Vector3 normal;
        public float damageApplied;
        public float cooldownRemaining;
    }
    public event Action<FireResult> Fired;
    public event Action<FireResult> Hit;
    public event Action<FireResult> Miss;
    public event Action<FireResult> CooldownBlocked;

    private float nextFireTime = 0f;

    public bool CanFireNow => Time.time >= nextFireTime;
    public float CooldownRemaining => Mathf.Max(0f, nextFireTime - Time.time);

    public bool WantsToFire()
    {
        var mouse = Mouse.current;
        return mouse != null && mouse.leftButton.isPressed;
    }

    public bool TryFire(out FireResult result)
    {
        result = default;
        if (aimCamera == null || owner == null)
        {
            result.cooldownRemaining = CooldownRemaining;
            return false;
        }
        if (!CanFireNow)
        {
            result.cooldownRemaining = CooldownRemaining;
            CooldownBlocked?.Invoke(result);
            return false;
        }

        nextFireTime = Time.time + cooldownSeconds;

        if (muzzleFlash) muzzleFlash.Play(true);
        if (audioSource && fireClip) audioSource.PlayOneShot(fireClip, 1f);

        Vector3 origin = aimCamera.transform.position;
        Vector3 dir = GetSpreadDirection(aimCamera.transform.forward, spreadDegrees);

        result.fired = true;
        result.damageApplied = damage;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, range, hittableLayers, QueryTriggerInteraction.Ignore))
        {
            result.hit = true;
            result.point = hit.point;
            result.normal = hit.normal;

            // --- Trouver l'entité touchée (dans le collider ou ses parents)
            EntityController target = hit.collider.GetComponentInParent<EntityController>();

            // --- Conditions: ne pas se tirer soi-même, et n'appliquer dégâts que si tag == "Monster"
            bool isSelf = target != null && ReferenceEquals(target, owner);
            bool isMonster = target != null && target.CompareTag("Monsters");

            if (!isSelf && isMonster)
            {
                result.target = target;
                owner.DealDamage(target,
                    overrideAmount: damage,
                    overrideType: damageType,
                    hitPoint: hit.point,
                    hitNormal: hit.normal);
            }

            // Optionnel: petite poussée physique (même si pas Monster, juste impact)
            if (impactForce > 0f && hit.rigidbody)
                hit.rigidbody.AddForceAtPosition(dir * impactForce, hit.point, ForceMode.Impulse);

            // Impacts visuels
            if (hitImpactVfxPrefab)
            {
                var vfx = Instantiate(hitImpactVfxPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(vfx, 5f);
            }
            if (hitDecalPrefab)
            {
                var decal = Instantiate(hitDecalPrefab, hit.point + hit.normal * 0.01f, Quaternion.LookRotation(-hit.normal));
                decal.transform.SetParent(hit.collider.transform, true);
                Destroy(decal, 20f);
            }

            Fired?.Invoke(result);
            if (!isSelf && isMonster) Hit?.Invoke(result);
            else Miss?.Invoke(result); // on a touché qqch, mais pas un Monster valable
        }
        else
        {
            Fired?.Invoke(result);
            Miss?.Invoke(result);
        }

        result.cooldownRemaining = CooldownRemaining;
        return true;
    }

    Vector3 GetSpreadDirection(Vector3 forward, float degrees)
    {
        if (degrees <= 0.001f) return forward;
        float rad = degrees * Mathf.Deg2Rad;
        float a = UnityEngine.Random.value * Mathf.PI * 2f;
        float r = Mathf.Tan(rad) * Mathf.Sqrt(UnityEngine.Random.value);
        Vector3 right = aimCamera.transform.right;
        Vector3 up = aimCamera.transform.up;
        Vector3 spread = right * (Mathf.Cos(a) * r) + up * (Mathf.Sin(a) * r);
        return (forward + spread).normalized;
    }
}
