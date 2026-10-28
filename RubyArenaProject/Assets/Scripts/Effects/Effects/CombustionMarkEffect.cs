using UnityEngine;

/// <summary>
/// A MARK — an effect that does nothing on its own and exists purely to be DETECTED by
/// another spell, which then consumes it for a bonus. This is the pattern to copy for any
/// "set up then pay off" mechanic.
///
/// Example: Yang's charged blast applies a combustion mark; any subsequent fire damage on
/// a marked target detonates it for burst damage plus a burn. The mark itself contributes
/// nothing — no stats, no ticks — which is why it can share the holder with buffs and DoTs.
///
/// See the "Marks" section of GameplayEffectSystem.md.
/// </summary>
public class CombustionMarkEffect : GameplayEffect
{
    public CombustionMarkDefinitionSO Def => (CombustionMarkDefinitionSO)Definition;

    /// <summary>Damage dealt by whoever detonates it (the blast that consumed the mark).
    /// Set by the consumer before calling Detonate.</summary>
    public int detonationDamageBonus;

    public override float Duration => Definition != null ? Definition.duration : 0f;

    protected override void OnStartAuthority(GameplayEffectContext ctx) { }
    protected override void OnTickAuthority(GameplayEffectContext ctx) { }   // inert by design

    /// <summary>
    /// Consume the mark early: deals burst damage and applies a burn to the target.
    /// Called by the DETECTING spell, not by the holder.
    /// </summary>
    public void Detonate(GameplayEffectContext ctx, ulong detonatedByNetworkObjectId)
    {
        if (ctx.Resources == null) return;

        ctx.Resources.damage(new SkillInstanceData
        {
            Type = Def.skillType,
            damage = Def.detonationDamage + detonationDamageBonus,
            ownerNetworkObjectId = detonatedByNetworkObjectId,
            spellCarrierNetworkObjectId = CarrierNetworkObjectId,
        });

        if (Def.burnOnDetonate != null)
        {
            GameplayEffect burn = Def.burnOnDetonate.CreateEffect();
            burn.Definition = Def.burnOnDetonate;
            burn.SourceNetworkObjectId = detonatedByNetworkObjectId;
            burn.CarrierNetworkObjectId = CarrierNetworkObjectId;
            burn.serverTimeEffectEnd = ServerTime + Def.burnOnDetonate.Duration;

            ctx.Holder.Apply(burn);
        }

        // Consuming from inside a hit handler is safe — the holder defers removal.
        ctx.Holder.Remove(this);
    }
}
