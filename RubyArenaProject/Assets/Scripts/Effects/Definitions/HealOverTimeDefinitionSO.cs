using UnityEngine;

[CreateAssetMenu(fileName = "HoT", menuName = "ScriptableObjects/Effects/HealOverTime", order = 11)]
public class HealOverTimeDefinitionSO : GameplayEffectSO
{
    [Header("Ticking")]
    public float tickInterval = 0.5f;
    public float healPerTick = 5f;


    public override GameplayEffect CreateEffect() => new HealOverTimeEffect
    {
        healPerTick = healPerTick,
        tickInterval = tickInterval,
    };

    void OnValidate()
    {
        if (tickInterval <= 0f) tickInterval = 0.1f;
    }
}
