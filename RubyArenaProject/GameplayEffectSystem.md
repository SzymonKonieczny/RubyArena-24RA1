# Gameplay Effect System — Implementation Reference

> **Status:** Implemented. `Assets/Scripts/Effects/`.
> **Supersedes:** the earlier draft of this document (interface-based `IGameplayEffect`,
> server-only `EffectHolder`, `CrowdControlContainer`). That design was never built; this
> one is what the code does. CC handling was not part of this work — stuns still go through
> `CombatEntity.SetStunTimer`.

---

## 1. What this system is for

One list, one lifetime, on every entity that can be buffed, harmed over time, or marked:

| You want | Implemented as |
|---|---|
| Passive stat buff ("+30% speed for 3s") | `StatBuffEffect` — contributes stats, ticks nothing |
| Damage over time | `DamageOverTimeEffect` — ticks damage, contributes nothing |
| Heal over time | `HealOverTimeEffect` — ticks a heal, contributes nothing |
| A mark other spells can detect and consume | `CombustionMarkEffect` — inert until something asks for it |

The point of collapsing these into one pipeline: a buff and a DoT have the same lifetime
(applied, timed, removed) and differ only in *what they do while active*. Keeping them
apart meant two lists, two expiry rules, two ways to write "reverse yourself on removal".

**Stat modifiers were deliberately not kept separate.** A stat modifier *is* an effect whose
tick is empty — `StatBuffEffect` above. Recalculation is not part of the tick loop (§6).

---

## 2. The three layers

```
GameplayEffectSO          (ScriptableObject: config + factory)
      │ CreateEffect()
      ▼
GameplayEffect            (runtime instance: duration, ticking, stat contribution)
      │ lives in
      ▼
StatModifierHolder        (MonoBehaviour: owns the list, ticks it, recalcs stats)
```

Plus `EffectRegistry` (wire id → SO) and `GameplayEffectContext` (what an effect is allowed
to touch). Both described below.

### Files

```
Assets/Scripts/Effects/
  GameplayEffect.cs            base class, EffectSnapshot, GameplayEffectContext
  EffectRegistry.cs            ushort id → SO lookup, snapshot reconstruction
  GameModeProvider.cs          static access to IGameMode for non-DI entities
  Definitions/
    GameplayEffectSO.cs        base definition
    StatBuffDefinitionSO.cs
    DamageOverTimeDefinitionSO.cs
    HealOverTimeDefinitionSO.cs
    CombustionMarkDefinitionSO.cs
  Effects/
    StatBuffEffect.cs
    DamageOverTimeEffect.cs
    HealOverTimeEffect.cs
    CombustionMarkEffect.cs

Assets/Scripts/Player/StatModifierHolder.cs   the holder (also used by mobs)
```

---

## 3. Writing a new effect

Three steps. The mark is used as the worked example.

### 3.1 The runtime class

```csharp
public class CombustionMarkEffect : GameplayEffect
{
    public CombustionMarkDefinitionSO Def => (CombustionMarkDefinitionSO)Definition;

    public override float Duration => Definition != null ? Definition.duration : 0f;

    protected override void OnStartAuthority(GameplayEffectContext ctx) { }
    protected override void OnTickAuthority(GameplayEffectContext ctx) { }   // inert by design
}
```

Override only what you need. The full set of hooks:

| Hook | Peers it runs on | Use for |
|---|---|---|
| `CollectStatModifier(ref acc)` | both (server writes) | stat contribution |
| `Duration` | both | how long it lasts — required |
| `OnStartAuthority` / `OnStartCosmetic` | server / client | setup, VFX, logging |
| `OnTickAuthority` / `OnTickCosmetic` | server / client | DoT damage, VFX pulses |
| `OnFinishAuthority` / `OnFinishCosmetic` | server / client | teardown |
| `OnStack(incoming, ctx)` | both | refresh-vs-stack behaviour |

**Rules that are easy to get wrong:**

