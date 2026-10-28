using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Assets.Scripts.Events;
using VContainer;

/// <summary>
/// The auto attack is the one skill that is driven entirely by <see cref="EntityStats"/>:
/// damage, cadence and reach all come from modifiedStats, not from serialized per-skill
/// fields. Every other skill keeps its own authored values.
///
/// Reads happen at use time rather than being cached into fields, so a stat buff that
/// lands mid-fight is picked up on the very next swing.
/// </summary>
public class AutoAttackSkillCarrier : SkillBase
{
    [Inject]
    EventBus<GlobalStatsChangeEvent> GlobalStatsChangeEvent;

    EventBinding<GlobalStatsChangeEvent> GlobalStatsChangeEventBinding;

    private void Start()
    {
        // Injection order is not guaranteed relative to Start (see the note in
        // PlayerScript.OnNetworkPostSpawn), so only bind if the bus actually arrived.
        if (GlobalStatsChangeEvent == null) return;

        GlobalStatsChangeEventBinding = new(OnStatsChanged);
        GlobalStatsChangeEvent.Register(GlobalStatsChangeEventBinding);
    }

    /// <summary>
    /// Fires whenever any entity's stats change. Ignores everyone except our own player.
    /// </summary>
    void OnStatsChanged(GlobalStatsChangeEvent args)
    {
        if (args == null) return;
        if (skillHolderRef == null || skillHolderRef.playerCombatManager == null) return;
        if (args.recieverNetworkObjectId != skillHolderRef.playerCombatManager.NetworkObjectId) return;

        // Nothing to refresh: stats are read live from modifiedStats at the point of use.
        // The bind exists so buffs that change cadence are observable, and is kept minimal
        // deliberately rather than caching values that could go stale.
    }
    private void OnTransformParentChanged()
    {
        Init();
    }
    /// <summary>
    /// Live stats for the owning player. Every auto-attack value comes from here, so a
    /// buff applied by any effect is reflected without any local copy to keep in sync.
    /// </summary>
    Stats OwnerStats => skillHolderRef != null && skillHolderRef.Stats != null
        ? skillHolderRef.Stats.modifiedStats.Value
        : default;

    /// <summary>
    /// The owning player's character asset name, purely so misconfigured stats can be
    /// traced back to the CharacterModelSO that needs fixing.
    /// </summary>
    string OwnerCharacterName
    {
        get
        {
            PlayerScript player = skillHolderRef != null ? skillHolderRef.GetComponent<PlayerScript>() : null;
            if (player == null || CharacterList.Instance == null) return "unknown";

            int id = player.characterID.Value;
            if (id < 0 || id >= CharacterList.Instance.Characters.Count) return "unknown";

            CharacterCardSO card = CharacterList.Instance.Characters[id];
            if (card == null) return "unknown";
            if (!string.IsNullOrEmpty(card.name)) return card.name;
            return card.characterModel != null ? card.characterModel.name : "unknown";
        }
    }

    public override void Init()
    {
        base.Init();
    }

    public override bool Use()
    {
        animationScript = combatManagerRef.AnimationScript;
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

        // Read once per swing so the whole attack uses one consistent snapshot.
        Stats stats = OwnerStats;

        // AttackSpeed is a frequency (attacks per second); cooldown is its reciprocal.
        // A zero/negative AttackSpeed would make the quotient infinite, so guard it.
        if (stats.AttackSpeed <= 0f)
        {
            Debug.LogError($"AutoAttack on '{name}': AttackSpeed is {stats.AttackSpeed}, so the " +
                           "attack cannot be paced. Check AttackSpeed in the character's " +
                           "BaseStats on its CharacterModelSO.");
            return;
        }
        setCooldown(1f / stats.AttackSpeed);

        if (stats.AttackRange <= 0f)
        {
            Debug.LogError($"AutoAttack on '{name}': AttackRange is {stats.AttackRange} for " +
                           $"character '{OwnerCharacterName}'. The hitbox will be zero-sized and " +
                           "connect with nothing. Fill in AttackRange in BaseStats on the " +
                           "character's CharacterModelSO.");
        }

        Vector3 skillshotSpawnPos = skillHolderRef.playerCombatManager.SkillshotSpawnPoint.transform.position;

        // The OverlapBox is centred half a range in front of the player, and sized half a
        // range deep, so it spans from the player out to AttackRange.
        var ovelappingColliders = Physics.OverlapBox(skillshotSpawnPos + (lookDir * stats.AttackRange / 2),
            new Vector3(2, 1, stats.AttackRange / 2), Quaternion.LookRotation(lookDir), rayCastMask);

        Assets.Scripts.Utility.DebugUtils.DrawBox(skillshotSpawnPos + (lookDir * stats.AttackRange / 2),
            new Vector3(1, 1, stats.AttackRange / 2), Quaternion.LookRotation(lookDir), Color.red);

        //check for which of them are combat entities
        List<CombatEntity> combatEntities = new();
        foreach (var o in ovelappingColliders)
        {
            var combatEntity = o.transform.GetComponent<CombatEntity>();
            if (combatEntity && combatEntity.GetComponent<UnitResource>() != null && combatManagerRef != combatEntity)
                combatEntities.Add(combatEntity);
        }


        //damage entities hit by the swing
        foreach (var entity in combatEntities)
        {
            var unitResources = entity.GetComponent<UnitResource>();
            if (!unitResources || entity.NetworkObjectId == combatManagerRef.NetworkObjectId) continue;

            var data = new SkillInstanceData
            {
                damage = stats.AttackDamage,
                ownerNetworkObjectId = combatManagerRef.NetworkObjectId
            };
            unitResources.damage(data);
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
            animationScript.Trigger("AutoAttackAcknowledge");
            combatManagerRef.PlayerMove.SnapModelToCameraDir();
            //combatManagerRef.SetStunTimer(windupTime);
        }
        else
        {
            animationScript.Trigger("WindUp");
            animationScript.Trigger("AutoAttackAcknowledge");
        }
    }

    private void OnDestroy()
    {
        if (GlobalStatsChangeEventBinding == null) return;
        GlobalStatsChangeEvent?.Unregister(GlobalStatsChangeEventBinding);
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
