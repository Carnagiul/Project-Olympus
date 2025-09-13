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
    public DamageType damageType = DamageType.Kinetic;   // ← enum global
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

    [Header("Laser / Tracer (optional)")]
    public bool useLaser = true;
    public Transform muzzle;                  // Optionnel : point de sortie. Si null → caméra.
    public LineRenderer laserLine;            // Optionnel : si null, on l’instancie à l’Awake.
    [Range(0.01f, 0.3f)] public float laserDuration = 0.06f;
    [Range(0.001f, 0.05f)] public float laserStartWidth = 0.01f;
    [Range(0.001f, 0.05f)] public float laserEndWidth = 0.002f;

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

    private Coroutine laserRoutine;

    private void Awake()
    {
        // Création auto si non assigné (pratique pour tester rapidement)
        if (useLaser && laserLine == null)
        {
            var go = new GameObject("LaserLine");
            go.transform.SetParent(transform, false);
            laserLine = go.AddComponent<LineRenderer>();
            laserLine.enabled = false;
            laserLine.useWorldSpace = true;
            laserLine.positionCount = 2;
            laserLine.startWidth = laserStartWidth;
            laserLine.endWidth = laserEndWidth;

            // Matériau simple par défaut (évite le rose si aucun mat n'est assigné)
            var shader = Shader.Find("Sprites/Default");
            if (shader != null) laserLine.material = new Material(shader);
            // Couleur blanche par défaut (tu peux mettre un Gradient dans l’inspecteur)
            laserLine.startColor = Color.white;
            laserLine.endColor = Color.white;
        }
    }

    private Vector3 GetMuzzlePosition()
        => muzzle ? muzzle.position : aimCamera.transform.position;

    private void DrawLaser(Vector3 start, Vector3 end)
    {
        if (!useLaser || laserLine == null) return;
        if (laserRoutine != null) StopCoroutine(laserRoutine);
        laserRoutine = StartCoroutine(LaserRoutine(start, end));
    }

    private System.Collections.IEnumerator LaserRoutine(Vector3 start, Vector3 end)
    {
        laserLine.enabled = true;
        laserLine.positionCount = 2;
        laserLine.startWidth = laserStartWidth;
        laserLine.endWidth = laserEndWidth;

        laserLine.SetPosition(0, start);
        laserLine.SetPosition(1, end);

        float t = 0f;
        // Petit fondu en réduisant l’épaisseur
        while (t < laserDuration)
        {
            t += Time.deltaTime;
            float k = 1f - (t / laserDuration);
            laserLine.startWidth = laserStartWidth * k;
            laserLine.endWidth = laserEndWidth * k;
            yield return null;
        }

        laserLine.enabled = false;
    }

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

        // NEW: point de départ visuel (si tu as un muzzle, on part du muzzle)
        Vector3 visualStart = GetMuzzlePosition();

        result.fired = true;
        result.damageApplied = damage;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, range, hittableLayers, QueryTriggerInteraction.Ignore))
        {
            result.hit = true;
            result.point = hit.point;
            result.normal = hit.normal;

            // --- Trouver l'entité touchée (dans le collider ou ses parents)
            EntityController target = hit.collider.GetComponentInParent<EntityController>();

            // --- Conditions: ne pas se tirer soi-même, et n'appliquer dégâts que si tag == "Monsters"
            bool isSelf = target != null && ReferenceEquals(target, owner);
            bool isMonster = target != null && target.CompareTag("Monsters");

            if (!isSelf && isMonster)
            {
                result.target = target;

                // Nouvelle API DealDamage(target, amount, DamageType, ...)
                owner.DealDamage(
                    target,
                    damage,
                    damageType,
                    hitPoint: hit.point,
                    hitNormal: hit.normal
                );
            }

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
            if (!isSelf && isMonster)
            {
                Hit?.Invoke(result);
                DrawLaser(visualStart, hit.point);
            }
            else
            {
                Vector3 visualEnd = origin + dir * range;
                DrawLaser(visualStart, visualEnd);
                Miss?.Invoke(result);
            }
        }
        else
        {
            Fired?.Invoke(result);
            Miss?.Invoke(result);
            Vector3 visualEnd = origin + dir * range;
            DrawLaser(visualStart, visualEnd);
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
