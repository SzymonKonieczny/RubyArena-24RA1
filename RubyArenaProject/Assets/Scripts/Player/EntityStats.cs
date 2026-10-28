using Assets.Scripts.Events;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using VContainer;

[System.Serializable]
public struct Stats : INetworkSerializable, System.IEquatable<Stats>
{
    public float speed;
    public bool canFly;
    public int AttackDamage;
    public float AttackSpeed;
    public float AttackRange;
    public float MaxHP;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref speed);
        serializer.SerializeValue(ref canFly);
        serializer.SerializeValue(ref AttackDamage);
        serializer.SerializeValue(ref AttackSpeed);
        serializer.SerializeValue(ref AttackRange);
        serializer.SerializeValue(ref MaxHP);
    }

    public bool Equals(Stats other)
    {
        return speed == other.speed &&
               canFly == other.canFly &&
               AttackDamage == other.AttackDamage &&
               AttackSpeed == other.AttackSpeed &&
               AttackRange == other.AttackRange &&
               MaxHP == other.MaxHP;
    }
}

public class EntityStats : NetworkBehaviour
{
    // Plain fields, not properties: NGO's inspector drawer only enumerates fields, so a
    // property is invisible in the Editor no matter what. Writes to modifiedStats should
    // still go through WriteModifiedStats so the GlobalStatsChangeEvent fires.
    [SerializeField] public NetworkVariable<Stats> basicStats = new();
    [SerializeField] public NetworkVariable<Stats> modifiedStats = new();


    [Inject]
    EventBus<GlobalStatsChangeEvent> GlobalStatsChangeEvent;



    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        basicStats.OnValueChanged += (Stats old, Stats newStats) =>
        {
            if(IsServer)
                modifiedStats.Value = newStats;
        };
        GlobalStatsChangeEvent.Raise(new GlobalStatsChangeEvent { recieverNetworkObjectId = NetworkObjectId });

    }

    public void WriteModifiedStats(Stats _modifiedStats) 
    {
        if (!IsServer) return;
        modifiedStats.Value = _modifiedStats;
        GlobalStatsChangeEvent.Raise(new GlobalStatsChangeEvent { recieverNetworkObjectId = NetworkObjectId });

        //TODO:
        // Sync after every write
    }

}
