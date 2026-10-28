using UnityEngine;

/// <summary>
/// Periodic damage. The workhorse effect: ticks damage at a fixed interval for a fixed
/// duration, crediting the original source.
///
/// Tick timing is scheduled against absolute ServerTime, not a delta accumulator: a long
/// FixedUpdate (a hitch) would otherwise stretch the interval, and the client's VFX pulse
/// would drift away from the server's. Each tick advances nextTickTime by exactly one
/// interval, so the schedule stays fixed even when a frame is late.
/// </summary>
public class DamageOverTimeEffect : GameplayEffect
{
    public DamageOverTimeDefinitionSO Def => (DamageOverTimeDefinitionSO)Definition;

    public int   tickDamage;
    public float tickInterval;

    public override float Duration => Definition != null ? Definition.duration : 0f;

    double nextTickTime;

    protected override void OnStartAuthority(GameplayEffectContext ctx)
    {
        nextTickTime = serverTimeEffectEnd - Def.duration + tickInterval;
    }

    protected override void OnTickAuthority(GameplayEffectContext ctx)
    {
        if (ServerTime < nextTickTime) return;
        nextTickTime += tickInterval;

        // Spawn protection: a DoT must not keep ticking into a target the gamemode has
        // declared undamageable, otherwise it bypasses what direct damage respects.
        if (!ctx.CanTakeDamage(ctx.Resources.NetworkObjectId)) return;

        ctx.Resources.damage(new SkillInstanceData
        {
            Type = Def.skillType,
            damage = tickDamage * stackCount,
            ownerNetworkObjectId = SourceNetworkObjectId,
            spellCarrierNetworkObjectId = CarrierNetworkObjectId,
        });
    }

    // Client side: nothing to do beyond the cosmetic prefab the holder spawns. The
    // mirror ticks so a future damage-number / pulse VFX has somewhere to live.
    protected override void OnTickCosmetic(GameplayEffectContext ctx) { }
}
