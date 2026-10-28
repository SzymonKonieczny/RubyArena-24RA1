using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class HealZone : MonoBehaviour
{
    public GameplayEffectSO healEffect;
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            var playerStatHolder = other.GetComponent<StatModifierHolder>();

            var snapshot = new EffectSnapshot
            {
                registryId = healEffect.registryId,
                carrierNetworkObjectId = playerStatHolder.NetworkObjectId,
                serverTimeEffectEnd = NetworkManager.Singleton.ServerTime.Time + healEffect.duration,
                stackCount = 1
            };
            var effect = EffectRegistry.Instance.FromSnapshot(snapshot);

            playerStatHolder.Apply(effect);
        }    
    }
}
