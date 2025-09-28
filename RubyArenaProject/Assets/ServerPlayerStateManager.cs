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
    private void Start()
    {
        if (Instance == null)
        {
            Instance = this;
            NetworkManager.Singleton.OnClientConnectedCallback += OnConnectedPlayer;

        }
        else
        {
            Destroy(this);
        }

    } 
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if(!IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnConnectedPlayer;
           // Destroy(this);
        }
    }
    void OnConnectedPlayer(ulong clientId)
    {
        var obj = Instantiate(playerStateCarrier);
        var state = obj.GetComponent<LocalPlayerStateManager>();


        state.NetworkObject.SpawnWithOwnership(clientId); // spawns and associates ownership
        
        playerStates[clientId] = state;

        foreach (var player in playerStates)
        {
            Debug.Log($"player connected with ID : {player.Key}");
        }
    }
}
