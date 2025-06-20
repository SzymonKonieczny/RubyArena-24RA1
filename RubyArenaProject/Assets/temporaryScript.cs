using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

using System;
public class temporaryScript : NetworkBehaviour
{
    public GameObject skillPrefab;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && IsServer)
        {
            ulong id = other.gameObject.GetComponent<NetworkBehaviour>().NetworkObjectId;
            GiveScriptServerRPC(id);
        }
    }
    private void Ontrigger(Collision collision)
    {
        if(collision.collider.CompareTag("Player") && IsServer)
        {
            ulong id = collision.collider.gameObject.GetComponent<NetworkBehaviour>().NetworkObjectId;
            GiveScriptServerRPC(id);
        }
    }
    [ServerRpc(RequireOwnership = false)]
    void GiveScriptServerRPC(ulong id)
    {
        //GiveScriptClientRPC(id);
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id, out var networkObject))
        {
            GameObject go = networkObject.gameObject;
            var skillHolder = go.GetComponentInChildren<PlayerSkillHolder>().transform;

            GameObject skillpref = Instantiate(skillPrefab);
            skillpref.GetComponent<NetworkObject>().Spawn();
            skillpref.transform.SetParent(skillHolder);

            var das = skillpref.GetComponent<TestSkillNewSystem>();
            das.Init();
        }
    }
    [ClientRpc]
    void GiveScriptClientRPC(ulong id)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id, out var networkObject))
        {
            GameObject go = networkObject.gameObject;
            var skillHolder = go.GetComponentInChildren<PlayerSkillHolder>().transform;

            GameObject skillpref = Instantiate(skillPrefab);
            skillpref.GetComponent<NetworkObject>().Spawn();
            skillpref.transform.SetParent(skillHolder);

            var das = skillpref.GetComponent<TestSkillNewSystem>();
        }
    }
}
