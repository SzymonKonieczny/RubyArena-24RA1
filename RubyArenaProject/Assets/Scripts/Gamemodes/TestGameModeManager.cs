using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class TestGameModeManager : MonoBehaviour, IGameMode
{
    Dictionary<PlayerScript, int> scoreBoard = new();
    [SerializeField] int pointsToWin = 3;
    [SerializeField] List<Transform> capturePointSpawnPositions = new();
    public void RegisterObject(long networkId)
    {

    }

    public void RegisterPlayer(long networkId)
    {

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
