using System.Collections;
using UnityEngine;
using Unity.Netcode;
using VContainer;
using Assets.Scripts.Events;

/// <summary>
/// Riposte: Neo enters a brief counter window. If she takes damage during it,
/// she vanishes, teleports behind the attacker, and strikes them with her parasol.
/// If the window expires without a hit, the skill goes on a reduced cooldown.
/// </summary>
public class RiposteSkill : SkillBase
{
    [Header("Riposte Settings")]
    [SerializeField] float counterWindow = 0.75f;
    [SerializeField] float reducedCooldown = 2f;
    [SerializeField] float fullCooldown = 8f;
    [SerializeField] float stunDuration = 0.5f;
    [SerializeField] float behindOffset = 1.5f;
    [SerializeField] float invisibilityDuration = 0.3f;

    [Header("VFX")]
    [SerializeField] GameObject counterReadyVFX;    // subtle aura during window
    [SerializeField] GameObject teleportVFX;        // glass-shatter blink
    [SerializeField] GameObject strikeVFX;          // parasol swing trail
    [SerializeField] float shieldEdgeSize = 0.3f;


    bool isRiposting = false;
    bool counterTriggered = false;

    [Inject]
    EventBus<DamageTakenEvent> damageEventBus;
    EventBinding<DamageTakenEvent> damageBinding;

    PropertyBlockMaterial propertyBlockMaterial;
    [SerializeField] SkinnedMeshRenderer[] skinnedRenderers;
    [SerializeField]  float currentDissolveProgression = 0;
    [SerializeField]  NetworkVariable<float> targetDissolveProgression = new(-0.3f);

    // --- Lifetime ---

    void Start()
    {
        Init();
        damageBinding = new EventBinding<DamageTakenEvent>(OnDamageTaken);
        damageEventBus?.Register(damageBinding);
    }

    public override void Init()
    {
        base.Init();
        skinnedRenderers = combatManagerRef.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        propertyBlockMaterial = new PropertyBlockMaterial(skinnedRenderers);
        propertyBlockMaterial.SetFloat("_Progress", currentDissolveProgression);
        propertyBlockMaterial.SetFloat("_edge", shieldEdgeSize);
        propertyBlockMaterial.SetFloat("_alphaThresholdAdjustment", 0.3f);
        
        if(IsServer)
            targetDissolveProgression.Value = -shieldEdgeSize;
    }

    void OnDestroy()
    {
        damageEventBus?.Unregister(damageBinding);
    }

    private void OnTransformParentChanged()
    {
        Init();
    }

    // --- Input ---

    void Update()
    {

        // Dissolve update (all clients)
        if (currentDissolveProgression != targetDissolveProgression.Value)
        {
            propertyBlockMaterial.SetFloat("_Progress", currentDissolveProgression);
        }
        float sign = (targetDissolveProgression.Value - currentDissolveProgression) > 0 ? 1 : -1;
        currentDissolveProgression += Time.deltaTime * sign * 5;

        if (currentDissolveProgression < -shieldEdgeSize) //clamp between -shieldEdgeSize and 1
            currentDissolveProgression = -shieldEdgeSize; 
        else if (currentDissolveProgression > 1)
                currentDissolveProgression = 1;




        if (InputCollector == null || combatManagerRef == null || !combatManagerRef.IsOwner)
            return;

        if (spellTriggeringFlag.value && !isOnCooldown())
        {
            Use();
        }

    }

    // --- Skill Execution ---

    public override bool Use()
    {
        animationScript.Trigger("WindUp");
        combatManagerRef.SetStunTimer(counterWindow);

        ServerSideUseServerRPC(new Vector3());
        return true;
    }

    [ServerRpc]
    public override void ServerSideUseServerRPC(Vector3 lookDir, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        if (isOnCooldown()) return;

        StartCoroutine(CounterWindow());
    }

