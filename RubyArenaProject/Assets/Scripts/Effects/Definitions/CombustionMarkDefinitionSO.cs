using UnityEngine;

[CreateAssetMenu(fileName = "CombustionMark", menuName = "ScriptableObjects/Effects/CombustionMark", order = 12)]
public class CombustionMarkDefinitionSO : GameplayEffectSO
{
    [Header("Mark")]
    [Tooltip("Burst damage when a later spell consumes the mark.")]
    public int detonationDamage = 40;

    [Tooltip("Burn applied to the target after detonation. Optional.")]
    public DamageOverTimeDefinitionSO burnOnDetonate;

    public SkillType skillType = SkillType.Seroid;


    public override GameplayEffect CreateEffect() => new CombustionMarkEffect();

    public void OnValidate()
    {
        if (string.IsNullOrEmpty(effectId)) effectId = "mark_combustion";
        if (maxStacks < 1) maxStacks = 1;
    }
}