- **Never touch stats in a tick.** Call `ctx.Holder.ContributionsChanged()` instead (§6).
- **DoT damage must check `ctx.CanTakeDamage(...)`** before calling `damage()`. Otherwise it
  keeps ticking into a target the gamemode has protected (§7).
- **Don't compute your own end time.** The holder sets `serverTimeEffectEnd` from `Duration`
  at apply. An effect that sets it manually will be out of sync with the client mirror.
- **Never store remaining time.** Store nothing; compare `ServerTime` against
  `serverTimeEffectEnd` when you need to know.

### 3.2 The definition

```csharp
[CreateAssetMenu(menuName = "ScriptableObjects/Effects/CombustionMark", order = 12)]
public class CombustionMarkDefinitionSO : GameplayEffectSO
{
    public int detonationDamage = 40;
    public DamageOverTimeDefinitionSO burnOnDetonate;

    public override GameplayEffect CreateEffect() => new CombustionMarkEffect();
}
```

Set `effectId` to a stable string (e.g. `"mark_combustion"`). It is the dedup key — two
applications of the same `effectId` from the same source are one effect, not two.

`registryId` is assigned automatically from list position. **Never** hand-edit it.

### 3.3 Registering it

1. Create the asset: *Assets → Create → ScriptableObjects → Effects → …*
2. Drag it into the `EffectRegistry` component's `effects` list in the scene.
3. **Append only.** The wire id is the list index; reordering or deleting an entry
   re-points every client at the wrong effect.

### 3.4 The context

`GameplayEffectContext` is what an effect is allowed to touch on its host. It is built once
in `StatModifierHolder.Awake` and is **immutable**:

| Member | Kind | Notes |
|---|---|---|
| `Holder` | stable reference | the list, for `Apply`/`Remove`/`GetEffect` |
| `Resources` | stable reference | `UnitResource` — damage, HP, mana |
| `Stats` | stable reference | `EntityStats` |
| `Combat` | stable reference | `CombatEntity` — stun, movement, animation |
| `Owner` | stable reference | the GameObject, for VFX parenting |
| `GameMode` | **property** | live `GameModeProvider.Current` |

Time and authority are **not** on the context. Effects read them directly, which is both
more explicit and impossible to hold stale:

```csharp
if (ServerTime < nextTickTime) return;    // protected on GameplayEffect
if (IsServerSide(ctx)) { ... }            // protected on GameplayEffect
```

`ServerTime` is `NetworkManager.Singleton.NetworkTimeSystem.ServerTime`, read fresh every
call. `IsServerSide(ctx)` asks the holder. Both live on the `GameplayEffect` base class so
subclasses get them for free — there is no `ctx.Now()` or `ctx.IsServerSide` to call.

The rule this enforces: **never cache the clock into a field, and never store
"time remaining."** An effect holding a context across ticks compares against
`serverTimeEffectEnd` every time.

`ctx.GameMode` does resolve live, so a gamemode that finishes loading after an effect is
applied is picked up automatically rather than leaving the effect blind to damage
restrictions.

---

## 4. Applying an effect (server side)

```csharp
GameplayEffect effect = definition.CreateEffect();
effect.Definition = definition;
effect.SourceNetworkObjectId = combatManagerRef.NetworkObjectId;   // kill credit
effect.CarrierNetworkObjectId = NetworkObjectId;                   // which skill

targetHolder.Apply(effect);
```

`Apply` does the following, in order:

1. Computes `serverTimeEffectEnd = ServerTime + Duration` (if not already set).
2. Looks for an active effect with the same `EffectId`. If found → refreshes its end time,
   calls `OnStack`, pushes the updated snapshot, recalcs. **Returns — the incoming instance
   is discarded.** Don't hold a reference to it expecting it to be in the list.
3. Otherwise: `OnStart` → add to list → push snapshot → recalc.

Get a target's holder with `GetComponent<StatModifierHolder>()`. It is on both player and
mob prefabs; `PlayerSkillHolder.statModifierHolder` and `MobSkillHolder.statModifierHolder`
both point at it.

