using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class TestSkillNewSystem : SkillBase
{
    GameObject testPrefab;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }
    private void OnTransformParentChanged()
    {
        Init();
    }

    public override bool Use()
    {
        animationScript = combatManagerRef.animationScript;

        float windupTime = 0.2f;
        animationScript.PlayState("WindUp", windupTime);
        InputCollector.StunTime = windupTime;
        Vector3 LookDir = getLookDirection();

        ServerSideUseServerRPC(LookDir);

        return true;
    }

    [ServerRpc]
    void ServerSideUseServerRPC(Vector3 lookDir, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;




        GameObject skillEntityGO = Instantiate(testPrefab);

        skillEntityGO.GetComponent<NetworkObject>().Spawn();
        skillEntityGO.transform.SetPositionAndRotation(combatManagerRef.SkillshotSpawnPoint.transform.position + lookDir * 2, Quaternion.LookRotation(lookDir, Vector3.up));
        var skillEntity = skillEntityGO.GetComponent<BaseSkillEntityBehavior>();
        skillEntity.SkillDataSO = ScriptableObject.CreateInstance<SkillDataSO>();
        skillEntity.SkillDataSO.damage = 5;
        ServerAnnounceSpellCastClientRPC(0);
    }
    async void SpawnEntityDelayed()
    {
         
    }
    [ClientRpc]
    void ServerAnnounceSpellCastClientRPC(ulong networkObjId)
    {
        //if (IsServer) return;
        animationScript.PlayState("Spellcast1");
        //animationScript.Trigger("SpellCastAccepted");

    }

    // Update is called once per frame
    void Update()
    {
        if (InputCollector == null || combatManagerRef == null)
            return;

        if (InputCollector.QClick && combatManagerRef.IsLocalPlayer) 
        {
            Use();
        }
    }

   
}
