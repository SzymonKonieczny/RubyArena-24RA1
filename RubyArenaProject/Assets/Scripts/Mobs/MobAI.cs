using System.Collections;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Mob AI state machine. Drives target acquisition, movement, and attacks.
/// Server-authoritative: only runs on the server.
/// </summary>
public class MobAI : NetworkBehaviour
{
    enum MobAIState
    {
        Idle,
        Chase,
        Attack,
        Stunned,
        Dead
    }

    [SerializeField] MobAIState currentState = MobAIState.Idle;
    [SerializeField] CombatEntity target;
    [SerializeField] MobConfigSO config;

    MobCombatEntity mobCombatEntity;
    MobMovement mobMovement;
    MobUnitResource mobUnitResource;
    MobAnimationScript mobAnimation;
    EntityStats entityStats;

    float stunTimer = 0f;
    float attackCooldownTimer = 0f;
    bool isDead = false;

    // For Neutral disposition: track who attacked us first
    ulong? firstAttackerNetworkId;

    public void Initialize(MobConfigSO cfg)
    {
        config = cfg;
        mobCombatEntity = GetComponent<MobCombatEntity>();
        mobMovement = GetComponent<MobMovement>();
        mobUnitResource = GetComponent<MobUnitResource>();
        mobAnimation = GetComponent<MobAnimationScript>();
        entityStats = GetComponent<EntityStats>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer && config != null)
        {
            // Subscribe to damage events to detect being attacked (for Passive/Neutral)
            // This will be wired up by MobCore after EventBus injection
        }
    }

    void Update()
    {
        if (!IsServer || isDead || config == null) return;

        // Handle stun timer
        if (stunTimer > 0)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0 && currentState == MobAIState.Stunned)
            {
                TransitionTo(MobAIState.Idle);
            }
            return;
        }

        // Update attack cooldown
        if (attackCooldownTimer > 0)
            attackCooldownTimer -= Time.deltaTime;

        // State machine
        switch (currentState)
        {
            case MobAIState.Idle:
                UpdateIdle();
                break;
            case MobAIState.Chase:
                UpdateChase();
                break;
            case MobAIState.Attack:
                UpdateAttack();
                break;
        }

        // Check death
        if (mobUnitResource != null && mobUnitResource.getHP() <= 0 && !isDead)
        {
            Die();
        }
    }

    void UpdateIdle()
    {
        // Aggressive mobs scan for targets
        if (config.disposition == MobDisposition.Aggressive)
        {
            ScanForTarget();
        }

        // Passive/Neutral: only act if we have a target (set externally when damaged)
        if (target != null)
        {
            float dist = Vector3.Distance(transform.position, target.transform.position);
            if (dist <= config.attackRange)
                TransitionTo(MobAIState.Attack);
            else
                TransitionTo(MobAIState.Chase);
        }
    }

    void UpdateChase()
    {
        if (target == null || !target.IsSpawned)
        {
            target = null;
            TransitionTo(MobAIState.Idle);
            return;
        }

        float dist = Vector3.Distance(transform.position, target.transform.position);

        // If target moved out of aggro range (for Aggressive), lose interest
        if (config.disposition == MobDisposition.Aggressive && dist > config.aggroRange * 1.5f)
        {
            target = null;
            firstAttackerNetworkId = null;
            TransitionTo(MobAIState.Idle);
            return;
        }

        if (dist <= config.attackRange)
        {
            TransitionTo(MobAIState.Attack);
            return;
        }

        mobMovement?.SetDestination(target.transform.position);
    }

    void UpdateAttack()
    {
        if (target == null || !target.IsSpawned)
        {
            target = null;
            TransitionTo(MobAIState.Idle);
            return;
        }

        float dist = Vector3.Distance(transform.position, target.transform.position);
        if (dist > config.attackRange * 1.2f)
        {
            TransitionTo(MobAIState.Chase);
            return;
        }

        // Face target
        Vector3 lookDir = (target.transform.position - transform.position);
        lookDir.y = 0;
        if (lookDir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(lookDir);

        // Attack on cooldown
        if (attackCooldownTimer <= 0)
        {
            StartCoroutine(ExecuteAttack());
            attackCooldownTimer = config.attackCooldown;
        }
    }

    IEnumerator ExecuteAttack()
    {
        // Windup
        mobAnimation?.Trigger("WindUp");
        yield return new WaitForSeconds(config.attackWindup);

        if (isDead || target == null || !target.IsSpawned) yield break;

        // Hit detection based on attack type
        switch (config.attackType)
        {
            case MobAttackType.Melee:
                ExecuteMeleeAttack();
                break;
            case MobAttackType.AOE:
                ExecuteAOEAttack();
                break;
            case MobAttackType.RangedProjectile:
                // TODO: spawn projectile
                break;
        }

        mobAnimation?.Trigger("Attack");
    }

    void ExecuteMeleeAttack()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position + transform.forward * config.attackRange * 0.5f, config.attackRange);
        foreach (var hit in hits)
        {
            var unitResource = hit.GetComponent<UnitResource>();
            var combatEntity = hit.GetComponent<CombatEntity>();
            if (unitResource == null || combatEntity == null || combatEntity == mobCombatEntity) continue;

            int damage = config.attackDamage;
            if (entityStats != null)
                damage = entityStats.modifiedStats.Value.AttackDamage;

            unitResource.damage(new SkillInstanceData
            {
                damage = damage,
                ownerNetworkObjectId = NetworkObjectId
            });
        }
    }

    void ExecuteAOEAttack()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, config.attackRange);
        foreach (var hit in hits)
        {
            var unitResource = hit.GetComponent<UnitResource>();
            var combatEntity = hit.GetComponent<CombatEntity>();
            if (unitResource == null || combatEntity == null || combatEntity == mobCombatEntity) continue;

            int damage = config.attackDamage;
            if (entityStats != null)
                damage = entityStats.modifiedStats.Value.AttackDamage;

            unitResource.damage(new SkillInstanceData
            {
                damage = damage,
                ownerNetworkObjectId = NetworkObjectId
            });
        }
    }

    void ScanForTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, config.aggroRange);
        float closestDist = float.MaxValue;
        CombatEntity closest = null;

        foreach (var hit in hits)
        {
            var combatEntity = hit.GetComponent<PlayerCombatManager>();
            if (combatEntity == null || combatEntity == mobCombatEntity) continue;
            if (!combatEntity.CanCombat) continue;

            float dist = Vector3.Distance(transform.position, combatEntity.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = combatEntity;
            }
        }

        if (closest != null)
        {
            target = closest;
            float dist = Vector3.Distance(transform.position, target.transform.position);
            if (dist <= config.attackRange)
                TransitionTo(MobAIState.Attack);
            else
                TransitionTo(MobAIState.Chase);
        }
    }

    void TransitionTo(MobAIState newState)
    {
        if (currentState == newState) return;

        // Exit current state
        switch (currentState)
        {
            case MobAIState.Chase:
                mobMovement?.Stop();
                break;
        }

        currentState = newState;

        // Enter new state
        switch (newState)
        {
            case MobAIState.Idle:
                mobMovement?.Stop();
                mobAnimation?.PlayState("Idle");
                break;
            case MobAIState.Chase:
                mobAnimation?.PlayState("Run");
                break;
            case MobAIState.Attack:
                mobMovement?.Stop();
                mobAnimation?.PlayState("Idle");
                break;
            case MobAIState.Stunned:
                mobMovement?.Stop();
                mobAnimation?.Trigger("Stun");
                break;
        }
    }

    /// <summary>
    /// Called when this mob takes damage. Used by Passive/Neutral dispositions to acquire targets.
    /// </summary>
    public void OnDamagedBy(ulong attackerNetworkObjectId)
    {
        if (!IsServer || isDead) return;

        if (config.disposition == MobDisposition.Passive)
        {
            // Passive: only retaliate if we don't have a target yet
            if (target == null)
            {
                firstAttackerNetworkId = attackerNetworkObjectId;
                AcquireTarget(attackerNetworkObjectId);
            }
        }
        else if (config.disposition == MobDisposition.Neutral)
        {
            // Neutral: remember first attacker, retaliate only against them
            if (firstAttackerNetworkId == null)
            {
                firstAttackerNetworkId = attackerNetworkObjectId;
                AcquireTarget(attackerNetworkObjectId);
            }
        }
        // Aggressive: already scanning, no change needed
    }

    void AcquireTarget(ulong networkObjectId)
    {
        if (NetworkManager.Singleton == null) return;
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject targetNO))
        {
            var combatEntity = targetNO.GetComponent<CombatEntity>();
            if (combatEntity != null)
            {
                target = combatEntity;
                TransitionTo(MobAIState.Chase);
            }
        }
    }

    public void SetStun(float duration)
    {
        if (!IsServer || isDead) return;
        stunTimer = duration;
        TransitionTo(MobAIState.Stunned);
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        TransitionTo(MobAIState.Dead);

        mobMovement?.Stop();

        // Disable the main physics collider (not trigger colliders — they don't matter)
        var mainCollider = GetComponent<CapsuleCollider>();
        if (mainCollider != null)
            mainCollider.enabled = false;

        // Death animation
        mobAnimation?.Trigger(config.deathAnimationTrigger);

        // Death VFX
        if (config.deathVFX != null)
        {
            GameObject vfx = Instantiate(config.deathVFX, transform.position, Quaternion.identity);
            Destroy(vfx, 3f);
        }

        // Despawn after delay
        StartCoroutine(DespawnAfterDelay(config.deathDespawnDelay));
    }

    IEnumerator DespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn();
    }

    void OnDrawGizmosSelected()
    {
        if (config == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, config.aggroRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, config.attackRange);
    }
}
