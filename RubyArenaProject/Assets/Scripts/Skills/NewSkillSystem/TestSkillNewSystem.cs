using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class TestSkillNewSystem : SkillBase
{
    [SerializeField] GameObject blakeShotPrefab;
    private void OnTransformParentChanged()
    {
        Init();

    }

    public override bool Use()
    {
        animationScript = combatManagerRef.animationScript;

        animationScript.Trigger("WindUp");
        combatManagerRef.SetStunTimer(windupTime);

        Vector3 LookDir = getLookDirection();

        ServerSideUseServerRPC(LookDir, combatManagerRef.NetworkObjectId);

        return true;
    }

    [ServerRpc]
    void ServerSideUseServerRPC(Vector3 lookDir,ulong senderNetworkObjectId, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        if (isOnCooldown()) return;
        setCooldown(cooldown);

        GameObject skillEntityGO = Instantiate(blakeShotPrefab);

        skillEntityGO.GetComponent<NetworkObject>().Spawn();
        skillEntityGO.transform.SetPositionAndRotation(combatManagerRef.SkillshotSpawnPoint.transform.position + lookDir * 2, Quaternion.LookRotation(lookDir, Vector3.up));
        var skillEntity = skillEntityGO.GetComponent<BaseSkillEntityBehavior>();
        skillEntity.ownerNetworkObjectId = senderNetworkObjectId;
        skillEntity.SkillDataSO = SkillDataSO;
        ServerAnnounceSpellCastClientRPC(0);
    }
    async void SpawnEntityDelayed()
    {
         
    }
    [ClientRpc]
    void ServerAnnounceSpellCastClientRPC(ulong networkObjId)
    {
        if (IsOwner)
        {
            animationScript.Trigger("SpellAcknowledge1");
        }
        else
        {
            animationScript.Trigger("WindUp");
            animationScript.Trigger("SpellAcknowledge1");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (InputCollector == null || combatManagerRef == null || isOnCooldown() || !combatManagerRef.IsOwner)
            return;

        if (spellTriggeringFlag.value && combatManagerRef.IsOwner) 
        {
            Use();
        }
    }

   
}
