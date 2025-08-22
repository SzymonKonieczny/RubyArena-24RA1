using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class DespawnDestroyAfterDelay : NetworkBehaviour
{
    public float Time;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if(IsServer)
        {
            StartCoroutine(DestroyCoroutine());
        }
    }

    IEnumerator DestroyCoroutine()
    {
        yield return new WaitForSeconds(Time);
        NetworkObject.Despawn(true);
    }

}
