using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class GameModeManager 
{
    public static  GameModeManager instance;
    public GameModeType CurrentGameMode;
    private GameModeManager() {

        OnPlayerTakeDmg = new Action<PlayerScript, float, PlayerScript?>(HandlePlayerTakeDmg);
        OnPlayerDeath = new Action<PlayerScript>(HandlePlayerDeath);
    }

    public static GameModeManager Instance()
     {
        if(instance == null)
        {
            instance = new GameModeManager();
        }
        return instance;
    }


    /// <summary>
    /// On player take damage. PlayerTakingDmg, amount, PlayerDealingDmg (nullable)
    /// </summary>
    public Action<PlayerScript,float, PlayerScript?> OnPlayerTakeDmg;

    /// <summary>
    /// On player take damage. PlayerDying, Player Killing (nullable)
    /// </summary>
    public Action<PlayerScript> OnPlayerDeath;

    private void HandlePlayerTakeDmg(PlayerScript PlayerTakingDmg, float famage  , PlayerScript? PlayerDealingDmg)
    {

    }
    private void HandlePlayerDeath(PlayerScript dyingPlayer)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        dyingPlayer.playerMove.PlayerDeathClientRPC();
        dyingPlayer.playerResources.Hp.Value = dyingPlayer.playerResources.getMaxHP();
    }

}
