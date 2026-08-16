using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[System.Serializable]
public struct AutoAttackParams : INetworkSerializable, System.IEquatable<AutoAttackParams>
{
    public float AttackSpeed;
    public float Range;
    public int Damage;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsReader)
        {
            var reader = serializer.GetFastBufferReader();
            reader.ReadValueSafe(out AttackSpeed);
            reader.ReadValueSafe(out Range);
            reader.ReadValueSafe(out Damage);
        }
        else
        {
            var writer = serializer.GetFastBufferWriter();
            writer.WriteValueSafe(AttackSpeed);
            writer.WriteValueSafe(Range);
            writer.WriteValueSafe(Damage);
        }
    }

    public bool Equals(AutoAttackParams other)
    {
        return  AttackSpeed == other.AttackSpeed && Range == other.Range && Damage == other.Damage;
    }
}
public class AutoAttackSkillCarrier : SkillBase
{
    public NetworkVariable< AutoAttackParams> autoAttackParams = new();
    
    [ServerRpc]
    private void ParamsSyncRequestServerRpc() //Could be set before networkSpawn, but that would cause dumb if-ology in the spawning logic
    {
        autoAttackParams.SetDirty(true);
    }

    private void Start()
    {
        autoAttackParams.OnValueChanged += (_, _) =>autoAttackDataUpdate();
        autoAttackDataUpdate();
        if (IsOwner)
        {
          ParamsSyncRequestServerRpc();
        }
    }
    private void autoAttackDataUpdate()
    {
        damage = autoAttackParams.Value.Damage;
        cooldown = 1.0f / autoAttackParams.Value.AttackSpeed;
    }
    private void OnTransformParentChanged()
    {
        Init();
    }
    public override void Init()
    {
        base.Init();
        if (IsServer)
        {
            autoAttackParams.Value = new AutoAttackParams
            {
                AttackSpeed = skillHolderRef.Stats.modifiedStats.AttackSpeed,
                Damage = skillHolderRef.Stats.modifiedStats.AttackDamage,
                Range = skillHolderRef.Stats.modifiedStats.AttackRange,
            };
        }

        autoAttackDataUpdate();
        if (IsOwner)
        {
            Debug.Log($"Logging : {damage} dmg {cooldown} as");
        }
    }

    public override bool Use()
    {
        animationScript = combatManagerRef.animationScript;
       
        animationScript.Trigger("WindUp");
        combatManagerRef.SetStunTimer(windupTime);
        
        Vector3 LookDir = getLookDirection();

        ServerSideUseServerRPC(LookDir);

        return true;
    }

    [ServerRpc]
    public override void ServerSideUseServerRPC(Vector3 lookDir, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        if (isOnCooldown()) return;

        setCooldown(cooldown);

        Vector3 skillshotSpawnPos = skillHolderRef.playerCombatManager.SkillshotSpawnPoint.transform.position;

        var ovelappingColliders = Physics.OverlapBox(skillshotSpawnPos + (lookDir * autoAttackParams.Value.Range / 2),
            new Vector3(2, 1, autoAttackParams.Value.Range / 2), Quaternion.LookRotation(lookDir),rayCastMask);

        Assets.Scripts.Utility.DebugUtils.DrawBox(skillshotSpawnPos + (lookDir* autoAttackParams.Value.Range / 2),
            new Vector3(1, 1, autoAttackParams.Value.Range / 2), Quaternion.LookRotation(lookDir), Color.red); 
         //Gizmos.DrawWireCube(skillshotSpawnPos + (combatManagerRef.transform.forward * autoAttackParams.Value.Range / 2), new Vector3(2, 1, autoAttackParams.Value.Range / 2));

        //check for which of them are players
        List<PlayerCombatManager> playerCombatManagers = new();
        foreach (var o in ovelappingColliders)
        {
            if (o.CompareTag("Player"))
            {
                var combatManager = o.transform.GetComponent<PlayerCombatManager>();
                if (combatManager && combatManagerRef != combatManager) playerCombatManagers.Add(combatManager);
            }
        }


        //damage players with this dataSO;
        foreach (var player in playerCombatManagers)
        {
            var playerResources = player.GetComponent<UnitResource>();
            if (!playerResources || player.NetworkObject.NetworkObjectId == combatManagerRef.NetworkObjectId) continue;

            var data = new SkillInstanceData
            {
                damage = this.damage,
                ownerNetworkObjectId = combatManagerRef.NetworkObjectId
            };
            playerResources.damage(data);
        }

        ServerAnnounceSpellCastClientRPC();
    }

    [ClientRpc]
    public override void ServerAnnounceSpellCastClientRPC()
    {
        //if (IsServer) return;
        //animationScript.Trigger("SpellCastAccepted");
        if (IsOwner)
        {
            //combatManagerRef.playerMove.AddNetworkRbForceClientRPC((combatManagerRef.playerMove.Orientation.forward * ForceAdded ) + new Vector3(0, 1f, 0));
            animationScript.Trigger("AutoAttackAcknowledge");
            combatManagerRef.playerMove.SnapModelToCameraDir();
            //combatManagerRef.SetStunTimer(windupTime);
        }
        else
        {
            animationScript.Trigger("WindUp");
            animationScript.Trigger("AutoAttackAcknowledge");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (InputCollector == null || combatManagerRef == null || isOnCooldown() || !combatManagerRef.IsOwner)
            return;

        if (spellTriggeringFlag.value && combatManagerRef.IsOwner)
        {
           Use();
        }
    }
}
