using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class WeissDashSkill : SkillBase
{
    public int ForceAdded = 50;
    public override bool Use()
    {
    
     
        //animationScript.PlayState("jumping");
        InputCollector.StunTime = 0.3f;
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
        }
    }
    private void Start()
    {
        Init();
    }
    private void Update()
    {
        cooldown -= Time.deltaTime;
        if (InputCollector == null || combatManagerRef == null || cooldown > 0)
            return;

        if (InputCollector.EClick && combatManagerRef.IsLocalPlayer)
        {
            Use();
            cooldown = 1f;
        }
    }
}
