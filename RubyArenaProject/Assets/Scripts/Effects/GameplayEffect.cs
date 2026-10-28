using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Base class for everything the StatModifierHolder can carry: passive stat buffs,
/// damage/heal over time, marks that other spells can detect and consume.
///
/// One class, both peers. The SAME instance ticks on the server (does real damage,
/// writes stats) and on the client (drives VFX). Subclasses fork only inside the
/// authority hooks, never by existing twice.
///
/// See RubyArenaProject/GameplayEffectSystem.md for the full design.
/// </summary>
public abstract class GameplayEffect
{
    // --- Identity / stacking ---

    /// <summary>The ScriptableObject this effect was built from. Lets the client
    /// reconstruct the right subclass from a network snapshot.</summary>
    public GameplayEffectSO Definition;

    /// <summary>Short wire id — the index of Definition in EffectRegistry.</summary>
    public ushort RegistryId => Definition != null ? Definition.registryId : (ushort)0;

    /// <summary>Deduplication / stacking key, e.g. "dot_burn". Two effects with the
    /// same key from the same source are one effect, not two.</summary>
    public virtual string EffectId => Definition != null ? Definition.effectId : GetType().Name;

    public ulong SourceNetworkObjectId;   // who caused it — kill credit, attribution
    public ulong CarrierNetworkObjectId;  // which skill carrier spawned it

    /// <summary>Client-only VFX spawned from Definition.cosmeticPrefab. Destroyed on
    /// finish; server instances leave it null.</summary>
    public GameObject CosmeticInstance;

    // --- Lifetime ---
    // Absolute server timestamp, identical on every peer (NetworkTime.ServerTime is
    // derived locally, not replicated — see the doc). Written once when applied,
    // then just compared against ServerTime in the holder's FixedUpdate.

    public double serverTimeEffectEnd;
    public int stackCount = 1;

    /// <summary>How long the effect lasts, read from its definition. Used by the holder
    /// to compute <see cref="serverTimeEffectEnd"/> at apply time.</summary>
    public abstract float Duration { get; }

    // --- Authority hooks: the only place a subclass forks server vs client ---

    /// <summary>Called on both peers immediately before the effect is added to the list.</summary>
    public void OnStart(GameplayEffectContext ctx)
    {
        if (IsServerSide(ctx)) OnStartAuthority(ctx);
        else OnStartCosmetic(ctx);
    }

    /// <summary>Called on both peers every FixedUpdate until expiry. Do NOT touch stats
    /// here — stats are recalculated when the effect list changes. If a tick needs to
    /// change a stat contribution, call ContributionsChanged() afterwards.</summary>
    public void OnFixedUpdate(GameplayEffectContext ctx)
        => OnFixedUpdateCore(ctx);

    /// <summary>Called on both peers when the effect leaves the list (expiry or consume).</summary>
    public void OnFinish(GameplayEffectContext ctx)
    {
        if (CosmeticInstance != null)
        {
            Object.Destroy(CosmeticInstance);
            CosmeticInstance = null;
        }

        if (IsServerSide(ctx)) OnFinishAuthority(ctx);
        else OnFinishCosmetic(ctx);
    }

    /// <summary>
    /// Called when an effect with the same <see cref="EffectId"/> is applied again while
    /// this one is still active. The holder has already refreshed this effect's duration;
    /// add stack behaviour here (ramp damage, add a charge, ignore).
    ///
    /// Default: stack up to Definition.maxStacks, then just refresh.
    /// </summary>
    public virtual void OnStack(GameplayEffect incoming, GameplayEffectContext ctx)
    {
        int max = Definition != null ? Definition.maxStacks : 1;
        if (stackCount >= max) return;
        stackCount++;
    }

    /// <summary>
    /// Whether this peer is the authoritative one. Asked of the holder live, so it is
    /// correct even before OnNetworkSpawn has settled.
    /// </summary>
    protected static bool IsServerSide(GameplayEffectContext ctx)
        => ctx.Holder != null && ctx.Holder.IsServerSide;

    /// <summary>
    /// Current server time, straight from the network clock. Read it where you need it —
    /// never cache it in a field, and never store "time remaining": compare this against
    /// <see cref="serverTimeEffectEnd"/>.
    /// </summary>
    protected static double ServerTime
        => NetworkManager.Singleton != null
            ? NetworkManager.Singleton.NetworkTimeSystem.ServerTime
            : 0;

    protected virtual void OnStartAuthority(GameplayEffectContext ctx) { }
    protected virtual void OnStartCosmetic(GameplayEffectContext ctx) { }
    protected virtual void OnFinishAuthority(GameplayEffectContext ctx) { }
    protected virtual void OnFinishCosmetic(GameplayEffectContext ctx) { }

    /// <summary>
    /// The tick. Most effects only care about one side — override the side you need.
    /// Effects that want the same logic on both peers (rare: purely local bookkeeping)
    /// override this instead.
    /// </summary>
    protected virtual void OnFixedUpdateCore(GameplayEffectContext ctx)
    {
        if (IsServerSide(ctx)) OnTickAuthority(ctx);
        else OnTickCosmetic(ctx);
    }

