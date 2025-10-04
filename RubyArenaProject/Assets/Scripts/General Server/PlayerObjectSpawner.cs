using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerObjectSpawner : NetworkBehaviour
{
    public GameObject playerPrefab;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer) return;

        NetworkManager.OnClientConnectedCallback += SpawnPlayer;
        foreach(var player in ServerPlayerStateManager.Instance.playerStates)
        {
            SpawnPlayer(player.Key);
        }

    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        NetworkManager.OnClientConnectedCallback -= SpawnPlayer;
    }

    void SpawnPlayer(ulong clientId)
    {
        var obj = Instantiate(playerPrefab);

        var objNO = obj.GetComponent<NetworkObject>();
        var playerScript = obj.GetComponent<PlayerScript>();
        playerScript.characterID.Value = ServerPlayerStateManager.Instance.playerStates[clientId].chosenCharacter.Value;
        objNO.SpawnWithOwnership(clientId);
    }

}
