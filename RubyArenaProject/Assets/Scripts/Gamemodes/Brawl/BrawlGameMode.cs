using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

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

    [SerializeField] private Slider redTeamSlider;
    [SerializeField] private Slider blueTeamSlider;
    [SerializeField] private float defaultMaxBaseHealth = 200;
    [SerializeField] NetworkVariable<float> redTeamHealth = new();
    [SerializeField] NetworkVariable<float> blueTeamHealth = new();

    public TeamBaseBrawlGameMode getTargetBaseRef(BrawlTeam targetTeam)
        => targetTeam switch
        {
            BrawlTeam.Red => redBase,
            BrawlTeam.Blue => blueBase,
            _ => null
        };
    public void RegisterNetworkedObject(ulong networkId)
    {
     
        
    }
    
    private void RegisterObject(GameObject gameObject)
    {
        // Client sided-----------
        var teamHeathBar = gameObject.GetComponent<BrawlGameModeTeamHealthBar>();
        if (teamHeathBar != null)
        {
            RegisterTeamHeathBar(teamHeathBar);
            return;
        }
        var teamBase = gameObject.GetComponent<TeamBaseBrawlGameMode>();
        if (teamBase != null)
        {
            RegisterTeamBase(teamBase);
            return;
        }


        if (!IsServer) return;
        // Server sided-----------
        var minionSpawner = gameObject.GetComponent<MinionSpawnRequester>();
        if (minionSpawner != null)
        {
            RegisterMinionSpawner(minionSpawner);
            return;
        }
    }
    private void RegisterTeamHeathBar(BrawlGameModeTeamHealthBar bar)
    {
        switch (bar.BrawlTeam)
        {
            case BrawlTeam.Red:
                redTeamSlider = bar.GetComponent<Slider>();
                redTeamHealth.OnValueChanged += (float oldValue, float newValue) =>
                {
                    redTeamSlider.value = newValue;
                };

                break;
            case BrawlTeam.Blue:
                blueTeamSlider = bar.GetComponent<Slider>();
                blueTeamHealth.OnValueChanged += (float oldValue, float newValue) =>
                {
                    blueTeamSlider.value = newValue;
                };
                break;
        }
    }


    private void RegisterMinionSpawner(MinionSpawnRequester  spawner)
    {
        spawner.Initialize(this);
    }
    private void RegisterTeamBase(TeamBaseBrawlGameMode teamBase)
    {
        teamBase.baseHealth = defaultMaxBaseHealth;
        switch (teamBase.BrawlTeam)
        {
            case BrawlTeam.Blue:
                blueBase = teamBase;
                blueBase.onHealthChanged += (float health) =>
                {
                    blueTeamHealth.Value = health/defaultMaxBaseHealth;
                };
                break;
            case BrawlTeam.Red:
                redBase = teamBase;
                redBase.onHealthChanged += (float health) =>
                {
                    redTeamHealth.Value = health / defaultMaxBaseHealth;
                };
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
    public bool CanDamage(ulong networkId)
    {
        return true;
    }
    void OnPlayerDeath(ulong killerNoId, ulong killedNoId)
    {
        NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(killerNoId, out NetworkObject killerPlayerNO);
    
    }
// Start is called before the first frame update
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

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