    protected virtual void OnTickAuthority(GameplayEffectContext ctx) { }
    protected virtual void OnTickCosmetic(GameplayEffectContext ctx) { }

    // --- Stat contribution ---

    /// <summary>
    /// Add this effect's contribution into the accumulator. Called during a FULL
    /// recalculation of the entity's stats — identity values ("no contribution") are
    /// the default, so a pure DoT just leaves this alone.
    ///
    /// Never apply directly to EntityStats here; only fold into <paramref name="acc"/>.
    /// </summary>
    public virtual void CollectStatModifier(ref EntityStatisticsModifier acc) { }

    // --- Marshalling ---

    /// <summary>Copy the networked fields onto a freshly-created instance.
    /// Used by both the server (writing the snapshot) and the client (reading it).</summary>
    public void CopyFromSnapshot(EffectSnapshot snapshot)
    {
        SourceNetworkObjectId = snapshot.sourceNetworkObjectId;
        CarrierNetworkObjectId = snapshot.carrierNetworkObjectId;
        serverTimeEffectEnd = snapshot.serverTimeEffectEnd;
        stackCount = snapshot.stackCount;
    }

    public EffectSnapshot ToSnapshot() => new EffectSnapshot
    {
        registryId = RegistryId,
        sourceNetworkObjectId = SourceNetworkObjectId,
        carrierNetworkObjectId = CarrierNetworkObjectId,
        serverTimeEffectEnd = serverTimeEffectEnd,
        stackCount = (byte)Mathf.Clamp(stackCount, 0, 255),
    };
}

/// <summary>
/// What actually goes over the wire, once per apply/remove. Deliberately small:
/// ticking is NOT networked (both peers run it against their own ServerTime), so the
/// list only changes on add/remove.
/// </summary>
public struct EffectSnapshot : INetworkSerializable, System.IEquatable<EffectSnapshot>
{
    public ushort registryId;
    public ulong sourceNetworkObjectId;
    public ulong carrierNetworkObjectId;
    public double serverTimeEffectEnd;
    public byte stackCount;

    // Field order here is the wire format. Appending is safe; reordering or removing a
    // field breaks clients running an older build, same rule as EffectRegistry ids.
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsReader)
        {
            var reader = serializer.GetFastBufferReader();
            reader.ReadValueSafe(out registryId);
            reader.ReadValueSafe(out sourceNetworkObjectId);
            reader.ReadValueSafe(out carrierNetworkObjectId);
            reader.ReadValueSafe(out serverTimeEffectEnd);
            reader.ReadValueSafe(out stackCount);
        }
        else
        {
            var writer = serializer.GetFastBufferWriter();
            writer.WriteValueSafe(registryId);
            writer.WriteValueSafe(sourceNetworkObjectId);
            writer.WriteValueSafe(carrierNetworkObjectId);
            writer.WriteValueSafe(serverTimeEffectEnd);
            writer.WriteValueSafe(stackCount);
        }
    }

    public bool Equals(EffectSnapshot o)
        => registryId == o.registryId
        && sourceNetworkObjectId == o.sourceNetworkObjectId
        && carrierNetworkObjectId == o.carrierNetworkObjectId
        && serverTimeEffectEnd == o.serverTimeEffectEnd
        && stackCount == o.stackCount;

    public override bool Equals(object obj) => obj is EffectSnapshot o && Equals(o);

    public override int GetHashCode()
        => System.HashCode.Combine(registryId, sourceNetworkObjectId, stackCount);
}

/// <summary>
/// Everything an effect is allowed to reach on the entity carrying it. Built once by
/// StatModifierHolder from components that BOTH players and mobs already have, so no
/// effect ever needs to know whether it is on a PlayerSkillHolder or a MobSkillHolder.
///
/// Immutable by design: the fields below are stable for the entity's lifetime. Time and
/// authority are NOT here — effects read them through the GameplayEffect base class
/// (ServerTime, IsServerSide(ctx)) so they can never be held stale.
/// </summary>
public class GameplayEffectContext
{
    public StatModifierHolder Holder;
    public UnitResource Resources;   // damage(), getHP(), takeMana() — PlayerResources / MobUnitResource
    public EntityStats Stats;
    public CombatEntity Combat;      // SetStunTimer(), movement, animation — works for both
    public GameObject Owner;         // the entity's GameObject, for VFX parenting

    /// <summary>
    /// Live lookup rather than a stored reference, so a gamemode that finishes loading
    /// after the effect was applied is still picked up.
    /// </summary>
    public IGameMode GameMode => GameModeProvider.Current;

    /// <summary>
    /// Whether damage can currently land on this entity. Damage over time MUST check
    /// this, otherwise a DoT bypasses the spawn protection that direct damage respects.
    /// </summary>
    public bool CanTakeDamage(ulong targetNetworkObjectId)
        => GameMode == null || GameMode.CanDamage(targetNetworkObjectId);
}
