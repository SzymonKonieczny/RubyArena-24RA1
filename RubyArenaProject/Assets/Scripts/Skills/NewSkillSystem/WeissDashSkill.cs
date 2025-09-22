using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class WeissDashSkill : SkillBase
{
    public int ForceAdded = 50;
    public override bool Use()
    {
        animationScript.Trigger("WindUp");
        combatManagerRef.SetStunTimer(windupTime);

        ServerSideUseServerRPC();
        return true;
    }
    private void OnTransformParentChanged()
    {
        Init();
    }

    [ServerRpc]
    void ServerSideUseServerRPC(ServerRpcParams rpcParams = default)
    {
        if (isOnCooldown()) return;
        setCooldown(cooldown);

        ServerAnnounceSpellCastClientRPC();
    }

    [ClientRpc]
    void ServerAnnounceSpellCastClientRPC()
    {
        if (animationScript == null)
        {
            Init();
        }
        //if (IsServer) return;
        //animationScript.PlayState("jumping");
       // effect.Play();
        if (IsOwner)
        {
            //combatManagerRef.playerMove.AddNetworkRbForceClientRPC((combatManagerRef.playerMove.Orientation.forward * ForceAdded ) + new Vector3(0, 1f, 0));
            combatManagerRef.playerMove.startDash();
            animationScript.Trigger("SpellAcknowledge1");
        }
        else
        {
            animationScript.Trigger("WindUp");
            animationScript.Trigger("SpellAcknowledge1");
        }
    }
    private void Start()
    {
        Init();
    }
    private void Update()
    {
        if (InputCollector == null || combatManagerRef == null || isOnCooldown() || !combatManagerRef.IsLocalPlayer)
            return;

        if (spellTriggeringFlag.value)
        {
            Use();
        }
    }
}
