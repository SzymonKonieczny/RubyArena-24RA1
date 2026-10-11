using Unity.Netcode;
using UnityEngine;
using VContainer;
using Assets.Scripts.Events;

/// <summary>
/// Lightweight damagable resource attached to the Mirage decoy.
/// Lets the standard attack path (UnitResource.damage) damage the decoy and
/// raise a DamageTakenEvent, which DecoyEntity listens for to trigger the explosion.
/// </summary>
public class DecoyUnitResource : UnitResource
{
    [Inject]
    EventBus<DamageTakenEvent> damageEventBus;

    public override void damage(SkillInstanceData skillData)
    {
        if (!IsServer) return;

        float hpBefore = Hp.Value;
        Hp.Value -= skillData.damage;

        damageEventBus.Raise(new DamageTakenEvent
        {
            damagerNetworkObjectId = skillData.ownerNetworkObjectId,
            recieverNetworkObjectId = this.NetworkObject.NetworkObjectId,
            healthBefore = hpBefore,
            healthAfter = Hp.Value,
            spellCarrierNetworkObjectId = skillData.spellCarrierNetworkObjectId,
            damageAmountPreMitigation = skillData.damage,
            damageAmountPostMitigation = skillData.damage,
        });
    }

    public override float getHP() => Hp.Value;
    public override float getMana() => 0f;
    public override void takeMana(float amount) { /* decoy has no mana */ }
}
