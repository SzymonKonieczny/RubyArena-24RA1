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
    NetworkVariable<int> targetDissolveProgression = new(0);


    public override bool Use() 
    {
        ServerSideUseServerRPC(new Vector3()); //doesnt expect input
        animationScript.PlayState("Jumping");

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

 
    [ServerRpc]
    public override void ServerSideUseServerRPC(Vector3 lookDir, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        if (isOnCooldown()) return;
        setCooldown(cooldown);

        targetDissolveProgression.Value = 1;

        StartCoroutine(UseWithCastTime());
    }

    [ClientRpc]
    public override void ServerAnnounceSpellCastClientRPC()
    {
        if (animationScript == null || skinnedRenderers.Length == 0)
        {
            Init();
        }

        skillHolderRef.statModifierHolder.AddModifier(new EntityStatisticsModifier()
        {
            speedMultiplier = 3,
            serverTimeEffectEnd = NetworkManager.Singleton.ServerTime.Time + 3f
        });

        StartCoroutine(this.FormSwap(duration));
    }
    IEnumerator UseWithCastTime()
    {
        yield return new WaitForSeconds(windupTime);
        ServerAnnounceSpellCastClientRPC();
    }
    IEnumerator FormSwap (float duration)
    {
        //TODO:
        // MOVE THIS TO AN IMPLEMENTATION AS AN "EFFECT"

       // combatManagerRef.playerMove.Rb.drag = 3;
       // combatManagerRef.playerMove.Rb.useGravity = false;
       // combatManagerRef.playerMove.speed *= 3;
       // combatManagerRef.playerMove.canFly = true;
       

        ballRenderer.enabled = true;
        roseParticles.Play();
        yield return new WaitForSeconds(duration);
        targetDissolveProgression.Value = 0; // set on animation start
        ballRenderer.enabled = false;


       // combatManagerRef.playerMove.Rb.drag = 0;
       // combatManagerRef.playerMove.canFly = false;
       // combatManagerRef.playerMove.speed /= 3;
       // combatManagerRef.playerMove.Rb.useGravity = true;
    }

    private void Start()
    {
        Init();
    }
    private void Update()
    {
        //Fixed update? Doesnt need updating EVRY frame
        float sign = (targetDissolveProgression.Value - currentDissolveProgression) > 0 ? 1 : -1;
        currentDissolveProgression += Time.deltaTime * sign * 2;
        currentDissolveProgression = Mathf.Clamp01(currentDissolveProgression);
      
        if (currentDissolveProgression != targetDissolveProgression.Value)
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
