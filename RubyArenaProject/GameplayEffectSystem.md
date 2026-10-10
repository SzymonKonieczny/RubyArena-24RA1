# Gameplay Effect System — Design Document

## Goal

Replace the current ad-hoc approach to buffs, debuffs, crowd control, damage-over-time, and reactive passives with a **unified `IGameplayEffect` pipeline** that is:

- **Composable** — effects are reusable building blocks, not hardcoded into individual skills
- **Maintainable** — adding a new CC type or buff doesn't require changes across 5 files
- **Network-safe** — server-authoritative, clients are stateless queryers
- **Incremental** — can be adopted one behaviour at a time without rewriting existing skills

---

## Current Problems

| Problem | Where |
|---|---|
| `YangBlastSkill` manually subscribes to `DamageTakenEvent` to accumulate damage | Every new "stores damage" skill rewrites this |
| `RubyFormSkill` hardcodes stat changes + dissolve + particles in one coroutine | Not reusable — no other skill can grant a speed buff |
| Stun is a flat `StunTime += value` in `InputCollectorScript` | A short stun overwrites a longer stun (the bug) |
| `EntityStatisticsModifier` has no `OnStart`/`OnFinish` lifecycle | Can't trigger VFX, SFX, or animation when a buff begins/ends |
| No DoT/HoT mechanism | Each would have to be a custom coroutine in a skill |
| Two parallel modifier systems (`StatModifierHolder` + manual per-skill code) | Inconsistent behaviour, duplicated logic |

---

## Architecture

### Layer 0 — IGameplayEffect Interface

```csharp
public interface IGameplayEffect
{
    void OnStart(EffectHolder target);
    void OnFinish(EffectHolder target);
    void OnTick(EffectHolder target);
    bool IsExpired { get; }
    string EffectId { get; }
}
```

Each effect implements exactly four lifecycle methods. `EffectId` is a unique string key used for deduplication (e.g., "stun", "slow_50", "ruby_form_buff").

### Layer 1 — EffectHolder (runs on server, ticks via FixedUpdate)

```csharp
public class EffectHolder : NetworkBehaviour
{
    [SerializeField] private float tickInterval = 0.5f;

    public PlayerCombatManager CombatManager { get; private set; }
    public CrowdControlContainer CCContainer { get; private set; }

    private List<IGameplayEffect> _activeEffects = new();
    private List<IGameplayEffect> _pendingAdd = new();

    private void Awake()
    {
        CombatManager = GetComponent<PlayerCombatManager>();
        CCContainer = GetComponent<CrowdControlContainer>();
    }

    public void ApplyEffect(IGameplayEffect effect)
    {
        if (!IsServer) return;
        _pendingAdd.Add(effect);
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        // — add pending —
        foreach (var effect in _pendingAdd)
        {
            effect.OnStart(this);
            _activeEffects.Add(effect);
        }
        _pendingAdd.Clear();

        // — tick & expiry (deferred removal) —
        double now = NetworkManager.ServerTime.Time;
        List<IGameplayEffect> expired = new();

        foreach (var effect in _activeEffects)
        {
            effect.OnTick(this);
            if (effect.IsExpired)
                expired.Add(effect);
        }

        foreach (var effect in expired)
        {
            effect.OnFinish(this);
            _activeEffects.Remove(effect);
        }
    }
}
```

Key decisions:
- **Deferred removal**: expired effects are collected in a separate list, then removed after iteration — avoids `CollectionModified`.
- **Pending queue**: effects added mid-tick (`_pendingAdd`) start on the *next* FixedUpdate, not during iteration. Simple and safe.
- **Server-only**: the EffectHolder runs lifecycle on the server. Clients receive results through events.

### Layer 2 — CrowdControlContainer (Central CC Authority)

```csharp
// Each CC type is tracked by its expiry time.
// When applying a new CC of the same type, we only overwrite
// if the new duration ends LATER than the current one.
public class CrowdControlContainer : NetworkBehaviour
{
    private Dictionary<string, double> _ccExpiries = new(); // ccId → serverTime expiry

    // Injected via [Inject] like all existing EventBus fields
    [Inject]
    EventBus<StunEvent> stunEventBus;

    public void ApplyCC(string ccId, double serverTimeExpiry)
    {
        if (!IsServer) return;

        // Max-duration semantics: only overwrite if the new one lasts longer
        if (_ccExpiries.TryGetValue(ccId, out double existing))
        {
            if (existing >= serverTimeExpiry) return; // existing one outlasts the new one
        }

        _ccExpiries[ccId] = serverTimeExpiry;

        // Publish strongly-typed event so listeners react
        BroadcastCcChange(ccId, isApplying: true);
    }

    public bool IsCCActive(string ccId)
    {
        return _ccExpiries.TryGetValue(ccId, out double expiry)
            && NetworkManager.ServerTime.Time < expiry;
    }

    public void FixedUpdate()
    {
        if (!IsServer) return;

        double now = NetworkManager.ServerTime.Time;

        // Deferred removal via Keys.ToArray() — avoids CollectionModified
        foreach (string ccId in _ccExpiries.Keys.ToArray())
        {
            if (now >= _ccExpiries[ccId])
            {
                BroadcastCcChange(ccId, isApplying: false);
                _ccExpiries.Remove(ccId);
            }
        }
    }

    private void BroadcastCcChange(string ccId, bool isApplying)
    {
        // Publish the event specific to this CC type
        // (StunEvent shown here — other CCs use their own event types)
        if (ccId == "stun")
        {
            stunEventBus.Raise(new StunEvent
            {
                isStunned = isApplying,
                serverTimeExpiry = isApplying ? _ccExpiries[ccId] : 0,
            });
        }
    }
}
```

