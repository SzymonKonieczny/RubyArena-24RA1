using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

public class YangBlastSkill : SkillBase
{
    [SerializeField] PlayerResources resources;
    public GameObject blastEffect;
    [SerializeField]  float storedDamage = 0;
    [SerializeField] ISkillEffect FlamingGloves;
    // Start is called before the first frame update
    void Start()
    {
        FlamingGloves = combatManagerRef.GetComponentInChildren<ISkillEffect>();
    }
    private void OnTransformParentChanged()
    {
        Init();

        resources = combatManagerRef.GetComponent<PlayerResources>();

       // if (IsServer)
        {
            if(resources)
            {
                resources.Hp.OnValueChanged += (float oldV, float newV) => {
                    if (newV > oldV) return; //no damage taken, heals
                    storedDamage += oldV-newV; 
                    if(storedDamage > 30)
                    {
                        FlamingGloves.PlayEffect(0);
                    }
                };
            }
        }
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
        ServerSideUseServerRPC(LookDir, combatManagerRef.SkillshotSpawnPoint.position, this.NetworkObjectId);

        return true;
    }

    [ServerRpc]
     void ServerSideUseServerRPC(Vector3 lookDir, Vector3 skillOrigin,ulong senderId, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        if (isOnCooldown()) return;
       

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
            if (!playerResources || player.NetworkObject.NetworkObjectId == senderId) continue;

            Vector3 toTarget = (player.transform.position - skillOrigin);
            player.playerMove.AddNetworkRbVelocityClientRPC(toTarget.normalized * 10);

            var SkillDataSO = ScriptableObject.CreateInstance<SkillDataSO>();


            SkillDataSO.damage = damage + (int)storedDamage;
            storedDamage = 0;
            SkillDataSO.ownerId = senderId;
            playerResources.damage(SkillDataSO);
        }

        ServerAnnounceSpellCastClientRPC(0);
    }

    [ClientRpc]
    void ServerAnnounceSpellCastClientRPC(ulong networkObjId)
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
        FlamingGloves.PlayEffect(1);
    }
}
