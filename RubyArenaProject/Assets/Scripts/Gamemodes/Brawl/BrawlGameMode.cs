using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BrawlGameMode : NetworkBehaviour, IGameMode
{
    enum BrawlTeam
    {
        Red,
        Blue
    }

    List<ulong> redTeamPlayers;
    List<ulong> blueTeamPlayers;
   [SerializeField] BrawlTeam nextTeamToAssignTo = BrawlTeam.Red;
    [SerializeField] Transform respawn_point;
    public bool CanDamage(ulong networkId)
    {
        return true;
    }

    public void RegisterObject(ulong networkId)
    {
     
        
    }

    public void RegisterPlayer(ulong networkId)
    {
        if (!IsServer) return;


        NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkId, out NetworkObject playerNO);
       if(playerNO != null)
        {

            nextTeamToAssignTo ^= nextTeamToAssignTo;
            PlayerScript playerScript = playerNO.GetComponent<PlayerScript>();
            playerScript.playerResources.gameMode = this;
            playerScript.playerMove.RequestTeleportClientRPC(respawn_point.position);
        }

        
    }
    void OnPlayerDeath(ulong killerNoId, ulong killedNoId)
    {
        NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(killerNoId, out NetworkObject killerPlayerNO);
        
    
    }
        // Start is called before the first frame update
        void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
