using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Networking.Transport;

/// <summary>
/// Accumulator for stat contributions. Effects fold into this via
/// GameplayEffect.CollectStatModifier; it is never applied directly.
/// Identity values (1 / 0 / 1) mean "no contribution", so a pure DoT leaves it alone.
/// </summary>
public class EntityStatisticsModifier //add more as needed
{
    public float speedMultiplier = 1;
    public int AttackDamageBoost = 0;
    public float AttackSpeedMultiplier = 1;
}

/// <summary>
/// The single home for gameplay effects on an entity — players and mobs alike.
///
/// Two jobs, on purpose:
///   * FixedUpdate ticks the effects (DoT damage, heal pulses, mark expiry).
///   * The effect list CHANGING recalculates stats — never the tick loop.
///
/// Stats are always a full re-derive from basicStats, never an incremental patch, so
/// effects can expire out of order or change contribution at runtime safely.
///
/// Runs on BOTH peers: the server drives real effects, the client drives VFX from the
/// same instances, marshalled through effectSnapshots.
///
/// See RubyArenaProject/GameplayEffectSystem.md.
/// </summary>
[RequireComponent(typeof(EntityStats))]
public class StatModifierHolder : NetworkBehaviour
{
    public EntityStats entityStats;

    [Tooltip("Wire forms of the active effects. Read-only at runtime; the holder writes it " +
             "on apply/remove and the client mirrors it. Must be assigned (or will be " +
             "grabbed in Awake).")]
    [SerializeField] NetworkList<EffectSnapshot> effectSnapshots = new();

    /// <summary>Live effects, in application order. Safe to query for marks (OfType&lt;&gt;()),
    /// but NOT safe to add/remove from directly — use Apply/Remove.</summary>
    public IReadOnlyList<GameplayEffect> ActiveEffects => active;

    readonly List<GameplayEffect> active = new();
    readonly List<GameplayEffect> toRemove = new();
    readonly Dictionary<(ushort, ulong), GameplayEffect> clientMirrors = new();

    // Immutable once built in Awake. Effects may hold this across ticks — the volatile
    // values (time, authority, gamemode) are properties, not fields, so they can't go stale.
    GameplayEffectContext ctx;

    NetworkBehaviour ownerBehaviour;

    /// <summary>Authoritative side. Resolved live from the entity's NetworkBehaviour.</summary>
    public bool IsServerSide => ownerBehaviour != null && ownerBehaviour.IsServer;

    /// <summary>Current server time, straight from the network clock — never cached.</summary>
    public double Now => NetworkManager.Singleton != null
        ? NetworkManager.Singleton.NetworkTimeSystem.ServerTime
        : 0;

    void Awake()
    {
        if (entityStats == null) entityStats = GetComponent<EntityStats>();

        ownerBehaviour = GetComponent<NetworkBehaviour>();
        if (ownerBehaviour == null)
        {
            Debug.LogError($"StatModifierHolder on '{name}' has no NetworkBehaviour on the " +
                           "same GameObject. Effects cannot determine server authority.");
        }

        ctx = new GameplayEffectContext
        {
            Holder = this,
            Resources = GetComponent<UnitResource>(),
            Stats = entityStats,
            Combat = GetComponent<CombatEntity>(),
            Owner = gameObject,
        };
    }

    void Start()
    {
        if (effectSnapshots == null)
            effectSnapshots = new NetworkList<EffectSnapshot>();

        effectSnapshots.OnListChanged += OnSnapshotsChanged;

        // The snapshot list is populated by the server before OnNetworkSpawn on a
        // late-joining client, so build any mirrors we arrived with.
        RebuildClientMirrors();
        RecalculateStats();
    }

    void OnDestroy()
    {
        if (effectSnapshots != null)
            effectSnapshots.OnListChanged -= OnSnapshotsChanged;
    }

    // ------------------------------------------------------------------
    // Apply / remove
    // ------------------------------------------------------------------