The `Dictionary<string, double>` approach has a clean property: applying the *same* CC twice naturally keeps the longer duration. No special-case logic needed beyond the single `if (existing >= serverTimeExpiry) return;` guard.

#### Behaviour of overlapping stuns

1. Stun A (3s) applied → `_ccExpiries["stun"] = now + 3` → `StunEvent` fires → all listeners react
2. 1s later, Stun B (1s) applied → `newExpiry = now + 1`, existing = `(previous now + 3) - 1s elapsed ≈ now + 2`. Since existing (now+2) > newExpiry (now+1), **nothing changes**. Listeners don't even get an event.
3. 2.5s later (0.5s before A would expire), Stun C (2s) applied → `newExpiry = now + 2`, existing ≈ `now + 0.5`. Since newExpiry > existing, **duration extends** to 2s. Event fires (but `isStunned` was already `true`, so listeners just see the timer extend).

Result: the longest stun always wins, listeners never do duration math.

### Layer 3 — Strongly-Typed Effect Events

NOT a single generic `GameplayEffectEvent`. Instead, each effect gets its own event struct and EventBus, preserving the type safety the existing codebase was built on.

```csharp
// Each effect has a dedicated struct with exactly the fields it needs
public struct StunEvent
{
    public bool isStunned;
    public double serverTimeExpiry;
    public ulong sourceNetworkObjectId;
}

public struct SlowEvent
{
    public float speedMultiplier;
    public double serverTimeExpiry;
    public ulong sourceNetworkObjectId;
}

public struct DamageOverlayEvent
{
    public string overlayId;     // "fire", "poison", "bleed"
    public bool isApplying;
    public int remainingTicks;
}
```

Registration in the DI scope (same pattern as existing EventBus registrations):

```csharp
// In DefaultLifeTimeScope or equivalent
builder.Register<EventBus<StunEvent>>(Lifetime.Singleton);
builder.Register<EventBus<SlowEvent>>(Lifetime.Singleton);
builder.Register<EventBus<DamageOverlayEvent>>(Lifetime.Singleton);
```

Consumers inject them via `[Inject]` on fields — exactly how `PlayerResources` and `TestGameModeManager` already work:

```csharp
public class PlayerMovementController : NetworkBehaviour
{
    [Inject]
    EventBus<StunEvent> stunEventBus;

    private EventBinding<StunEvent> stunBinding;
    private bool _canMove = true;

    private void Start()
    {
        stunBinding = new EventBinding<StunEvent>(HandleStun);
        stunEventBus.Register(stunBinding);
    }

    private void HandleStun(StunEvent evt)
    {
        _canMove = !evt.isStunned;
    }

    private void OnDestroy()
    {
        stunEventBus.Unregister(stunBinding);
    }
}
```

This is exactly how `PlayerResources` uses `EventBus<DamageTakenEvent>` and `EventBinding<DamageTakenEvent>`. No new patterns, no string parsing, no casting.

---

## Concrete Effect Examples

### Stat Buff

```csharp
public class StatBuffEffect : IGameplayEffect
{
    float speedMultiplier;
    double expireTime;

    public bool IsExpired => NetworkManager.ServerTime.Time >= expireTime;
    public string EffectId => "stat_buff";

    public StatBuffEffect(float speedMul, float duration)
    {
        speedMultiplier = speedMul;
        expireTime = NetworkManager.ServerTime.Time + duration;
    }

    public void OnStart(EffectHolder target)
    {
        target.CombatManager.statModifierHolder.AddModifier(new EntityStatisticsModifier
        {
            speedMultiplier = speedMultiplier,
            serverTimeEffectEnd = expireTime
        });
        // Optional: trigger VFX/sound here
    }

    public void OnTick(EffectHolder target) { }
    public void OnFinish(EffectHolder target) { /* cleanup handled by StatModifierHolder expiry */ }
}
```

### Stun Effect (delegates to CrowdControlContainer)

```csharp
public class StunEffect : IGameplayEffect
{
    float duration;
    double expireTime;

    public bool IsExpired => NetworkManager.ServerTime.Time >= expireTime;
    public string EffectId => "stun";

    public StunEffect(float dur)
    {
        duration = dur;
        expireTime = NetworkManager.ServerTime.Time + dur;
    }

    public void OnStart(EffectHolder target)
    {
        target.CCContainer.ApplyCC("stun", expireTime);
        // Existing direct stun call can be removed after migration
        // target.CombatManager.SetStunTimer(duration);
    }

    public void OnTick(EffectHolder target) { }
    public void OnFinish(EffectHolder target) { }
}
```