**Never call `Apply` from a `ClientRpc`.** Stats are server-authoritative; a client-side
apply writes to a `NetworkVariable` that is ignored. The client learns about effects through
the snapshot list (§5).

---

## 5. Networking

### 5.1 The core idea: time is not networked

`NetworkTimeSystem.ServerTime` is **derived locally on each peer** — every peer estimates its
offset from the server and keeps it converged with periodic time-sync messages. It is not a
replicated value.

So a `serverTimeEffectEnd` written once at apply is already meaningful on every peer, and
both sides can expire against their own `ServerTime` with no further traffic. **Ticking
costs nothing on the wire.**

The offset is an estimate (typically tens of milliseconds), which means a client may retire
an effect slightly before or after the server. That is fine for VFX and is why
`OnFinishCosmetic` must stay purely visual — never let a client-side effect act on its own
expiry as if it were authoritative.

### 5.2 What actually goes over the wire

`NetworkList<EffectSnapshot>` on the holder:

```csharp
public struct EffectSnapshot         // ~27 bytes
{
    ushort registryId;               // which subclass, via EffectRegistry
    ulong  sourceNetworkObjectId;
    ulong  carrierNetworkObjectId;
    double serverTimeEffectEnd;
    byte   stackCount;
}
```

| Event | Cost |
|---|---|
| Applying / removing one effect | one snapshot, one entity |
| Late join | every entity's full list, one time |
| Idle | **zero** — nothing writes |

Note NGO's `NetworkList` reports the **whole list** on every mutation, so the client handler
reconciles by `(registryId, sourceNetworkObjectId)` rather than rebuilding. Without that,
applying effect B would restart effect A's particle system.

### 5.3 The client mirror

`NetworkBehaviour.IsServer` splits the two peers. The client never calls `Apply`; it builds
mirrors in `RebuildClientMirrors`, driven by `effectSnapshots.OnListChanged`:

1. Retire mirrors whose snapshot disappeared → `OnFinish`.
2. Create mirrors for new snapshots via `EffectRegistry.FromSnapshot` → `OnStart`, spawn
   cosmetic prefab.
3. Refresh existing mirrors' end time and stack count.

Mirrors are real `GameplayEffect` instances of the same subclass, so `OnTickCosmetic` works
identically to `OnTickAuthority`. This is what lets one class serve both peers.

### 5.4 Cosmetic prefabs

Set `cosmeticPrefab` on the definition. It is instantiated on the **client only**, parented
to the target (or to `cosmeticBoneName` if set), and destroyed automatically on
`OnFinish`. The server never spawns it — the host would double up otherwise.

---

## 6. Stats: recalculated on list change, never in the tick loop

```csharp
public void RecalculateStats()
{
    EntityStatisticsModifier acc = new();              // identity: 1f / 0 / 1f
    foreach (var e in active)
        e.CollectStatModifier(ref acc);                // EVERY active effect, every time

    Stats stats = entityStats.basicStats.Value;        // always from the base
    stats.speed       *= acc.speedMultiplier;
    stats.AttackSpeed *= acc.AttackSpeedMultiplier;
    stats.AttackDamage += acc.AttackDamageBoost;
    entityStats.WriteModifiedStats(stats);
}
```

Key properties:

- **Full reapply, not incremental.** The list is never reset or patched. Every contribution
  is recomputed from `basicStats`. This is what makes effects safe to expire out of order,
  and it is the only correct option for multiplicative contributions — there is no exact
  inverse for `* 1.3` on a float.
- **Called only when the list changes** — apply, remove, natural expiry. The `FixedUpdate`
  tick loop does no stat work.
- **An effect whose contribution changes** (gained a stack, changed mode) calls
  `ctx.Holder.ContributionsChanged()`, which is just `RecalculateStats()`.

`EntityStatisticsModifier` is an accumulator, not a thing you author or add. It carries
identity values so an effect with no stat contribution simply doesn't implement
`CollectStatModifier`.

---

## 7. Marks — the "detected and consumed" pattern

A mark is an effect with no tick and no stat contribution. It exists so another spell can
find it.