    /// <summary>
    /// Add an effect. Call this on the server; clients receive it through the snapshot
    /// list. If an effect with the same <see cref="GameplayEffect.EffectId"/> is already
    /// active it is refreshed or stacked per the definition, not duplicated.
    /// </summary>
    public void Apply(GameplayEffect effect)
    {
        // The holder owns the clock — effects describe their duration, never their end time.
        if (effect.serverTimeEffectEnd <= 0)
            effect.serverTimeEffectEnd = Now + effect.Duration;

        // --- stack / refresh ---
        GameplayEffect existing = FindActive(effect.EffectId);
        if (existing != null)
        {
            // Refresh duration. Stacking is opt-in per effect via OnStack.
            existing.serverTimeEffectEnd = effect.serverTimeEffectEnd;
            existing.OnStack(effect, ctx);
            if (IsServerSide)
                PushSnapshot(existing);
            RecalculateStats();
            return;
        }

        effect.OnStart(ctx);
        active.Add(effect);

        if (IsServerSide)
            PushSnapshot(effect);
        else
            SpawnCosmetic(effect);

        RecalculateStats();
    }

    /// <summary>Client-side only: parent the definition's cosmetic prefab to the target.
    /// Server peers don't need it (they have no patient view) and spawning it there would
    /// double up on the host.</summary>
    void SpawnCosmetic(GameplayEffect effect)
    {
        GameObject prefab = effect.Definition != null ? effect.Definition.cosmeticPrefab : null;
        if (prefab == null) return;

        Transform parent = transform;
        string bone = effect.Definition.cosmeticBoneName;
        if (!string.IsNullOrEmpty(bone))
        {
            Transform found = transform.Find(bone);
            if (found != null) parent = found;
        }

        GameObject vfx = Instantiate(prefab, parent);
        vfx.transform.localPosition = Vector3.zero;
        effect.CosmeticInstance = vfx;
    }

    GameplayEffect FindActive(string effectId)
    {
        for (int i = 0; i < active.Count; i++)
            if (active[i].EffectId == effectId) return active[i];
        return null;
    }

    /// <summary>
    /// Remove an effect immediately (as opposed to waiting for expiry). Used when a
    /// second spell consumes a mark. Safe to call from inside a tick or a hit handler —
    /// the actual removal is deferred to the next FixedUpdate, see <see cref="toRemove"/>.
    /// </summary>
    public void Remove(GameplayEffect effect)
    {
        if (effect == null || !active.Contains(effect)) return;

        if (IsServerSide)
            RemoveSnapshot(effect);

        if (!toRemove.Contains(effect))
            toRemove.Add(effect);
    }

    /// <summary>True if an effect with this id is on the entity. The primitive marks are
    /// built from.</summary>
    public bool Has(string effectId) => FindActive(effectId) != null;

    /// <summary>First active effect of a given type — the usual way a spell checks for a
    /// mark. Returns null if absent.</summary>
    public T GetEffect<T>() where T : GameplayEffect
    {
        for (int i = 0; i < active.Count; i++)
            if (active[i] is T typed) return typed;
        return null;
    }

    /// <summary>
    /// Tell the holder an active effect changed what it contributes to the stats
    /// (e.g. a mark gained a stack). Triggers a full recalculation.
    /// </summary>
    public void ContributionsChanged() => RecalculateStats();

    // ------------------------------------------------------------------
    // Tick loop
    // ------------------------------------------------------------------

    void FixedUpdate()
    {

        // Drain removals requested since the last step (marks consumed, e.g.).
        bool listChanged = false;
        if (toRemove.Count > 0)
        {
            for (int i = 0; i < toRemove.Count; i++)
            {
                GameplayEffect removed = toRemove[i];
                removed.OnFinish(ctx);
                active.Remove(removed);
            }
            toRemove.Clear();
            listChanged = true;
        }

        // Tick + natural expiry. NO stat work here — that only happens when the list
        // changes, which is exactly what a recalc is for.
        for (int i = active.Count - 1; i >= 0; i--)
        {
            GameplayEffect effect = active[i];

            if (Now >= effect.serverTimeEffectEnd)
            {
                effect.OnFinish(ctx);
                active.RemoveAt(i);
                listChanged = true;
                continue;
            }

            effect.OnFixedUpdate(ctx);
        }

        if (listChanged)
            RecalculateStats();
    }

    // ------------------------------------------------------------------
    // Stats
    // ------------------------------------------------------------------

