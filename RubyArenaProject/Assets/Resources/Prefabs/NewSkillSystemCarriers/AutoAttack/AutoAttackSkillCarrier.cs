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

    private void OnTransformParentChanged()
    {
        Init();
    }
    public override void Init()
    {
        base.Init();
        SkillDataSO.damage = autoAttackParams.Value.Damage;
        cooldown = 1 / autoAttackParams.Value.AttackSpeed;
    }

    public override bool Use()
    {
        animationScript = combatManagerRef.animationScript;
       
        animationScript.PlayState("WindUp", windupTime);
        combatManagerRef.SetStunTimer(windupTime);

        Vector3 LookDir = getLookDirection();

        ServerSideUseServerRPC(LookDir, this.NetworkObjectId);

        return true;
    }

    [ServerRpc]
    void ServerSideUseServerRPC(Vector3 lookDir, ulong senderId, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        if (isOnCooldown()) return;
        nextAvaliableTicks.Value = System.DateTime.UtcNow.AddSeconds(cooldown).Ticks;
        Vector3 skillshotSpawnPos = combatManagerRef.SkillshotSpawnPoint.transform.position;

        var ovelappingColliders = Physics.OverlapBox(skillshotSpawnPos + (lookDir * autoAttackParams.Value.Range / 2),
            new Vector3(2, 1, autoAttackParams.Value.Range / 2), Quaternion.LookRotation(lookDir));

        Assets.Scripts.Utility.DebugUtils.DrawBox(skillshotSpawnPos + (lookDir* autoAttackParams.Value.Range / 2),
            new Vector3(2, 1, autoAttackParams.Value.Range / 2), Quaternion.LookRotation(lookDir), Color.red); 
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
            if (!playerResources || player.NetworkObject.NetworkObjectId == senderId) continue;


            SkillDataSO.ownerId = senderId;
            playerResources.damage(SkillDataSO);
        }

        ServerAnnounceSpellCastClientRPC(0);
    }

    [ClientRpc]
    void ServerAnnounceSpellCastClientRPC(ulong networkObjId)
    {
        //if (IsServer) return;
        animationScript.PlayState("Spellcast1");
        //animationScript.Trigger("SpellCastAccepted");

    }

    // Update is called once per frame
    void Update()
    {
        if (InputCollector == null || combatManagerRef == null || isOnCooldown() || !combatManagerRef.IsLocalPlayer)
            return;

        if (spellTriggeringFlag.value && combatManagerRef.IsLocalPlayer)
        {
           Use();
        }
    }
}
