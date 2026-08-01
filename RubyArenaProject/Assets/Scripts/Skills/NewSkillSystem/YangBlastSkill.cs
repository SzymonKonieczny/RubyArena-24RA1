using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;
using VContainer;
using Assets.Scripts.Events;
using Unity.VisualScripting;

public class YangBlastSkill : SkillBase
{
    public GameObject blastEffect;
    [SerializeField]  NetworkVariable<float> storedDamage = new(0);
    [SerializeField] ISkillEffect FlamingGloves;

    [Inject]
    EventBus<DamageTakenEvent> damageTakenEvent;

    EventBinding<DamageTakenEvent> damageEventBinding;
    private void OnDestroy()
    {
        damageTakenEvent.Unrgister(damageEventBinding);
    }

    void Start()
    {
        FlamingGloves = combatManagerRef.GetComponentInChildren<ISkillEffect>();
        storedDamage.OnValueChanged += updateVFX;
        damageEventBinding = new EventBinding<DamageTakenEvent>((DamageTakenEvent data) => onDamageTaken(data.damageAmountPostMitigation));
        damageTakenEvent.Register(damageEventBinding);

    }
    private void OnTransformParentChanged()
    {
        Init();
    }

    void updateVFX(float oldStoredDamage, float newStoredDamage)
    {
        if (newStoredDamage > 30)
        {
            FlamingGloves.PlayEffect(0);
        }
        else
        {
            FlamingGloves.PlayEffect(1);
        }
    }

    void onDamageTaken(float damageAmount)
    {
        storedDamage.Value += damageAmount;
    }
    // Update is called once per frame
    void Update()
    {
        if (InputCollector == null || combatManagerRef == null || isOnCooldown()  || !combatManagerRef.IsOwner)
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

        GameObject skillEntityGO = Instantiate(blastEffect);

        skillEntityGO.GetComponent<NetworkObject>().Spawn();
        skillEntityGO.transform.SetPositionAndRotation(combatManagerRef.SkillshotSpawnPoint.transform.position + lookDir * 2, Quaternion.LookRotation(lookDir, Vector3.up));


        Collider[] overlaps = Physics.OverlapSphere(skillOrigin, 5);
        List<PlayerCombatManager> playerCombatManagers = new();
        foreach(var o in overlaps)
        {
            if(o.CompareTag("Player"))
            {
                var combatManager = o.transform.GetComponent<PlayerCombatManager>();
                if (combatManager && combatManagerRef != combatManager) playerCombatManagers.Add(combatManager);
            }
        }
        List<PlayerCombatManager> playersInRange = new();

        Debug.DrawLine(skillOrigin, skillOrigin + (lookDir.normalized * 5), Color.blue, 2f);
        foreach (var player in playerCombatManagers)
        {
            Vector3 toTarget = ( player.transform.position - skillOrigin);
            float angle = Mathf.Acos(Vector3.Dot(lookDir.normalized, toTarget.normalized));

            Debug.DrawLine(skillOrigin, skillOrigin + toTarget, Color.gray, 2f);

            Debug.Log($"angle : {angle}");
            if(Mathf.Abs(angle) < Mathf.Deg2Rad * 40)
            {
                playersInRange.Add(player);
                Debug.DrawLine(skillOrigin, skillOrigin + toTarget, Color.red,2f);
            }

        }

        foreach (var player in playersInRange)
        {
            var playerResources = player.GetComponent<PlayerResources>();
            if (!playerResources || player.NetworkObject.NetworkObjectId == senderNetworkObjectId) continue;

            Vector3 toTarget = (player.transform.position - skillOrigin);
            player.playerMove.AddNetworkRbVelocityClientRPC(toTarget.normalized * 10);


            var data = new SkillInstanceData
            {
                damage = this.damage + (int)storedDamage.Value,
                ownerNetworkObjectId = senderNetworkObjectId
            };
            playerResources.damage(data);
        }
        storedDamage.Value = 0;

        ServerAnnounceSpellCastClientRPC();
    }

    [ClientRpc]
    public override void ServerAnnounceSpellCastClientRPC()
    {
        //if (IsServer) return;
        if(IsOwner)
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
