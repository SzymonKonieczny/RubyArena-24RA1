using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SniperShotSkillCarrier : SkillBase
{
    private void OnTransformParentChanged()
    {
        Init();
    }
    public override void Init()
    {
        base.Init();
        SkillDataSO.damage = 40;
    }

    public override bool Use()
    {
        animationScript = combatManagerRef.animationScript;

        float windupTime = 0.2f;
        animationScript.PlayState("WindUp", windupTime);
        InputCollector.StunTime = windupTime;
        Vector3 LookDir = getLookDirection();

        ServerSideUseServerRPC(LookDir, this.NetworkObjectId);

        return true;
    }

    [ServerRpc]
    void ServerSideUseServerRPC(Vector3 lookDir, ulong senderId, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;

        var collider = Physics.Raycast(new Ray(combatManagerRef.SkillshotSpawnPoint.position, lookDir),out RaycastHit hit, 500);
        
        if(hit.collider.CompareTag("Player"))
        {
            var playerResources = hit.collider.transform.GetComponent<UnitResource>();
            if (!playerResources || playerResources.NetworkObject.NetworkObjectId == senderId) return;


            SkillDataSO.ownerId = senderId;
            playerResources.damage(SkillDataSO);
        }


        ServerAnnounceSpellCastClientRPC(0);
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
        if (InputCollector == null || combatManagerRef == null || cooldown > 0 || !combatManagerRef.IsLocalPlayer)
            return;

        if (spellTriggeringFlag.value && combatManagerRef.IsLocalPlayer)
        {
            Use();
        }
    }
}
