using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class RubyFormSkill : SkillBase
{
  //  [SerializeField] MeshRenderer characterModelMesh;
    [SerializeField] ParticleSystem roseParticles;
    [SerializeField] Renderer[] renderers;
    [SerializeField] float duration;

    public override bool Use() 
    {


        //animationScript.PlayState("jumping");
        //InputCollector.StunTime = 0.3f;
        ServerSideUseServerRPC();
        return true;
    }
    private void OnTransformParentChanged()
    {
        Init();
    }

    override public void Init()
    {
        base.Init();
        renderers = combatManagerRef.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        ParticleSystem.MainModule main = roseParticles.main;
        main.duration = duration;
    }
    [ServerRpc]
    void ServerSideUseServerRPC(ServerRpcParams rpcParams = default)
    {
        ServerAnnounceSpellCastClientRPC();
    }

    [ClientRpc]
    void ServerAnnounceSpellCastClientRPC()
    {
        if (animationScript == null || renderers.Length == 0)
        {
          Init(); 
        } 
        //if (IsServer) return;
        //animationScript.PlayState("jumping");
        // effect.Play();
            //combatManagerRef.playerMove.AddNetworkRbForceClientRPC((combatManagerRef.playerMove.Orientation.forward * ForceAdded ) + new Vector3(0, 1f, 0));
         StartCoroutine(this.FormSwap(duration));
    }
    IEnumerator FormSwap (float duration)
    {
        combatManagerRef.playerMove.Rb.drag = 3;
        combatManagerRef.playerMove.Rb.useGravity = false;
        combatManagerRef.playerMove.speed *= 3;
        combatManagerRef.playerMove.canFly = true;
        foreach(var r in renderers)
        {
            r.enabled = false;
        }
        roseParticles.Play();

        yield return new WaitForSeconds(duration);

        foreach (var r in renderers)
        {
            r.enabled = true;
        }
        combatManagerRef.playerMove.Rb.drag = 0;
        combatManagerRef.playerMove.canFly = false;
        combatManagerRef.playerMove.speed /= 3;
        combatManagerRef.playerMove.Rb.useGravity = true;

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
