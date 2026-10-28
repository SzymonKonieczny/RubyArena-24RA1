using UnityEngine;

/// <summary>
/// Periodic healing. Deliberately NOT implemented as negative damage: PlayerResources
/// treats any Hp decrease as a bleed burst (and generates a camera impulse), so routing
/// a heal through damage() would fire blood particles and screen shake. Heals write Hp
/// directly and clamp at the target's max.
/// </summary>
public class HealOverTimeEffect : GameplayEffect
{
    public HealOverTimeDefinitionSO Def => (HealOverTimeDefinitionSO)Definition;

    public float healPerTick;
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

        if (ctx.Resources == null) return;

        float max = GetMaxHP(ctx);
        ctx.Resources.Hp.Value = Mathf.Min(ctx.Resources.Hp.Value + healPerTick * stackCount, max);
    }

    /// <summary>PlayerResources carries MaxHP privately; fall back to the networked
    /// stats so this works for mobs and decoys too.</summary>
    static float GetMaxHP(GameplayEffectContext ctx)
    {
        if (ctx.Stats != null && ctx.Stats.modifiedStats.Value.MaxHP > 0f)
            return ctx.Stats.modifiedStats.Value.MaxHP;
        return ctx.Resources.Hp.Value;   // no known max → heal is a no-op, safest default
    }
}