**Migration note:** Over time, `SetStunTimer` and `InputCollectorScript.StunTime` are replaced entirely by `PlayerMovementController` subscribing to `StunEvent` via EventBus.

### Damage Over Time

```csharp
public class DamageOverTimeEffect : IGameplayEffect
{
    float damagePerTick, tickInterval, duration;
    double startTime, lastTickTime;
    ulong damagerNetworkId;

    public bool IsExpired => NetworkManager.ServerTime.Time - startTime >= duration;
    public string EffectId => "dot_" + damagerNetworkId; // unique per-instance for stacking

    public void OnStart(EffectHolder target)
    {
        startTime = NetworkManager.ServerTime.Time;
        lastTickTime = startTime;
    }

    public void OnTick(EffectHolder target)
    {
        double now = NetworkManager.ServerTime.Time;
        if (now - lastTickTime < tickInterval) return;

        lastTickTime = now;
        target.CombatManager.GetComponent<UnitResource>().damage(new SkillInstanceData
        {
            damage = (int)damagePerTick,
            ownerNetworkObjectId = damagerNetworkId
        });
    }

    public void OnFinish(EffectHolder target) { }
}
```

### Damage Accumulation (replaces YangBlastSkill's manual event wiring)

```csharp
public class DamageAccumulationEffect : IGameplayEffect
{
    public float StoredDamage { get; private set; }

    public bool IsExpired => false; // manual removal
    public string EffectId => "damage_accumulation";

    public void OnStart(EffectHolder target) { }
    public void OnTick(EffectHolder target) { }
    public void OnFinish(EffectHolder target) { }

    public void Add(float amount) => StoredDamage += amount;
    public float Consume() { float v = StoredDamage; StoredDamage = 0; return v; }
}
```

---

## How Existing Skills Change

### YangBlastSkill — Before

```csharp
// Manually subscribes to DamageTakenEvent
// Manually tracks stored damage as a NetworkVariable<float>
// Manually unregisters in OnDestroy
void OnDamageTaken(float dmg) => storedDamage.Value += dmg;
// In server RPC:
damage = this.damage + (int)storedDamage.Value;
storedDamage.Value = 0;
```

### YangBlastSkill — After

```csharp
DamageAccumulationEffect accumEffect;

void Start()
{
    accumEffect = new DamageAccumulationEffect();
    combatManagerRef.GetComponent<EffectHolder>().ApplyEffect(accumEffect);
}

// In server RPC:
damage = this.damage + (int)accumEffect.Consume();
```

The event subscription moves into `DamageAccumulationEffect` itself — reusable by any skill.

### RubyFormSkill — Before

```csharp
// Hardcoded: stat change + dissolve + particles + duration, all in one coroutine
StartCoroutine(FormSwap(duration));
```

### RubyFormSkill — After

```csharp
void Start()
{
    effectHolder.ApplyEffect(new StatBuffEffect(speedMultiplier: 3, duration: 3));
    effectHolder.ApplyEffect(new DissolveEffect(duration: 3, skinnedRenderers));
    // Rose particles can react via their own EventBus subscription
}
```

---

## Migration Strategy

Because `IGameplayEffect` is an interface (not a base class), it can coexist with the existing code. No skill needs to be rewritten before it's ready.

**Phase 1 — Infrastructure (no behaviour change)**
- Create `IGameplayEffect`, `EffectHolder`, `CrowdControlContainer`
- Create per-effect event structs (`StunEvent`, `SlowEvent`, etc.)
- Wire `EffectHolder` and `CrowdControlContainer` into the player prefab
- Register `EventBus<StunEvent>`, `EventBus<SlowEvent>`, etc. in the DI scope

**Phase 2 — Convert one behaviour**
- Convert RubyForm's speed buff into a `StatBuffEffect`
- Verify it works identically

**Phase 3 — Convert CC**
- Add `CrowdControlContainer.ApplyCC("stun", ...)` to `StunEffect.OnStart`
- Migrate `InputCollectorScript.StunTime` → `PlayerMovementController` subscribes to `StunEvent`
- Fix the "short stun overwrites long stun" bug

**Phase 4 — Expand**
- Add `DamageOverTimeEffect` for a new skill
- Add `DamageAccumulationEffect` — migrate YangBlastSkill to use it
- Add more effect types as new skills require them

---

## Design Rules

1. **The EffectHolder owns the lifecycle.** Listeners query state; they don't set timers.
2. **CC uses max-duration semantics.** A short stun never overwrites a longer one.
3. **Every effect has its own strongly-typed event.** Systems subscribe to only what they need — no string matching, no casting.
4. **Server-authoritative.** `EffectHolder.FixedUpdate()` runs on the server. Clients react via events.
5. **No central switch statements.** Adding a new effect type means a new class, not editing an enum or a giant if/else chain.