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
        SkillDataSO.damage = damage;
        Debug.Log($"Damage set to {SkillDataSO.damage}");
    }


    public override bool Use()
    {
        animationScript = combatManagerRef.animationScript;

        animationScript.Trigger("WindUp");
        combatManagerRef.SetStunTimer(windupTime);

        Vector3 LookDir = getLookDirection();

        ServerSideUseServerRPC(LookDir);

        return true;
    }

    [ServerRpc]
    public override void ServerSideUseServerRPC(Vector3 lookDir, ServerRpcParams rpcParams = default)
    {
        if (isOnCooldown()) return;
        setCooldown(cooldown);
        if (!IsServer) return;

        var collider = Physics.Raycast(new Ray(combatManagerRef.SkillshotSpawnPoint.position, lookDir),out RaycastHit hit, 500);
        Debug.DrawLine(combatManagerRef.SkillshotSpawnPoint.position, combatManagerRef.SkillshotSpawnPoint.position + (lookDir * 100), Color.green,3f);
        if(hit.collider.CompareTag("Player"))
        {
            var playerResources = hit.collider.transform.GetComponent<UnitResource>();
            if (!playerResources || playerResources.NetworkObject.NetworkObjectId == combatManagerRef.NetworkObjectId) return;

            var data = new SkillInstanceData
            {
                damage = this.damage,
                ownerNetworkObjectId = combatManagerRef.NetworkObjectId
            };
            playerResources.damage(data);
        }


        ServerAnnounceSpellCastClientRPC();
    }

    [ClientRpc]
    public override void ServerAnnounceSpellCastClientRPC()
    {
        if (IsOwner)
        {
            //combatManagerRef.playerMove.AddNetworkRbForceClientRPC((combatManagerRef.playerMove.Orientation.forward * ForceAdded ) + new Vector3(0, 1f, 0));
            animationScript.Trigger("SpellAcknowledge1");
            shotEffect?.PlayEffect(0);
            combatManagerRef.playerMove.SnapModelToCameraDir();
        }
        else
        {
            animationScript.Trigger("WindUp");
            animationScript.Trigger("SpellAcknowledge1");
            shotEffect?.PlayEffect(0);
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
