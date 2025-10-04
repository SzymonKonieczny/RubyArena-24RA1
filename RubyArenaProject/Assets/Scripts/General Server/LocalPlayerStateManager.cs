using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class LocalPlayerStateManager : NetworkBehaviour
{
    public static LocalPlayerStateManager LocalInstance;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
        {
            LocalInstance = this;
        }

        if (!IsServer) return; 
        chosenCharacter.OnValueChanged += (_, _) =>
        {
            ServerPlayerStateManager.Instance.onPlayerStatesChanged?.Invoke(this);
        };
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

    }

    public NetworkVariable<int> chosenCharacter = new(-1, writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> isLobbyReady = new(false, writePerm: NetworkVariableWritePermission.Owner);

}
