using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using VContainer;
using Assets.Scripts.Events;

public class TestGameModeManager : NetworkBehaviour, IGameMode
{
    Dictionary<PlayerScript, int> scoreBoard = new();
    [SerializeField] int pointsToWin = 3;
    [SerializeField] List<Transform> capturePointSpawnPositions = new();
    [SerializeField] Transform playerRespawn;
    [SerializeField] TMPro.TMP_Text victoryText;
    [SerializeField] TMPro.TMP_Text scoreDisplay;
    Dictionary<ulong, int> clientScoreBoard = new();

    NetworkVariable<ulong> winningPlayerNetworkObjectId= new NetworkVariable<ulong>();
    [SerializeField] GameModeCaptureOwnedObjective capturePoint;

    [Inject]
    EventBus<EntityDeathEvent> entityDeathEvent;

    [Inject]
    EventBus<DamageTakenEvent> damageTakenEvent;


    EventBinding<EntityDeathEvent> deathEventBinding;
    EventBinding<DamageTakenEvent> damageEventBinding;

    public void RegisterNetworkedObject(ulong networkId)
    {
        if (!NetworkManager.Singleton.IsServer) return;
    }

    public void RegisterPlayer(ulong networkId)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkId, out NetworkObject playerNO))
        {

            PlayerScript playerScript = playerNO.GetComponent<PlayerScript>();

            playerScript.playerMove.RequestTeleportClientRPC(playerRespawn.position);
        }
        Debug.Log($"PLAYER WITH ID {networkId} REGISTERED!");

    }
  
    void OnPlayerDamaged(DamageTakenEvent eventData)
    {

    }
    void OnEntityDeath(ulong killerNoId, ulong killedNoId)
    {
        NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(killerNoId, out NetworkObject killerNO);
        NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(killedNoId, out NetworkObject killedNO);

        // Handle player death (respawn, scoring)
        if (killedNO != null)
        {
            PlayerScript killedPlayerScript = killedNO.GetComponent<PlayerScript>();
            if (killedPlayerScript != null)
            {
                killedPlayerScript.playerMove.RequestTeleportClientRPC(playerRespawn.position);
                killedPlayerScript.playerResources.Hp.Value = killedPlayerScript.playerResources.getMaxHP();
            }
        }

        // Only proceed with capture-point scoring if killer is a player
        if (killerNO != null && killerNO.GetComponent<PlayerScript>() != null)
        {
            capturePoint.playerAssignedToCapturePoint.Value = killerNoId;
            capturePoint.active.Value = true;
            MoveCapture();
        }
    }

    void Start()
    {
        winningPlayerNetworkObjectId.OnValueChanged += AnnounceWinner;

        if (!NetworkManager.Singleton.IsServer) return;

        var gameModeGameObjects= GameObject.FindGameObjectsWithTag("GameModeRelatedObject");
        foreach(var GMGO in gameModeGameObjects)
        {
            capturePoint = GMGO.GetComponent<GameModeCaptureOwnedObjective>();
            if(capturePoint != null)
            {
                capturePoint.onPlayerCapturedObjective += playerCapturedPoint;

                int randomIndex = Random.Range(0, capturePointSpawnPositions.Count);
                capturePoint.transform.position = capturePointSpawnPositions[randomIndex].position;
            }    
        }
        deathEventBinding = new EventBinding<EntityDeathEvent>((eventData) =>
            OnEntityDeath(eventData.killerNetworkObjectId, eventData.killedNetworkObjectId)
        );
        damageEventBinding = new EventBinding<DamageTakenEvent>(OnPlayerDamaged);

        entityDeathEvent.Register(deathEventBinding);
        damageTakenEvent.Register(damageEventBinding);

    }

    [ClientRpc]
    void setScoreboardPointClientRPC(ulong clientNOId, int score)
    {
       if(clientScoreBoard.ContainsKey(clientNOId))
        {
            clientScoreBoard[clientNOId] = score;
        }
        else
        {
            clientScoreBoard.Add(clientNOId, score);
        }
        string scoreboardDisplayString = "";
       foreach(var pair in clientScoreBoard)
       {
            PlayerScript playerScript = NetworkManager.SpawnManager.SpawnedObjects[pair.Key]?.GetComponent<PlayerScript>();
            int? champId = playerScript?.characterID.Value;
            if(champId.HasValue && champId.Value >= 0)
            {
                string champName = CharacterList.Instance.Characters[champId.Value].name;
                scoreboardDisplayString += $"{champName}: {pair.Value}\n";
            }
       }
        scoreDisplay.text = scoreboardDisplayString;
    }
    int MoveCapture()
    {
        if (capturePointSpawnPositions.Count == 0)
        {
            Debug.LogError($"{name}: MoveCapture failed - no capture point spawn positions assigned on {nameof(TestGameModeManager)}.");
            return -1;
        }

        Vector3 currentPos = capturePoint.transform.position;
        List<int> validIndices = new();
        for (int i = 0; i < capturePointSpawnPositions.Count; i++)
        {
            Transform spawn = capturePointSpawnPositions[i];
            if (spawn != null && Vector3.Distance(currentPos, spawn.position) >= 1)
                validIndices.Add(i);
        }

        // Every configured position is too close to the current one; just move to a different one if possible.
        if (validIndices.Count == 0)
        {
            Debug.LogWarning($"{name}: MoveCapture found no spawn position at least 1 unit away - falling back to any other position. Check capturePointSpawnPositions.");
            for (int i = 0; i < capturePointSpawnPositions.Count; i++)
            {
                if (capturePointSpawnPositions[i] != null &&
                    Vector3.Distance(currentPos, capturePointSpawnPositions[i].position) > 0)
                    validIndices.Add(i);
            }
        }

        if (validIndices.Count == 0)
        {
            Debug.LogError($"{name}: MoveCapture failed - no usable spawn position (only one position, or all are null). Capture point stays at {currentPos}.");
            return -1;
        }

        int index = validIndices[Random.Range(0, validIndices.Count)];
        capturePoint.transform.position = capturePointSpawnPositions[index].position;
        return index;
    }
    void playerCapturedPoint(ulong playerObjectId, GameModeCaptureOwnedObjective capturedPoint)
    {

        if(NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerObjectId, out NetworkObject playerNO))
        {
            capturePoint.playerAssignedToCapturePoint.Value = this.NetworkObjectId; //Manager will never be a player so this a non-player networkId
            capturePoint.active.Value = false;

            PlayerScript player = playerNO.transform.GetComponent<PlayerScript>();
            if (player == null) return;
           if( !scoreBoard.TryAdd(player,1))
            {
                scoreBoard[player] += 1;

            }
            
           if(scoreBoard[player] >= pointsToWin)
            {
                capturedPoint.NetworkObject.Despawn();
                Debug.Log($"Player networkobjId: {player.NetworkObjectId} won!");
                OnVictory(player);
                return;
            }
            setScoreboardPointClientRPC(playerObjectId, scoreBoard[player]);

            MoveCapture();
        }

    }
    void AnnounceWinner(ulong oldV, ulong winnerNOId)
    {
        if (winnerNOId == 0) return;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(winnerNOId, out NetworkObject playerNO))
        {
            PlayerScript player = playerNO.transform.GetComponent<PlayerScript>();
            string name = CharacterList.Instance.Characters[player.characterID.Value].name;
            victoryText.text = name + " Won ! \n You will return to the lobby shortly";
        }

    }
    void OnVictory(PlayerScript victor )
    {
        if (!IsServer) return;
        winningPlayerNetworkObjectId.Value = victor.NetworkObjectId;
        StartCoroutine(BackToLobbyCoroutine());
    }
    IEnumerator BackToLobbyCoroutine()
    {
        yield return new WaitForSeconds(5);
        NetworkManager.Singleton.SceneManager.LoadScene("MultiplayerLobby", LoadSceneMode.Single);

    }
    // Update is called once per frame
    void Update()
    {
        
    }

    public bool CanDamage(ulong networkId)
    {
        return true;
    }
}
