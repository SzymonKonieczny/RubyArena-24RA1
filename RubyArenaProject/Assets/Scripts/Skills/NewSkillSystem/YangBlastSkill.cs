using UnityEngine;
using Unity.Netcode;
using VContainer;
using Assets.Scripts.Events;

/// <summary>
/// Yang's charged blast skill. Stores damage taken, then fires a cone AOE that
/// deals stored + base damage and knocks hit targets away.
/// </summary>
public class YangBlastSkill : SkillBase
{
    [Header("Prefabs & References")]
    public GameObject blastEffect;

    NetworkVariable<float> storedDamage = new(0);
    ISkillEffect flamingGloves;

    [Inject]
    EventBus<DamageTakenEvent> damageTakenEvent;
    EventBinding<DamageTakenEvent> damageEventBinding;

    // --- Lifetime ---

    void Start()
    {
        flamingGloves = combatManagerRef.GetComponentInChildren<ISkillEffect>();
        storedDamage.OnValueChanged += UpdateVFX;

        damageEventBinding = new EventBinding<DamageTakenEvent>(
            data => OnDamageTaken(data.damageAmountPostMitigation)
        );
        damageTakenEvent.Register(damageEventBinding);
    }

    void OnDestroy()
    {
        damageTakenEvent.Unregister(damageEventBinding);
    }

    private void OnTransformParentChanged()
    {
        Init();
    }

    // --- Input ---

    void Update()
    {
        if (InputCollector == null || combatManagerRef == null || isOnCooldown() || !combatManagerRef.IsOwner)
            return;

        if (spellTriggeringFlag.value)
        {
            Use();
        }
    }

    // --- Skill Execution ---

    public override bool Use()
    {
        animationScript = combatManagerRef.animationScript;
        animationScript.Trigger("WindUp");
        combatManagerRef.SetStunTimer(windupTime);

        Vector3 lookDir = getLookDirection();
        combatManagerRef.playerMove.AddNetworkRbVelocityClientRPC(-lookDir * 5);
        ServerSideUseServerRPC(lookDir);

        return true;
    }

    [ServerRpc]
    public override void ServerSideUseServerRPC(Vector3 lookDir, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        if (isOnCooldown()) return;
        setCooldown(cooldown);

        SpawnBlastEffect(lookDir);

        ulong senderId = combatManagerRef.NetworkObjectId;
        Vector3 skillOrigin = combatManagerRef.SkillshotSpawnPoint.position;

        foreach (PlayerCombatManager target in GetPlayersInCone(skillOrigin, lookDir))
        {
            if (target.NetworkObject.NetworkObjectId == senderId) continue;

            PlayerResources resources = target.GetComponent<PlayerResources>();
            if (resources == null) continue;

            // Knockback away from Yang
            Vector3 knockbackDir = (target.transform.position - skillOrigin).normalized;
            target.playerMove.AddNetworkRbVelocityClientRPC(knockbackDir * 10);

            resources.damage(new SkillInstanceData
            {
                damage = this.damage + (int)storedDamage.Value,
                ownerNetworkObjectId = senderId
            });
        }

        storedDamage.Value = 0;
        ServerAnnounceSpellCastClientRPC();
    }

    [ClientRpc]
    public override void ServerAnnounceSpellCastClientRPC()
    {
        if (IsOwner)
        {
            animationScript.Trigger("SpellAcknowledge2");
        }
        else
        {
            animationScript.Trigger("WindUp");
            animationScript.Trigger("SpellAcknowledge2");
        }
    }

    // --- Damage Accumulation ---

    void OnDamageTaken(float damageAmount)
    {
        storedDamage.Value += damageAmount;
    }

    void UpdateVFX(float oldStoredDamage, float newStoredDamage)
    {
        int effectId = newStoredDamage > 30 ? 0 : 1;
        flamingGloves.PlayEffect(effectId);
    }

    // --- Helpers ---

    /// <summary>
    /// Spawns the blast visual at the skill origin, facing the look direction.
    /// </summary>
    void SpawnBlastEffect(Vector3 lookDir)
    {
        Transform spawnPoint = combatManagerRef.SkillshotSpawnPoint;
        Vector3 position = spawnPoint.position + lookDir * 2;
        Quaternion rotation = Quaternion.LookRotation(lookDir, Vector3.up);

        GameObject effect = Instantiate(blastEffect);
        effect.GetComponent<NetworkObject>().Spawn();
        effect.transform.SetPositionAndRotation(position, rotation);
    }

    /// <summary>
    /// Returns all players within a 5-unit 40° cone in front of the skill origin.
    /// </summary>
    PlayerCombatManager[] GetPlayersInCone(Vector3 origin, Vector3 direction)
    {
        Collider[] overlaps = Physics.OverlapSphere(origin, 5);
        int coneCount = 0;

        // Two-pass: count then fill — avoids allocating a list per cast.
        for (int i = 0; i < overlaps.Length; i++)
        {
            if (IsInCone(overlaps[i], origin, direction))
                coneCount++;
        }

        PlayerCombatManager[] result = new PlayerCombatManager[coneCount];
        int index = 0;

        for (int i = 0; i < overlaps.Length; i++)
        {
            if (IsInCone(overlaps[i], origin, direction))
                result[index++] = overlaps[i].GetComponent<PlayerCombatManager>();
        }

        return result;
    }

    bool IsInCone(Collider collider, Vector3 origin, Vector3 direction)
    {
        if (!collider.CompareTag("Player"))
            return false;

        PlayerCombatManager combatManager = collider.GetComponent<PlayerCombatManager>();
        if (combatManager == null || combatManager == combatManagerRef)
            return false;

        Vector3 toTarget = (combatManager.transform.position - origin);
        float angle = Mathf.Acos(Vector3.Dot(direction.normalized, toTarget.normalized));

        return Mathf.Abs(angle) < Mathf.Deg2Rad * 40;
    }
}
