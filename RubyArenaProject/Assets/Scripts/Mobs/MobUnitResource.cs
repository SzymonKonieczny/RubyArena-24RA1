using UnityEngine;
using Unity.Netcode;
using Assets.Scripts.Events;
using VContainer;

/// <summary>
/// Mob-specific UnitResource. Handles HP, damage application, death event raising,
/// and death animation/despawn lifecycle. No UI bindings (no sliders, camera shake).
/// </summary>
public class MobUnitResource : UnitResource
{
    [SerializeField] float maxHP = 100f;

    [Inject]
    EventBus<DamageTakenEvent> damageEventBus;

    [Inject]
    EventBus<EntityDeathEvent> entityDeathEvent;

    [SerializeField] ParticleSystem bleedSystem;

    public override void damage(SkillInstanceData skillData)
    {
        if (!IsServer) return;

        float hpBefore = Hp.Value;
        Hp.Value -= skillData.damage;

        if (damageEventBus == null) Debug.LogError("damageEventBus was NOT injected. This is a problem!");
        damageEventBus?.Raise(new DamageTakenEvent
        {
            damagerNetworkObjectId = skillData.ownerNetworkObjectId,
            recieverNetworkObjectId = this.NetworkObjectId,
            healthBefore = hpBefore,
            healthAfter = Hp.Value,
            spellCarrierNetworkObjectId = skillData.spellCarrierNetworkObjectId,
            damageAmountPreMitigation = skillData.damage,
            damageAmountPostMitigation = skillData.damage,
        });
        bleedSystem?.Play();
        if (Hp.Value <= 0)
        {
            entityDeathEvent.Raise(new EntityDeathEvent
            {
                killerNetworkObjectId = skillData.ownerNetworkObjectId,
                killedNetworkObjectId = this.NetworkObjectId
            });
        }
    }

    public override float getHP() => Hp.Value;
    public override float getMana() => 0f;
    public override void takeMana(float amount) { /* mobs have no mana */ }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            Hp.Value = maxHP;
        }
    }

    public void SetMaxHP(float amount) => maxHP = amount;
}
