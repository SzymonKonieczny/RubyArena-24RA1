using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Assets.Scripts.Events;
using VContainer;

/// <summary>
/// Runtime core for mobs. Analogous to PlayerScript — spawns the model,
/// initializes all sub-components, and manages the mob's lifecycle.
/// </summary>
public class MobCore : NetworkBehaviour
{
    [SerializeField] Transform modelAnchor;
    [SerializeField] MobConfigSO config;

    MobCombatEntity mobCombatEntity;
    MobUnitResource mobUnitResource;
    MobAnimationScript mobAnimation;
    MobMovement mobMovement;
    MobAI mobAI;
    EntityStats entityStats;
    MobSkillHolder mobSkillHolder;
    StatModifierHolder statModifierHolder;

    [Inject]
    EventBus<DamageTakenEvent> damageEventBus;
    EventBinding<DamageTakenEvent> damageEventBinding;

    public MobConfigSO Config => config;

    /// <summary>
    /// Called by the spawner before NetworkObject.Spawn() to set the mob's configuration.
    /// </summary>
    public void InitializeMob(MobConfigSO cfg)
    {
        config = cfg;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (config == null)
        {
            Debug.LogError($"MobCore {name} spawned without a MobConfigSO!");
            return;
        }

        InitializeComponents();
        SpawnModel();
        SetupStats();
        SetupDamageListener();

        if (IsServer)
        {
            mobAI?.Initialize(config);
            mobUnitResource?.SetMaxHP(config.baseStats.MaxHP > 0 ? config.baseStats.MaxHP : 100f);
        }
    }

    void InitializeComponents()
    {
        mobCombatEntity = GetComponent<MobCombatEntity>();
        mobUnitResource = GetComponent<MobUnitResource>();
        mobAnimation = GetComponent<MobAnimationScript>();
        mobMovement = GetComponent<MobMovement>();
        mobAI = GetComponent<MobAI>();
        entityStats = GetComponent<EntityStats>();
        mobSkillHolder = GetComponent<MobSkillHolder>();
        statModifierHolder = GetComponent<StatModifierHolder>();

        mobCombatEntity?.Initialize();
        mobSkillHolder?.Initialize();
    }

    void SpawnModel()
    {
        if (modelAnchor == null)
            modelAnchor = transform;

        if (config.modelPrefab != null)
        {
            GameObject modelGO = Instantiate(config.modelPrefab, modelAnchor);
            modelGO.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            // Find animator on the model
            Animator animator = modelGO.GetComponentInChildren<Animator>(false);
            if (animator != null && mobAnimation != null)
            {
                mobAnimation.SetAnimator(animator);
            }

        }
        else
        {
            Debug.LogWarning($"Mob {name} has no modelPrefab assigned in config.");
        }
   }

    void SetupStats()
    {
        if (entityStats == null) return;

        if (IsServer)
        {
            entityStats.basicStats.Value = config.baseStats;
        }
    }

    void SetupDamageListener()
    {
        if (damageEventBus == null) return;

        damageEventBinding = new EventBinding<DamageTakenEvent>(OnDamageTaken);
        damageEventBus.Register(damageEventBinding);
    }

    void OnDamageTaken(DamageTakenEvent e)
    {
        if (e.recieverNetworkObjectId != NetworkObjectId) return;
        if (mobAI == null) return;

        // Notify AI that we were damaged (for Passive/Neutral target acquisition)
        mobAI.OnDamagedBy(e.damagerNetworkObjectId);
    }

    public override void OnNetworkDespawn()
    {
        if (damageEventBus != null && damageEventBinding != null)
            damageEventBus.Unregister(damageEventBinding);

        base.OnNetworkDespawn();
    }
}
