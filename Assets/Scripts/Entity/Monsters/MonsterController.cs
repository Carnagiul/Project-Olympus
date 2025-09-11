using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[DisallowMultipleComponent]
public class MonsterController : EntityController
{
    [Header("Targeting")]
    [Tooltip("Cherche automatiquement un objet Tag 'Nexus' si vide")]
    public Transform target; // laisse vide pour auto-find
    public string nexusTag = "Nexus";

    [Header("Movement")]
    public float repathInterval = 0.25f;       // temps entre 2 SetDestination
    public float stoppingDistance = 1.2f;      // cohérent avec attackRange
    public float sampleRadius = 2.0f;          // tolérance pour snap au NavMesh

    [Header("Attack")]
    public float attackRange = 1.5f;
    public float attackCooldown = 1.0f;
    public float attackDamage = 10f;
    public EntityDamageType attackDamageType = EntityDamageType.Kinetic;

    [Header("Death FX")]
    public GameObject deathFxPrefab;

    private NavMeshAgent agent;
    private float nextPathTime;
    private float nextAttackTime;
    private EntityController targetEntity;

    protected override void Awake()
    {
        base.Awake();
        agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = Mathf.Max(stoppingDistance, attackRange * 0.9f);
        agent.updateRotation = true;
        agent.updateUpAxis = true;

        // Snap au NavMesh si nécessaire (évite les agents "off mesh" au spawn)
        if (NavMesh.SamplePosition(transform.position, out var hit, sampleRadius, NavMesh.AllAreas))
            transform.position = hit.position;

        AcquireTarget();
    }

    void AcquireTarget()
    {
        if (target == null)
        {
            var go = GameObject.FindGameObjectWithTag(nexusTag);
            if (go) target = go.transform;
        }
        targetEntity = target ? target.GetComponentInParent<EntityController>() : null;
    }

    void Update()
    {
        if (target == null || targetEntity == null || !targetEntity.IsAlive)
        {
            AcquireTarget();
            return;
        }

        float dist = Vector3.Distance(transform.position, target.position);

        // Déplacement par NavMesh (pas de fallback)
        if (dist > attackRange && Time.time >= nextPathTime && agent.isOnNavMesh)
        {
            agent.stoppingDistance = Mathf.Max(stoppingDistance, attackRange * 0.9f);
            agent.SetDestination(target.position);
            nextPathTime = Time.time + repathInterval;
        }

        // Regarder la cible quand on est proche
        if (dist <= Mathf.Max(attackRange * 1.5f, 3f))
        {
            Vector3 look = target.position - transform.position; look.y = 0f;
            if (look.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), 10f * Time.deltaTime);
        }

        // Attaque
        if (dist <= attackRange && Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + attackCooldown;
            DealDamage(targetEntity, overrideAmount: attackDamage, overrideType: attackDamageType,
                       hitPoint: target.position, hitNormal: Vector3.up);
            // TODO: déclencher anim/sfx d’attaque ici si tu as un Animator/Audio
        }
    }

    protected override void OnKilled()
    {
        if (deathFxPrefab != null)
        {
            var fx = Instantiate(deathFxPrefab, transform.position, Quaternion.identity);
            Destroy(fx, 5f);
        }
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, stoppingDistance);
    }
}
