using UnityEngine;

/// <summary>
/// Glue component for mobs, analogous to PlayerSkillHolder.
/// Provides the references that attack/ability systems need.
/// </summary>
public class MobSkillHolder : MonoBehaviour
{
    public MobCombatEntity mobCombatManager;
    public MobAnimationScript animationScript;
    public EntityStats Stats;
    public StatModifierHolder statModifierHolder;

    public void Initialize()
    {
        mobCombatManager = GetComponent<MobCombatEntity>();
        animationScript = GetComponent<MobAnimationScript>();
        Stats = GetComponent<EntityStats>();
        statModifierHolder = GetComponent<StatModifierHolder>();
    }
}
