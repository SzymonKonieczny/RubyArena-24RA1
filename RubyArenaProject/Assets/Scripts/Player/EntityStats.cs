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

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref speed);
        serializer.SerializeValue(ref canFly);
        serializer.SerializeValue(ref AttackDamage);
        serializer.SerializeValue(ref AttackSpeed);
        serializer.SerializeValue(ref AttackRange);
    }

    public bool Equals(Stats other)
    {
        return speed == other.speed &&
               canFly == other.canFly &&
               AttackDamage == other.AttackDamage &&
               AttackSpeed == other.AttackSpeed &&
               AttackRange == other.AttackRange;
    }
}

public class EntityStats : NetworkBehaviour
{
    [SerializeField] public NetworkVariable<Stats> basicStats;
    [SerializeField] public NetworkVariable<Stats> modifiedStats { get; private set; } = new();


    [Inject]
    EventBus<GlobalStatsChangeEvent> GlobalStatsChangeEvent;



    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        basicStats.OnValueChanged += (Stats old, Stats newStats) =>
        {
            modifiedStats.Value = newStats;
        };
        GlobalStatsChangeEvent.Raise(new GlobalStatsChangeEvent { recieverNetworkObjectId = NetworkObjectId });

    }

    public void WriteModifiedStats(Stats _modifiedStats) 
    {
        modifiedStats.Value = _modifiedStats;
        GlobalStatsChangeEvent.Raise(new GlobalStatsChangeEvent { recieverNetworkObjectId = NetworkObjectId });

        //TODO:
        // Sync after every write
    }

}
