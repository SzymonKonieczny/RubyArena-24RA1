using UnityEngine;

[CreateAssetMenu(fileName = "StatBuff", menuName = "ScriptableObjects/Effects/StatBuff", order = 13)]
public class StatBuffDefinitionSO : GameplayEffectSO
{
    [Header("Contribution (leave at identity if unused)")]
    public float speedMultiplier = 1f;
    public float attackSpeedMultiplier = 1f;
    public int attackDamageBonus = 0;


    public override GameplayEffect CreateEffect() => new StatBuffEffect();

    void OnValidate()
    {
        if (duration <= 0f) duration = 0.1f;
    }
}
