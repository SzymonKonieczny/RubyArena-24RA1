using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class TestGameModeManager : MonoBehaviour, IGameMode
{
    Dictionary<PlayerScript, int> scoreBoard = new();
    [SerializeField] int pointsToWin = 3;
    [SerializeField] List<Transform> capturePointSpawnPositions = new();
    [SerializeField] Transform playerRespawn;
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
            playerScript.playerResources.Hp.OnValueChanged += (float prev, float newV) =>
                {
                    if (newV <= 0)
                    {
                        OnPlayerDeath(playerScript);
                    }
                };
        }
        Debug.Log($"PLAYER WITH ID {networkId} REGISTERED!");

    }

    void OnPlayerDeath(PlayerScript playerScript)
    {
        playerScript.playerMove.RequestTeleportClientRPC(playerRespawn.position);
        playerScript.playerResources.Hp.Value = playerScript.playerResources.getMaxHP();

    }
    // Start is called before the first frame update
    void Start()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        var gameModeGameObjects= GameObject.FindGameObjectsWithTag("GameModeRelatedObject");
        foreach(var GMGO in gameModeGameObjects)
        {
            GameModeCaptureObjective capturePoint = GMGO.GetComponent<GameModeCaptureObjective>();
            if(capturePoint != null)
            {
                capturePoint.onPlayerCapturedObjective += playerCapturedPoint;

                int randomIndex = Random.Range(0, capturePointSpawnPositions.Count);
                capturePoint.transform.position = capturePointSpawnPositions[randomIndex].position;
            }    
        }
    }
    void playerCapturedPoint(ulong playerObjectId, GameModeCaptureObjective capturedPoint)
    {

        if(NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerObjectId, out NetworkObject playerNO))
        {
            PlayerScript player = playerNO.transform.GetComponent<PlayerScript>();
            if (player == null) return;
           if( !scoreBoard.TryAdd(player,1))
            {
                scoreBoard[player] += 1;
            }

           if(scoreBoard[player] >= pointsToWin)
            {
                capturedPoint.NetworkObject.Despawn();
                Debug.Log($"Player {capturedPoint.gameObject} won!");
                return;
            }

            int randomIndex = Random.Range(0, capturePointSpawnPositions.Count);
            capturedPoint.transform.position = capturePointSpawnPositions[randomIndex].position;
        }

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
