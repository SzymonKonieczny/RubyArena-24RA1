using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;
using System.Linq;

public class GameModeCaptureObjective : NetworkBehaviour
{
    public Action<ulong, GameModeCaptureObjective> onPlayerCapturedObjective; //player Who Captured, refernce to self (for repositioning)
    private Dictionary<ulong, float> playerInAreaTimers = new(); //ulong - playerNOid, float-consecutive seconds in area
    private readonly List<ulong> pendingAdd = new();
    private readonly List<ulong> pendingRemove = new();
    public float timeToCaptureSeconds = 5;
    private void Start()
    {
        
    }
    private void FixedUpdate()
    {
        if (!IsServer) return;

        foreach(ulong id in pendingRemove)
        {
            if(playerInAreaTimers.ContainsKey(id))
            {
             playerInAreaTimers.Remove(id);
            }
        }
        foreach (ulong id in pendingAdd)
        {
            playerInAreaTimers.TryAdd(id,0.0f);

        }

        pendingRemove.Clear();
        pendingAdd.Clear();
        foreach (var playerId in playerInAreaTimers.ToList()) //this is stupid, but with playerInAreaTimers.Keys by some miracle    playerInAreaTimers[playerId] += Time.fixedDeltaTime; throws Collection Modified ;-;
        {
            playerInAreaTimers[playerId.Key] += Time.fixedDeltaTime;
            if(playerInAreaTimers[playerId.Key] > timeToCaptureSeconds)
            {
                playerInAreaTimers.Clear();

                onPlayerCapturedObjective?.Invoke(playerId.Key, this);
                return;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        if(other.CompareTag("Player"))
        {  
           var playerNO = other.transform.GetComponent<NetworkObject>();
           if (playerNO == null)
           {
               Debug.LogError("Player NetworkObject doesnt exist");
               return;
           }
          pendingAdd.Add(playerNO.NetworkObjectId);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (!IsServer) return;
        if (other.CompareTag("Player"))
        {
            var playerNO = other.transform.GetComponent<NetworkObject>();
            if (playerNO == null)
            {
                Debug.LogError("Player NetworkObject doesnt exist");
                return;
            }
           
          pendingRemove.Add(playerNO.NetworkObjectId);
        }
    }
}
