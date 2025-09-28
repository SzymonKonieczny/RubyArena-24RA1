using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

public class ServerPlayerStateManager : NetworkBehaviour
{
    public static ServerPlayerStateManager Instance;
    public GameObject playerStateCarrier;
    public Dictionary<ulong, LocalPlayerStateManager> playerStates = new();
    public Action<LocalPlayerStateManager> onPlayerStatesChanged; //LocalPlayerStateManagers hook and unhook themselves on their own

    // Start is called before the first frame update
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }
    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback += OnConnectedPlayer;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnDisconnectedPlayer;
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (!IsServer) return;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnConnectedPlayer;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnDisconnectedPlayer;
    }
    void OnDisconnectedPlayer(ulong clientId)
    {
        playerStates.Remove(clientId);
    }


    void OnConnectedPlayer(ulong clientId)
    {
        var obj = Instantiate(playerStateCarrier);
        var state = obj.GetComponent<LocalPlayerStateManager>();


        state.NetworkObject.SpawnWithOwnership(clientId); 
        
        playerStates[clientId] = state;

        foreach (var player in playerStates)
        {
            Debug.Log($"player connected with ID : {player.Key}");
        }
    }
}
