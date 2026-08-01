using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class EntityStatistics //add more as needed
{
    public float speedModifier = 1;

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
    public List<EntityStatistics> modifiers = new();


    List<EntityStatistics> toRemove = new();
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
