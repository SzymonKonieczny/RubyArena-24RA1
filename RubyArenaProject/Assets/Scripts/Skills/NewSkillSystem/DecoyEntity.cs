using UnityEngine;
using Unity.Netcode;
using Assets.Scripts.Events;
using VContainer;

/// <summary>
/// The decoy left behind by MirageSkill. It is a normal damagable entity: when an
/// enemy hits it (any attack that calls UnitResource.damage on it), it explodes,
/// dealing AOE damage to nearby enemies and despawning.
///
/// The EventBus is injected through VContainer. MirageSkill calls
/// container.InjectGameObject(decoy) before SpawnWithOwnership so these [Inject]
/// fields are populated before OnNetworkSpawn runs.
/// </summary>
public class DecoyEntity : NetworkBehaviour
{
    [SerializeField] float explosionRadius = 6f;
    [SerializeField] float lifetime = 8f;        // fallback: despawn if never attacked
    [SerializeField] GameObject explosionVFX;   // optional, visual only (client side)

    // Who the decoy belongs to — the explosion is credited to this player.
    ulong ownerNetworkObjectId;
    int decoyDamage;

    [Inject]
    EventBus<DamageTakenEvent> damageEventBus;
    EventBinding<DamageTakenEvent> damageEventBinding;

    bool exploded = false;

    // --- Lifetime ---

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        damageEventBinding = new EventBinding<DamageTakenEvent>(OnDecoyDamaged);
        damageEventBus.Register(damageEventBinding);

        // Fallback cleanup if the decoy is never attacked (server only).
        if (IsServer)
        {
            Invoke(nameof(DespawnIfNotExploded), lifetime);
        }
    }

    void DespawnIfNotExploded()
    {
        if (!exploded && IsSpawned)
        {
            PlayExplosionVFXClientRPC();
            NetworkObject.Despawn();
        }
    }

    public override void OnNetworkDespawn()
    {
        damageEventBus.Unregister(damageEventBinding);
        base.OnNetworkDespawn();
    }

    /// <summary>Called by MirageSkill right after InjectGameObject, before Spawn.</summary>
    public void Setup(ulong ownerId, int damage)
    {
        ownerNetworkObjectId = ownerId;
        decoyDamage = damage;
    }

    // --- Damage handling (server) ---

    void OnDecoyDamaged(DamageTakenEvent e)
    {
        if (!IsServer) return;
        if (e.recieverNetworkObjectId != NetworkObjectId) return; // not us
        if (exploded) return;

        Explode();
    }

    /// <summary>
    /// Blows up the decoy and damages every player within range (except the owner).
    /// Runs on the server; despawns the decoy network object.
    /// </summary>
    void Explode()
    {
        exploded = true;
        damageEventBus.Unregister(damageEventBinding);
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;

            CombatEntity combatEntity = hit.GetComponent<CombatEntity>();
            if (combatEntity == null) continue;
            if (combatEntity.NetworkObjectId == ownerNetworkObjectId) continue;

            UnitResource resource = hit.GetComponent<UnitResource>();
            if (resource == null) continue;

            resource.damage(new SkillInstanceData
            {
                damage = decoyDamage,
                ownerNetworkObjectId = ownerNetworkObjectId
            });
        }

        PlayExplosionVFXClientRPC();
        NetworkObject.Despawn();
    }

    [ClientRpc]
    void PlayExplosionVFXClientRPC()
    {
        if (explosionVFX == null) return;
        GameObject effect = Instantiate(explosionVFX, transform.position, transform.rotation);
        Destroy(effect, 3f);
    }
}
