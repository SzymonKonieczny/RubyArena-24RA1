using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class BrawlGameMode : NetworkBehaviour, IGameMode
{
    [SerializeField]
    public enum BrawlTeam
    {
        Red,
        Blue
    }

    List<ulong> redTeamPlayers;
    List<ulong> blueTeamPlayers;
    [SerializeField] BrawlTeam nextTeamToAssignTo = BrawlTeam.Red;
    [SerializeField] Transform respawn_point;
    [SerializeField] private TeamBaseBrawlGameMode redBase;
    [SerializeField] private TeamBaseBrawlGameMode blueBase;

    public TeamBaseBrawlGameMode getTargetBaseRef(BrawlTeam targetTeam)
        => targetTeam switch
        {
            BrawlTeam.Red => redBase,
            BrawlTeam.Blue => blueBase,
            _ => null
        };
       
        
    public bool CanDamage(ulong networkId)
    {
        return true;
    }

    public void RegisterNetworkedObject(ulong networkId)
    {
     
        
    }
    
    private void RegisterObject(GameObject gameObject)
    {
        var teamBase = gameObject.GetComponent<TeamBaseBrawlGameMode>();
        if (teamBase != null)
        {
            RegisterTeamBase(teamBase);
            return;
        }
        var minionSpawner = gameObject.GetComponent<MinionSpawnRequester>();
        if (minionSpawner != null)
        {
            RegisterMinionSpawner(minionSpawner);
            return;
        }

    }
    private void RegisterMinionSpawner(MinionSpawnRequester  spawner)
    {
        spawner.Initialize(this);
    }
    private void RegisterTeamBase(TeamBaseBrawlGameMode teamBase)
    { 
        switch (teamBase.BrawlTeam)
        {
            case BrawlTeam.Blue:
                blueBase = teamBase;
                break;
            case BrawlTeam.Red:
                redBase = teamBase;
                break;
        } 
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
        if (!IsServer) return;

       GameObject[] gamemodeRelatedGOs= GameObject.FindGameObjectsWithTag("GameModeRelatedObject");
        foreach(GameObject go in gamemodeRelatedGOs)
        {
            RegisterObject(go);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