    /// <summary>
    /// Full re-derive: start from basic stats, fold every active effect's contribution,
    /// write the result. Never resets or patches the list — recomputing from scratch is
    /// what makes multiplicative contributions safe (there is no exact inverse for *1.3).
    /// </summary>
    public void RecalculateStats()
    {
        if (entityStats == null) return;
        if (!IsServerSide) return;

        EntityStatisticsModifier acc = new();
        for (int i = 0; i < active.Count; i++)
            active[i].CollectStatModifier(ref acc);

        Stats stats = entityStats.basicStats.Value;
        stats.speed *= acc.speedMultiplier;
        stats.AttackSpeed *= acc.AttackSpeedMultiplier;
        stats.AttackDamage += acc.AttackDamageBoost;

        entityStats.WriteModifiedStats(stats);
    }

    // ------------------------------------------------------------------
    // Network mirror (client-side VFX)
    // ------------------------------------------------------------------

    void PushSnapshot(GameplayEffect effect)
    {
        EffectSnapshot snapshot = effect.ToSnapshot();

        for (int i = 0; i < effectSnapshots.Count; i++)
        {
            if (!SameEffect(effectSnapshots[i], snapshot)) continue;
            effectSnapshots[i] = snapshot;   // refresh / restack
            return;
        }
        effectSnapshots.Add(snapshot);
    }

    void RemoveSnapshot(GameplayEffect effect)
    {
        for (int i = 0; i < effectSnapshots.Count; i++)
        {
            if (!SameEffect(effectSnapshots[i], effect)) continue;
            effectSnapshots.RemoveAt(i);
            return;
        }
    }

    static bool SameEffect(EffectSnapshot a, EffectSnapshot b)
        => a.registryId == b.registryId
        && a.sourceNetworkObjectId == b.sourceNetworkObjectId;

    static bool SameEffect(EffectSnapshot a, GameplayEffect b)
        => a.registryId == b.RegistryId
        && a.sourceNetworkObjectId == b.SourceNetworkObjectId;

    /// <summary>
    /// NGO's NetworkList reports the WHOLE list on every mutation, so reconcile by
    /// identity rather than rebuilding — otherwise applying effect B restarts effect A's
    /// particles. Mirrors only exist on non-authoritative peers: the server already owns
    /// the real instances and building copies there would duplicate VFX.
    /// </summary>
    void OnSnapshotsChanged(NetworkListEvent<EffectSnapshot> changeEvent)
    {
        if (IsServerSide) return;
        RebuildClientMirrors();
    }

    void RebuildClientMirrors()
    {
        if (IsServerSide) return;
        if (effectSnapshots == null) return;


        // 1) Retire mirrors whose snapshot is gone.
        List<(ushort, ulong)> stale = null;
        foreach (var pair in clientMirrors)
        {
            bool stillListed = false;
            for (int i = 0; i < effectSnapshots.Count; i++)
            {
                if (effectSnapshots[i].registryId != pair.Key.Item1) continue;
                if (effectSnapshots[i].sourceNetworkObjectId != pair.Key.Item2) continue;
                stillListed = true;
                break;
            }
            if (stillListed) continue;

            pair.Value.OnFinish(ctx);
            active.Remove(pair.Value);
            (stale ??= new List<(ushort, ulong)>()).Add(pair.Key);
        }
        if (stale != null)
            foreach (var key in stale) clientMirrors.Remove(key);

        // 2) Create mirrors for newly-seen snapshots, refresh existing ones.
        EffectRegistry registry = EffectRegistry.Instance;
        if (registry == null)
        {
            Debug.LogError("StatModifierHolder: no EffectRegistry in the scene. Client-side " +
                           "effect VFX cannot be reconstructed.");
            return;
        }

        for (int i = 0; i < effectSnapshots.Count; i++)
        {
            EffectSnapshot snapshot = effectSnapshots[i];
            var key = (snapshot.registryId, snapshot.sourceNetworkObjectId);

            if (clientMirrors.TryGetValue(key, out GameplayEffect existing))
            {
                existing.serverTimeEffectEnd = snapshot.serverTimeEffectEnd;
                existing.stackCount = snapshot.stackCount;
                continue;
            }

            GameplayEffect mirror = registry.FromSnapshot(snapshot);
            if (mirror == null) continue;

            mirror.OnStart(ctx);
            clientMirrors[key] = mirror;
            active.Add(mirror);
            SpawnCosmetic(mirror);   // client-only VFX for this effect
        }
    }
}
