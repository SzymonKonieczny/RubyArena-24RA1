using Assets.Scripts.Events;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using VContainer;

public class SlowingAreaSkillCarrier : SkillBase
{
    public GameObject blastEffect;
    [SerializeField] ISkillEffect FlamingGloves;


    void Start()
    {


    }
    private void OnTransformParentChanged()
    {
        Init();
    }

    // Update is called once per frame
    void Update()
    {
        if (InputCollector == null || combatManagerRef == null || isOnCooldown() || !combatManagerRef.IsOwner)
            return;

        if (spellTriggeringFlag.value)
        {
            Use();
        }
    }

    public override bool Use()
    {
        animationScript = combatManagerRef.animationScript;

        animationScript.Trigger("WindUp");
        combatManagerRef.SetStunTimer(windupTime);

        Vector3 LookDir = getLookDirection();
        combatManagerRef.playerMove.AddNetworkRbVelocityClientRPC(-LookDir * 5);
        ServerSideUseServerRPC(LookDir);

        return true;
    }

    [ServerRpc]
    public override void ServerSideUseServerRPC(Vector3 lookDir, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        if (isOnCooldown()) return;

        Vector3 skillOrigin = combatManagerRef.SkillshotSpawnPoint.position;
        ulong senderNetworkObjectId = combatManagerRef.NetworkObjectId;
        setCooldown(cooldown);

       

        ServerAnnounceSpellCastClientRPC();
    }

    [ClientRpc]
    public override void ServerAnnounceSpellCastClientRPC()
    {
        //if (IsServer) return;
        if (IsOwner)
        {
            animationScript.Trigger("SpellAcknowledge2");
        }
        else
        {
            animationScript.Trigger("WindUp");
            animationScript.Trigger("SpellAcknowledge2");
        }
    }
}
