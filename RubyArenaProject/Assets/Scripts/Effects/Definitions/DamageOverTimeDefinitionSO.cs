using UnityEngine;

[CreateAssetMenu(fileName = "DoT", menuName = "ScriptableObjects/Effects/DamageOverTime", order = 10)]
public class DamageOverTimeDefinitionSO : GameplayEffectSO
{
    [Header("Ticking")]
    public float tickInterval = 0.5f;
    public int damagePerTick = 5;

    [Tooltip("Skill type recorded on each tick, for effects that react to specific damage sources.")]
    public SkillType skillType = SkillType.Seroid;

    public override GameplayEffect CreateEffect() => new DamageOverTimeEffect
    {
        tickDamage = damagePerTick,
        tickInterval = tickInterval,
    };

    /// <summary>Set by the holder before apply so the effect knows its absolute end time.
    /// Kept on the SO so effects can compute their own tick schedule.</summary>

    void OnValidate()
    {
        if (tickInterval <= 0f) tickInterval = 0.1f;
    }
}
