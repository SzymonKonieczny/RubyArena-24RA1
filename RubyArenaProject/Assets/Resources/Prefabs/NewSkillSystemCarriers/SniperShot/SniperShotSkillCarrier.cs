using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SniperShotSkillCarrier : SkillBase
{
    [SerializeField] ISkillEffect shotEffect;
    private void OnTransformParentChanged()
    {
        Init();
        shotEffect = combatManagerRef.transform.GetComponentInChildren<ISkillEffect>();
    }
    public override void Init()
    {
        base.Init();
        SkillDataSO.damage = 40;
    }
 

    public override bool Use()
    {
        animationScript = combatManagerRef.animationScript;

        animationScript.Trigger("WindUp");
        combatManagerRef.SetStunTimer(windupTime);

        Vector3 LookDir = getLookDirection();

        ServerSideUseServerRPC(LookDir, this.NetworkObjectId);

        return true;
    }

    [ServerRpc]
    void ServerSideUseServerRPC(Vector3 lookDir, ulong senderId, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        if (isOnCooldown()) return;
        setCooldown(cooldown);


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
        if (IsOwner)
        {
            //combatManagerRef.playerMove.AddNetworkRbForceClientRPC((combatManagerRef.playerMove.Orientation.forward * ForceAdded ) + new Vector3(0, 1f, 0));
            animationScript.Trigger("SpellAcknowledge1");
            shotEffect.PlayEffect(0);
            combatManagerRef.playerMove.SnapModelToCameraDir();
        }
        else
        {
            animationScript.Trigger("WindUp");
            animationScript.Trigger("SpellAcknowledge1");
            shotEffect.PlayEffect(0);
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
