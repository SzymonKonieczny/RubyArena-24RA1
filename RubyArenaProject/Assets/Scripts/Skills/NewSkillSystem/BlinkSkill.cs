using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class BlinkSkill : SkillBase
{
    Vector3 posOffset;
    [SerializeField] ParticleSystem effect;
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
        combatManagerRef.SetStunTimer(windupTime);

        ServerSideUseServerRPC(); 
        return true;
    }
    private void OnTransformParentChanged()
    {
        Init();
    }
    IEnumerator Jump(Vector3 position)
    {
        yield return new WaitForSeconds(0.2f);
        combatManagerRef.transform.position += position;
    }
    [ServerRpc]
    void ServerSideUseServerRPC(ServerRpcParams rpcParams = default)
    {
        if (isOnCooldown()) return;
        nextAvaliableTicks.Value = System.DateTime.UtcNow.AddSeconds(cooldown).Ticks;
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
        effect.Play();
        if (IsOwner)
        {
            StartCoroutine(Jump(posOffset));
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

        if (spellTriggeringFlag.value && combatManagerRef.IsLocalPlayer)
        {
            Use();
        }
    }
}
