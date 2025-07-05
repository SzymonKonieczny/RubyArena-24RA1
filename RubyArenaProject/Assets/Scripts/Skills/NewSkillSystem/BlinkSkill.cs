using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class BlinkSkill : SkillBase
{
    Vector3 posOffset;
    float cooldown = 0;
    public override bool Use()
    {
        RaycastHit rayHit = getRayHit();
        if (rayHit.distance > 10 || rayHit.distance == 0 )
        {
            var ray = Camera.main.ScreenPointToRay(new Vector3((float)Screen.width / 2f, (float)Screen.height / 2f));
            posOffset =  ray.direction.normalized * 10 + new Vector3(0, 3, 0);
        }
        else
        {
            posOffset = rayHit.point + new Vector3(0, 3, 0);
        }
        animationScript.PlayState("jumping");
        InputCollector.StunTime = 0.3f;
        ServerSideUseServerRPC();
        return true;
    }

    IEnumerator Jump(Vector3 position)
    {
        yield return new WaitForSeconds(0.2f);
        combatManagerRef.transform.position += position;
    }
    [ServerRpc]
    void ServerSideUseServerRPC(ServerRpcParams rpcParams = default)
    {
        ServerAnnounceSpellCastClientRPC();
    }

    [ClientRpc]
    void ServerAnnounceSpellCastClientRPC()
    {
        if(animationScript == null)
        {
            Init();
        }
        //if (IsServer) return;
        animationScript.PlayState("jumping");
        if (IsOwner)
        {
            StartCoroutine(Jump(posOffset));
        }
        

        // animationScript.Trigger("SpellCastAccepted");

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
            cooldown = 5f;
        }
    }
}
