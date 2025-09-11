using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(CapsuleCollider))]
public class MonsterController : EntityController
{
    [Header("Targeting")]
    public Transform explicitTarget;                   // (optionnel) drag ton Nexus ici
    [Tooltip("Recherche le Nexus par scène si pas de target explicite")]
    public bool autoFindNexus = true;
    [Tooltip("Tag utilisé pour trouver le Nexus si autoFind est activé")]
    public string nexusTag = "Untagged";               // mets "Nexus" si tu tagges l'objet Nexus

    [Header("Movement")]
    public float repathInterval = 0.25f;               // secondes entre SetDestination
    public float fallbackMoveSpeed = 2.2f;             // si pas d'agent
    public float stoppingDistance = 1.2f;              // distance à laquelle on s'arrête (≈ attaqueRange)

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
    private EntityController nexusEntity;

    protected override void Awake()
    {
        base.Awake();
        agent = GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            agent.stoppingDistance = Mathf.Max(stoppingDistance, attackRange * 0.9f);
            agent.updateRotation = true;
            agent.updateUpAxis = true;
        }

        AcquireTarget();
    }

    void AcquireTarget()
    {
        if (explicitTarget != null) return;

        var go = GameObject.FindGameObjectWithTag("Nexus");
        if (go)
        {
            explicitTarget = go.transform;
            nexusEntity = go.GetComponentInParent<EntityController>();
        }
    }


    void Update()
    {
        if (explicitTarget == null || nexusEntity == null || !nexusEntity.IsAlive)
        {
            AcquireTarget();
            return;
        }

        float dt = Time.deltaTime;
        float dist = Vector3.Distance(transform.position, explicitTarget.position);

        // Déplacement
        if (dist > attackRange)
        {
            MoveTowardsTarget(dt);
        }
        else
        {
            // face au Nexus
            Vector3 look = explicitTarget.position - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), 10f * dt);
        }

        // Attaque
        if (dist <= attackRange && Time.time >= nextAttackTime && nexusEntity != null)
        {
            nextAttackTime = Time.time + attackCooldown;
            DealDamage(nexusEntity, overrideAmount: attackDamage, overrideType: attackDamageType,
                       hitPoint: explicitTarget.position, hitNormal: Vector3.up);
            // TODO: jouer une anim/sfx ici si tu as un Animator / Audio
        }
    }

    void MoveTowardsTarget(float dt)
    {
        if (agent != null && agent.isOnNavMesh)
        {
            if (Time.time >= nextPathTime)
            {
                agent.stoppingDistance = Mathf.Max(stoppingDistance, attackRange * 0.9f);
                agent.SetDestination(explicitTarget.position);
                nextPathTime = Time.time + repathInterval;
            }
        }
        else
        {
            // Fallback sans NavMeshAgent : déplacement simple “glissant”
            Vector3 dir = (explicitTarget.position - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                dir.Normalize();
                transform.position += dir * fallbackMoveSpeed * dt;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * dt);
            }
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
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);
    }
}
