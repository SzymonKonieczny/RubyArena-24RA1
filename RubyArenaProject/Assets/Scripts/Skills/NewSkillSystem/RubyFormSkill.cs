using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class RubyFormSkill : SkillBase
{
  //  [SerializeField] MeshRenderer characterModelMesh;
    [SerializeField] ParticleSystem roseParticles;
    [SerializeField] Renderer ballRenderer;
    [SerializeField] float duration;
    PropertyBlockMaterial propertyBlockMaterial;
    [SerializeField] SkinnedMeshRenderer[] skinnedRenderers;
    
    float currentDissolveProgression = 1; //initializes as dissapeared ^^
    float targetDissolveProgression = 0;

    public override bool Use() 
    {
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
        ballRenderer.enabled = false;
        skinnedRenderers = combatManagerRef.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        propertyBlockMaterial = new PropertyBlockMaterial(skinnedRenderers);
        ParticleSystem.MainModule main = roseParticles.main;
        main.duration = duration;
    }
    [ClientRpc]
    void ClientSideAcknowledgeSpellStartClientRPC()
    {
        animationScript.PlayState("Jumping");
        targetDissolveProgression = 1;

    }

    [ServerRpc]
    void ServerSideUseServerRPC(ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        if (isOnCooldown()) return;
        setCooldown(cooldown);

        StartCoroutine(UseWithCastTime());
    }
    IEnumerator UseWithCastTime()
    {
        ClientSideAcknowledgeSpellStartClientRPC();
        yield return new WaitForSeconds(windupTime);
        ServerAnnounceSpellCastClientRPC();

    }


    [ClientRpc]
    void ServerAnnounceSpellCastClientRPC()
    {
        if (animationScript == null || skinnedRenderers.Length == 0)
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
        /*foreach(var r in renderers)
        {
            r.enabled = false;
        }*/
        ballRenderer.enabled = true;
        roseParticles.Play();
        yield return new WaitForSeconds(duration);
        targetDissolveProgression = 0; // set on animation start
        ballRenderer.enabled = false;

        /* foreach (var r in renderers)
         {
             r.enabled = true;
         }*/
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
        //Fixed update? Doesnt need updating EVRY frame
        float sign = (targetDissolveProgression - currentDissolveProgression) > 0 ? 1 : -1;
        currentDissolveProgression += Time.deltaTime * sign * 2;
        currentDissolveProgression = Mathf.Clamp01(currentDissolveProgression);
      
        if (currentDissolveProgression != targetDissolveProgression)
        {
            propertyBlockMaterial.SetFloat("_Progress",currentDissolveProgression);
        }

        
        
        
        
        if (InputCollector == null || combatManagerRef == null || isOnCooldown() || !combatManagerRef.IsOwner)
        return;

        if (spellTriggeringFlag.value && combatManagerRef.IsOwner)
        {
            Use();
        }
    }
}
