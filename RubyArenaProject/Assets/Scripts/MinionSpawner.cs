using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MinionSpawner : NetworkBehaviour
{
    private Dictionary<ulong,NetworkObject> spawnedObjects = new();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void KillAllMinions()
    {
        if (!IsServer) return;

        foreach(var pair in spawnedObjects)
        {
            pair.Value.Despawn();
        }

        spawnedObjects.Clear();
    }
    public void SpawnMinion(Minion minion)
    {
        if (!IsServer) return;

        minion.NetworkObject.Spawn(minion);
        spawnedObjects.Add(minion.NetworkObjectId, minion.NetworkObject);
    }

    public void DespawnMinion(Minion minion)
    {
        if (!IsServer) return;

        minion.NetworkObject.Despawn();
        spawnedObjects.Remove(minion.NetworkObjectId);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
