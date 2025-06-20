using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;


public abstract class SkillBase : NetworkBehaviour
{
    public PlayerCombatManager combatManagerRef;

    public PlayerAnimationScript animationScript;

    public InputCollectorScript InputCollector;

    [SerializeField] public SkillDataSO SkillDataSO;

    public abstract bool Use();
    protected Vector3 getLookDirection()
    {
        var ray = Camera.main.ScreenPointToRay(new Vector3((float)Screen.width / 2f, (float)Screen.height / 2f));
        Vector3 SkillDir = new();
        if (Physics.Raycast(ray, out RaycastHit raycastHit))
        {
            SkillDir = (raycastHit.point - this.combatManagerRef.SkillshotSpawnPoint.position).normalized;
        }
        else
        {
            SkillDir = ray.direction;
        }
        return SkillDir;
    }
/*      General Usage Recommendations :
     bool Use()
    {
        Play Animation For Windup

        Get Direction etc

        Send an RPC to teh server to ask for exetution.
    }

    [ServerRpc]
    void ServerSideUseServerRPC(Vector3 lookDir, ServerRpcParams rpcParams = default)
    {
       
        Execute, or scheldue (with an async for delay) execution

        Announce it was used so the clients can play the animations

    }

    [ClientRpc]
    void ServerAnnounceSpellCastClientRPC(ulong networkObjId)
    {
        animationScript.PlayState("SpellCast1");

    }*/
}
