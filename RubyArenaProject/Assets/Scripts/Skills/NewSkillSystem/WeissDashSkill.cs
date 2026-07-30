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

        ServerSideUseServerRPC(new Vector3()); //does not expect input
        return true;
    }
    private void OnTransformParentChanged()
    {
        Init();
    }

    [ServerRpc]
    public override void ServerSideUseServerRPC(Vector3 lookDir, ServerRpcParams rpcParams = default)
    {
        if (isOnCooldown()) return;
        setCooldown(cooldown);

        ServerAnnounceSpellCastClientRPC();
    }

    [ClientRpc]
    public override void ServerAnnounceSpellCastClientRPC()
    {
        if (animationScript == null)
        {
            Init();
        }
        if (IsOwner)
        {
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
        if (InputCollector == null || combatManagerRef == null || isOnCooldown() || !combatManagerRef.IsOwner)
            return;

        if (spellTriggeringFlag.value)
        {
            Use();
        }
    }
}
