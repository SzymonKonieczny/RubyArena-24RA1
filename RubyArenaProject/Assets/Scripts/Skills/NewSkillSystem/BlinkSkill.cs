using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class BlinkSkill : SkillBase
{
    Vector3 jumpPositionRequest;
    [SerializeField] ParticleSystem effect;
    [SerializeField] float delayToTeleport = 0.2f;

    PropertyBlockMaterial propertyBlockMaterial;
    [SerializeField] SkinnedMeshRenderer[] skinnedRenderers;

    float currentDissolveProgression = 1; //initializes as dissapeared ^^
    NetworkVariable<int> targetDissolveProgression = new(0);
    public override void Init()
    {
        base.Init();
        skinnedRenderers = combatManagerRef.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        propertyBlockMaterial = new PropertyBlockMaterial(skinnedRenderers);

    }
    public override bool Use()
    {
        RaycastHit rayHit = getRayHit();
        var worldPosCamera = Camera.main.ScreenToWorldPoint(new Vector3((float)Screen.width / 2f, (float)Screen.height / 2f));
        if (Vector3.Distance(combatManagerRef.transform.position, rayHit.point) > 15 || rayHit.distance == 0 )
        {
            var ray = Camera.main.ScreenPointToRay(new Vector3((float)Screen.width / 2f, (float)Screen.height / 2f));
            jumpPositionRequest = combatManagerRef.transform.position+ ( ray.direction.normalized * 15 + new Vector3(0, 2, 0));
            Debug.DrawLine(worldPosCamera, worldPosCamera +  ray.direction.normalized * 10, Color.green, 3);
        }
        else
        {
            jumpPositionRequest = rayHit.point + new Vector3(0, 2, 0); 
            Debug.DrawLine(worldPosCamera, rayHit.point, Color.green, 3);
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
        yield return new WaitForSeconds(delayToTeleport);
        combatManagerRef.transform.position = position;

    }
    [ServerRpc]
    void ServerSideUseServerRPC(ServerRpcParams rpcParams = default)
    {
        if (isOnCooldown()) return;
        setCooldown(cooldown);
        ServerExecuteSpellCastClientRPC();
        StartCoroutine(DissapperaAndAppearServer());
    }
    IEnumerator DissapperaAndAppearServer()
    {
        targetDissolveProgression.Value = 1;

        yield return new WaitForSeconds(delayToTeleport*2);

        targetDissolveProgression.Value = 0;

    }

    [ClientRpc]
    void ServerExecuteSpellCastClientRPC()
    {
        if(animationScript == null)
        {
            Init();

        }
        //if (IsServer) return;
        animationScript.Trigger("WindUp");
        animationScript.Trigger("SpellAcknowledge2");


       // effect.Play();
        if (IsOwner)
        {
            StartCoroutine(Jump(jumpPositionRequest));
        }
    }
    private void Start()
    {
        Init();
    }
    private void Update()
    {
        //Fixed update? Doesnt need updating EVRY frame


        if (currentDissolveProgression != targetDissolveProgression.Value)
        {
            propertyBlockMaterial.SetFloat("_Progress", currentDissolveProgression -0.01f);
        }
        float sign = (targetDissolveProgression.Value - currentDissolveProgression) > 0 ? 1 : -1;
        currentDissolveProgression += Time.deltaTime * sign * 5;
        currentDissolveProgression = Mathf.Clamp01(currentDissolveProgression);




        if (InputCollector == null || combatManagerRef == null || isOnCooldown() || !combatManagerRef.IsOwner)
            return;

        if (spellTriggeringFlag.value && combatManagerRef.IsOwner)
        {
            Use();
        }
    }
}
