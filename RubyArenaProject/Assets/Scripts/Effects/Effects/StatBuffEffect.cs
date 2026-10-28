using UnityEngine;

/// <summary>
/// Pure passive stat contribution: speed, attack speed, attack damage. Ticks nothing —
/// it exists only to fold into <see cref="GameplayEffect.CollectStatModifier"/>, which is
/// what makes a buff and a DoT the same shape to the holder.
///
/// Replaces the inline EntityStatisticsModifier that RubyFormSkill used to build.
/// </summary>
public class StatBuffEffect : GameplayEffect
{
    public StatBuffDefinitionSO Def => (StatBuffDefinitionSO)Definition;

    public override float Duration => Definition != null ? Definition.duration : 0f;

    public override void CollectStatModifier(ref EntityStatisticsModifier acc)
    {
        StatBuffDefinitionSO def = Def;
        if (def == null) return;

        // Stacking multiplies the delta above 1 (a 1.3x buff at 2 stacks = 1.6x), and
        // leaves a debuff's below-1 factor alone beyond the first application.
        if (def.speedMultiplier != 1f)
            acc.speedMultiplier *= Mathf.Lerp(1f, def.speedMultiplier, stackCount);

        if (def.attackSpeedMultiplier != 1f)
            acc.AttackSpeedMultiplier *= Mathf.Lerp(1f, def.attackSpeedMultiplier, stackCount);

        acc.AttackDamageBoost += def.attackDamageBonus * stackCount;
    }
}
