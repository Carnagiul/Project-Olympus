using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[DisallowMultipleComponent]
public class MonsterController : EntityController
{
    [Header("Targeting")]
    public Transform target;                 // si null => fallback par tag
    public string nexusTag = "Nexus";

    [Header("Movement")]
    public float repathInterval = 0.25f;
    public float stoppingDistance = 1.2f;
    public float sampleRadius = 2.0f;

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
        agent.updateRotation = true;
        agent.updateUpAxis = true;
        agent.stoppingDistance = Mathf.Max(stoppingDistance, AttackRange * 0.9f);

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

    public void SetTarget(Transform t)
    {
        target = t;
        targetEntity = target ? target.GetComponentInParent<EntityController>() : null;
    }

    public void SetTargetTeam(Team enemyTeam)
    {
        if (enemyTeam && enemyTeam.nexus)
            SetTarget(enemyTeam.nexus.transform);
    }

    void Update()
    {
        if (target == null || targetEntity == null || !targetEntity.IsAlive)
        {
            AcquireTarget();
            return;
        }

        float dist = Vector3.Distance(transform.position, target.position);

        if (dist > AttackRange && Time.time >= nextPathTime && agent.isOnNavMesh)
        {
            agent.stoppingDistance = Mathf.Max(stoppingDistance, AttackRange * 0.9f);
            agent.SetDestination(target.position);
            nextPathTime = Time.time + repathInterval;
        }

        if (dist <= Mathf.Max(AttackRange * 1.5f, 3f))
        {
            Vector3 look = target.position - transform.position; look.y = 0f;
            if (look.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), 10f * Time.deltaTime);
        }

        if (dist <= AttackRange && Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + AttackCooldown;
            DealDamage(targetEntity, DamageEffective, DamageType, hitPoint: target.position, hitNormal: Vector3.up);
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
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, AttackRange);
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, Mathf.Max(stoppingDistance, AttackRange * 0.9f));
    }
}