    IEnumerator CounterWindow()
    {
        isRiposting = true;
        counterTriggered = false;
        targetDissolveProgression.Value =0;

        ServerAnnounceSpellCastClientRPC();
        ShowCounterReadyClientRPC();

        yield return new WaitForSeconds(counterWindow);

        isRiposting = false;
        targetDissolveProgression.Value = -shieldEdgeSize;

        float cd = counterTriggered ? fullCooldown : reducedCooldown;
        nextAvaliable.Value = NetworkManager.NetworkTimeSystem.ServerTime + cd;

        HideCounterReadyClientRPC();
    }

    // --- Damage Handling ---

    void OnDamageTaken(DamageTakenEvent e)
    {
        if (!IsServer) return;
        if (!isRiposting) return;
        if (e.recieverNetworkObjectId != combatManagerRef.NetworkObjectId) return;
        if (counterTriggered) return;

        counterTriggered = true;
        targetDissolveProgression.Value = 1f;
        // Find the attacker by their network object ID
        
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(e.damagerNetworkObjectId, out var attackerObj))
            return;

        PlayerCombatManager attackerCombat = attackerObj.GetComponent<PlayerCombatManager>();
        if (attackerCombat == null) return;

        ExecuteRiposteClientRPC(e.damagerNetworkObjectId);

        // Server-side: damage and stun after the teleport delay
        StartCoroutine(ApplyRiposteEffects(attackerCombat));
    }

    IEnumerator ApplyRiposteEffects(PlayerCombatManager attacker)
    {
        yield return new WaitForSeconds(invisibilityDuration);

        if (attacker == null) yield break;

        UnitResource attackerResource = attacker.GetComponent<UnitResource>();
        if (attackerResource != null)
        {
            attackerResource.damage(new SkillInstanceData
            {
                damage = damage,
                ownerNetworkObjectId = combatManagerRef.NetworkObjectId
            });
        }

        // Stun
        attacker.SetStunTimer(stunDuration);
    }

    // --- Client RPCs ---

    [ClientRpc]
    void ShowCounterReadyClientRPC()
    {
        if (counterReadyVFX != null)
        {
            GameObject vfx = Instantiate(counterReadyVFX, combatManagerRef.transform);
            Destroy(vfx, counterWindow);
        }
    }

    [ClientRpc]
    void HideCounterReadyClientRPC()
    {
        // VFX self-destructs via Destroy(vfx, counterWindow) above
    }

    [ClientRpc]
    void ExecuteRiposteClientRPC(ulong attackerNetworkObjectId)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(attackerNetworkObjectId, out var attackerObj)) 
            return;

        // Teleport behind attacker
        Vector3 attackerPos = attackerObj.transform.position;
        Vector3 attackerForward = attackerObj.transform.forward;
        Vector3 behindPos = attackerPos - attackerForward * behindOffset;
        behindPos.y = combatManagerRef.transform.position.y;

        // Face the attacker
        combatManagerRef.transform.position = behindPos;
        combatManagerRef.transform.rotation = Quaternion.LookRotation(attackerForward);

        // Invisibility flash
        StartCoroutine(BriefInvisibility());

        // VFX
        if (teleportVFX != null)
        {
            GameObject vfx = Instantiate(teleportVFX, combatManagerRef.transform.position, Quaternion.identity);
            Destroy(vfx, 2f);
        }

        // Animation
        animationScript.Trigger("SpellAcknowledge1");

        if (strikeVFX != null)
        {
            GameObject vfx = Instantiate(strikeVFX, combatManagerRef.SkillshotSpawnPoint.position, combatManagerRef.transform.rotation);
            Destroy(vfx, 1f);
        }
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
            animationScript.Trigger("SpellAcknowledge1");
        }
        else
        {
            animationScript.Trigger("WindUp");
            animationScript.Trigger("SpellAcknowledge1");
        }
    }

    IEnumerator BriefInvisibility()
    {
        targetDissolveProgression.Value = 1;

        yield return new WaitForSeconds(invisibilityDuration);

        if (combatManagerRef == null) yield break;

        targetDissolveProgression.Value = -shieldEdgeSize;
    }
}
