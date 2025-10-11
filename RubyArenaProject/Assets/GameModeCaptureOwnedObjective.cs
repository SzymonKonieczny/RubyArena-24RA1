using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;
using System.Linq;

public class GameModeCaptureOwnedObjective : NetworkBehaviour
{
    public NetworkVariable<ulong> playerAssignedToCapturePoint = new(0);
    public NetworkVariable<bool> active = new();
    [SerializeField] Material Green;
    [SerializeField] Material Red;
    [SerializeField] Material Invisible;
    [SerializeField] float timeInsideTracker;
    [SerializeField] Renderer renderer;

    public Action<ulong, GameModeCaptureOwnedObjective> onPlayerCapturedObjective; //player Who Captured, refernce to self (for repositioning)
    bool isPlayerInside = false;
    public float timeToCapture=5;
    public override void OnNetworkSpawn()
    { 
        base.OnNetworkSpawn();
        playerAssignedToCapturePoint.OnValueChanged += (ulong oldV, ulong newV) =>
        {
            var ownedNOList = NetworkManager.SpawnManager.GetClientOwnedObjects(NetworkManager.LocalClientId);
            bool isLocalForLocalPlayer = ownedNOList.Any(no => no.NetworkObjectId == newV);

            if (isLocalForLocalPlayer)
            {
                renderer.material = Green;
            }
            else
            {
                renderer.material = Red;
            }

            if (!IsServer) return;
            timeInsideTracker = 0;
            isPlayerInside = false;
        };
        active.OnValueChanged += (bool oldV, bool newV) =>
         {
             if(newV == false)
             {
                 renderer.material = Invisible;
                 timeInsideTracker = 0;
                 isPlayerInside = false;
                 if (!IsServer) return;
                 transform.position += new Vector3(0, -1000, 0);
             }

         };
    }
    private void FixedUpdate()
    {
        if (!IsServer || !active.Value) return;
        if(isPlayerInside)
            timeInsideTracker += Time.fixedDeltaTime;

        if(timeInsideTracker >= timeToCapture)
        {
            onPlayerCapturedObjective.Invoke(playerAssignedToCapturePoint.Value, this);
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || !active.Value) return;
        if (other.CompareTag("Player"))
        {
            var playerNO = other.transform.GetComponent<NetworkObject>();
            if (playerNO == null)
            {
                Debug.LogError("Player NetworkObject doesnt exist");
                return;
            }
            if(playerAssignedToCapturePoint.Value == playerNO.NetworkObjectId)
            {
                isPlayerInside = true;
            }

        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (!IsServer || !active.Value) return;
        if (other.CompareTag("Player"))
        {
            var playerNO = other.transform.GetComponent<NetworkObject>();
            if (playerNO == null)
            {
                Debug.LogError("Player NetworkObject doesnt exist");
                return;
            }

            if (playerAssignedToCapturePoint.Value == playerNO.NetworkObjectId)
            {
                isPlayerInside = false;
            }
        }
    }
}
