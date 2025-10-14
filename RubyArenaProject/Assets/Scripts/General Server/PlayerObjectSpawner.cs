using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerObjectSpawner : NetworkBehaviour
{
    public GameObject playerPrefab;
    public List<NetworkObject> spawnedObjects = new();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer) return;

        NetworkManager.OnClientConnectedCallback += SpawnPlayer;
        foreach(var player in ServerPlayerStateManager.Instance.playerStates) //catch up for players from lobby
        {
            SpawnPlayer(player.Key);
        }

    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        NetworkManager.OnClientConnectedCallback -= SpawnPlayer;
        if (!IsServer) return;

        foreach (var obj in spawnedObjects)
        {
            if (obj != null && obj.IsSpawned)
            {
                obj.Despawn();
            }
        }

    }
   

    void SpawnPlayer(ulong clientId)
    {
        var obj = Instantiate(playerPrefab);

        var objNO = obj.GetComponent<NetworkObject>();
        spawnedObjects.Add(objNO);
        var playerScript = obj.GetComponent<PlayerScript>();
        playerScript.characterID.Value = ServerPlayerStateManager.Instance.playerStates[clientId].chosenCharacter.Value;
        objNO.SpawnWithOwnership(clientId);
    }

}
