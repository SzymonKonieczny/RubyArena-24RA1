using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Networking.Transport;

public class EntityStatisticsModifier //add more as needed
{
    public float speedMultiplier = 1;
    public int AttackDamageBoost = 0;
    public int AttackSpeedMultiplier = 1;

    public double serverTimeEffectEnd;

}


/*
class GameplayEffectExample : IGameplayEffect <--------- Pipeline Takich!
{
    float speedBuff = 1.4;
    public Apply(PlayerSkillHolder playerRef)
    { 
        playerRef.StatsAfterEffects.Speed *= speedBuff; 
    }
    public void OnStart()
    public void OnFinish()
    public void OnFixedUpdate() //apply overtime dmg np



}
public class EntityEffectHolder : MonoBehaviour
{
    public List<IEffect> modifiers = new();
    public EntityStatistics statsAfterModifiers = new();

    List<IEffect> toRemove = new();
    private void FixedUpdate()
    {

        foreach (var modifier in modifiers)
        {
            if (NetworkManager.Singleton.NetworkTimeSystem.ServerTime >= modifier.serverTimeEffectEnd)
            {
                toRemove.Add(modifier);
            }
        }
        foreach (var r in toRemove)
        {
            modifiers.Remove(r);
        }
        toRemove.Clear();
    }
}
 
 */
public class StatModifierHolder : MonoBehaviour
{
    public EntityStats entityStats;
    public void AddModifier(EntityStatisticsModifier modifier)
    {
        modifiers.Add(modifier);
        RecalculateStats();
    }

    [SerializeField]
    List<EntityStatisticsModifier> modifiers = new();

    List<EntityStatisticsModifier> toRemove = new();

    void RecalculateStats()
    {
        Stats stats = new()
        {
            AttackDamage = entityStats.basicStats.Value.AttackDamage,
            AttackRange = entityStats.basicStats.Value.AttackRange,
            AttackSpeed = entityStats.basicStats.Value.AttackSpeed,
            canFly = entityStats.basicStats.Value.canFly,
            speed = entityStats.basicStats.Value.speed,
        };

        foreach (EntityStatisticsModifier modifier in modifiers) 
        {
            stats.AttackSpeed *= modifier.AttackSpeedMultiplier;
            stats.AttackDamage += modifier.AttackDamageBoost;
            stats.speed *= modifier.speedMultiplier;
        }
        entityStats.WriteModifiedStats(stats);
    }

    private void FixedUpdate()
    {

        foreach (var modifier in modifiers)
        {
            if (NetworkManager.Singleton.NetworkTimeSystem.ServerTime >= modifier.serverTimeEffectEnd)
            {
                toRemove.Add(modifier);
            }
        }
        foreach (var r in toRemove)
        {
            modifiers.Remove(r);
        }
        if (toRemove.Count > 0)
        {
            //TODO: 
            // Call recalculation of EntityStats
            RecalculateStats();
        }

        toRemove.Clear();
    }
}