**Apply one** (server):

```csharp
GameplayEffect mark = combustionMarkDef.CreateEffect();
mark.Definition = combustionMarkDef;
mark.SourceNetworkObjectId = NetworkObjectId;
targetHolder.Apply(mark);
```

**Detect one** (any peer, e.g. from a hit handler):

```csharp
var mark = targetHolder.GetEffect<CombustionMarkEffect>();
if (mark != null)
    mark.Detonate(ctx, NetworkObjectId);
```

Or checking without a type: `targetHolder.Has("mark_combustion")`.

**Consume safely.** `Detonate` calls `ctx.Holder.Remove(this)` from inside a hit handler. That
is safe: `Remove` defers to the `toRemove` list drained at the top of the next `FixedUpdate`,
so the active list is never mutated mid-iteration. Same guarantee applies if a mark consumes
itself from its own tick.

Reference implementation: `CombustionMarkEffect.Detonate` — burst damage, then applies a
`DamageOverTimeEffect` burn, then removes itself. That chain (mark → burst → DoT) is the
template for most "set up then pay off" mechanics.

---

## 8. Server vs client at a glance

| | Server | Client |
|---|---|---|
| Who calls `Apply` | skills | never — builds mirrors from `effectSnapshots` |
| `IsServerSide(ctx)` | `true` | `false` |
| Tick that runs | `OnTickAuthority` | `OnTickCosmetic` |
| Stats | writes `modifiedStats` | receives it, read-only |
| Damage | real `damage()` calls | never |
| Cosmetic prefab | not spawned | spawned, destroyed on finish |
| Expiry | authoritative | local estimate, VFX only |

---

## 9. Gotchas and known limits

1. **`NetworkList<EffectSnapshot>` must be initialised** before the client reads it. The
   holder allocates it in `Start` if unassigned; assign it in the inspector if a prefab
   exposes one.
2. **Only effects with a definition work over the network.** An effect created without
   `Definition` has `RegistryId == 0` and will resolve to whatever is first in the registry.
   Always set `Definition`.
3. **`effectId` must be stable and unique per definition.** Two definitions sharing an
   `effectId` will refresh each other instead of coexisting.
4. **`stackCount` is a byte** (0–255) in the snapshot. Raise the base type if you need more.
5. **`GameModeProvider` is a static set by the active gamemode manager.** If no gamemode is
   registered, `CanTakeDamage` returns `true` (matching `PlayerResources`' existing
   behaviour with a null gamemode).
6. **`HealOverTimeEffect` reads max HP from `modifiedStats.MaxHP`**, falling back to "no
   heal" if that is zero. `PlayerResources.MaxHP` is private and not in `Stats` — if heals
   silently do nothing, check that the entity's `Stats.MaxHP` is populated.
7. **The tick loop is `FixedUpdate`** (50 Hz). Effects with `tickInterval < 0.02s` will tick
   at the physics rate, not their stated interval.
8. **Not yet migrated:** `YangBlastSkill`'s damage accumulation is still hand-wired to
   `DamageTakenEvent`. It fits this system (see the old draft's `DamageAccumulationEffect`)
   but was left alone to keep this change scoped.
9. **Stun is untouched.** `SetStunTimer` is still called directly; there is no
   `CrowdControlContainer`. A `StunEffect` would need the "short stun overwrites long stun"
   bug fixed first.

---

## 10. Checklist for adding an effect

- [ ] Runtime class in `Effects/Effects/`, overriding `Duration` at minimum
- [ ] Definition SO in `Effects/Definitions/` with a stable `effectId`
- [ ] Registered (appended) in `EffectRegistry`
- [ ] `Definition` assigned on the instance before `Apply`
- [ ] DoT calls `ctx.CanTakeDamage` before damaging
- [ ] No stat writes in any tick — `ContributionsChanged()` instead
- [ ] `OnFinishCosmetic` purely visual
- [ ] If it spawns anything, it is `cosmeticPrefab` on the definition, not hand-spawned
