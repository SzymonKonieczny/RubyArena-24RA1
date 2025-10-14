using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class TestGameModeManager : NetworkBehaviour, IGameMode
{
    Dictionary<PlayerScript, int> scoreBoard = new();
    [SerializeField] int pointsToWin = 3;
    [SerializeField] List<Transform> capturePointSpawnPositions = new();
    [SerializeField] Transform playerRespawn;
    [SerializeField] TMPro.TMP_Text victoryText;
    NetworkVariable<ulong> winningPlayerNetworkObjectId= new NetworkVariable<ulong>();
    [SerializeField] GameModeCaptureOwnedObjective capturePoint;
    public void RegisterObject(ulong networkId)
    {
        if (!NetworkManager.Singleton.IsServer) return;

    }

    public void RegisterPlayer(ulong networkId)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkId, out NetworkObject playerNO))
        {

            PlayerScript playerScript = playerNO.GetComponent<PlayerScript>();
            playerScript.playerResources.onPlayerDeath+=OnPlayerDeath;
            playerScript.playerResources.onDamageDealt += OnPlayerDamaged;
            playerScript.playerMove.RequestTeleportClientRPC(playerRespawn.position);
        }
        Debug.Log($"PLAYER WITH ID {networkId} REGISTERED!");

    }
  
    void OnPlayerDamaged(ulong dealerNoId,ulong recieveNoId,float hpBefore, float hpAfter)
    {

    }
    void OnPlayerDeath(ulong killerNoId, ulong killedNoId)
    {
        NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(killerNoId, out NetworkObject killerPlayerNO);
        NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(killedNoId, out NetworkObject killedPlayerNO);
        if(killerPlayerNO== null )
        {
            Debug.LogError($" Killed player unobtainable");
            return;
        }

        PlayerScript killerPlayerScript = killerPlayerNO.GetComponent<PlayerScript>();
        PlayerScript killedPlayerScript = killedPlayerNO.GetComponent<PlayerScript>();

        killedPlayerScript.playerMove.RequestTeleportClientRPC(playerRespawn.position);
        killedPlayerScript.playerResources.Hp.Value = killedPlayerScript.playerResources.getMaxHP();

        if ( killerPlayerNO == null)
        {
            Debug.LogError($"Killer player unobtainable, probably skill doesnt set its data. Killer {killerPlayerNO}, Killed {killedPlayerNO}");
            return;
        }
        capturePoint.playerAssignedToCapturePoint.Value = killerNoId;
        capturePoint.active.Value = true;
        MoveCapture();
    }
    // Start is called before the first frame update
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

        StartCoroutine(BackToLobbyCoroutine());

    }
    int MoveCapture()
    {
        int randomIndex = Random.Range(0, capturePointSpawnPositions.Count);
        while (true)
        {
            if (Vector3.Distance(capturePoint.transform.position, capturePointSpawnPositions[randomIndex].position) < 1)
            {
                randomIndex = Random.Range(0, capturePointSpawnPositions.Count);
                continue;
            }
            capturePoint.transform.position = capturePointSpawnPositions[randomIndex].position;
            break;
        }
        return randomIndex;
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
            victoryText.text = name + " Won ! You can restart to play again xd";
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
        yield return new WaitForSeconds(10);
        NetworkManager.Singleton.SceneManager.LoadScene("MultiplayerLobby", LoadSceneMode.Single);

    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
