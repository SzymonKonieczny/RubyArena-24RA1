# Designing a Scalable Unity Ability System

The key is not predicting every future spell. That is impossible. Instead, design around a small set of **stable concepts**:

- An ability receives a context.
- It validates whether it can run.
- It selects targets.
- It pays costs and starts cooldowns.
- It produces effects.
- Effects modify game state.
- Passive abilities react to game events.
- Exceptional abilities can inject custom logic without rewriting the framework.

Most abilities in games such as League of Legends are combinations of reusable mechanics:

- Targeting
- Movement
- Damage and healing
- Status effects
- Projectiles
- Area queries
- Delays and channels
- Resource costs
- Event reactions
- Conditional modifiers

Below are two viable designs.

---

# Design 1: ScriptableObject Definitions + Composable Effects

This is usually the best starting point for a conventional Unity project.

The architecture separates:

1. **Definition** — immutable authored data.
2. **Runtime state** — cooldowns, charges, stacks and active casts.
3. **Execution** — the cast pipeline.
4. **Effects** — reusable operations such as damage or stun.
5. **Events** — notifications used by passives.
6. **Custom extensions** — an escape hatch for unusual abilities.

## High-level flow

```text
Input or AI
    ↓
AbilityController.TryActivate()
    ↓
Build AbilityContext
    ↓
Validate requirements
    ↓
Select targets
    ↓
Pay costs / start cooldown
    ↓
Run execution sequence
    ↓
Apply composed effects
    ↓
Publish gameplay events
    ↓
Passive abilities and status effects react
```

## Core data model

### Ability definition

A `ScriptableObject` contains immutable configuration. It must not hold per-character runtime state.

```csharp
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Abilities/Ability")]
public sealed class AbilityDefinition : ScriptableObject
{
    public string abilityId;
    public Sprite icon;

    public TargetingDefinition targeting;
    public List<AbilityRequirement> requirements;
    public List<AbilityCost> costs;
    public List<AbilityAction> actions;

    public float cooldown;
    public int maxCharges = 1;

    public List<GameplayTag> tags;
}
```

Examples of tags might be:

```text
Ability.Spell
Ability.Fire
Ability.Ultimate
Damage.Magical
CrowdControl.Stun
State.Silenced
State.Invulnerable
```

Tags reduce hard-coded type checks. Instead of asking:

```csharp
if (target.IsMagicImmune)
```

you can ask:

```csharp
if (target.Tags.Has("State.Immune.Magic"))
```

Use strongly identified tag assets or generated IDs in production rather than arbitrary strings.

---

## Runtime state

Each unit owns a runtime instance for every granted ability.

```csharp
public sealed class AbilityInstance
{
    public AbilityDefinition Definition { get; }
    public AbilityOwner Owner { get; }

    public float CooldownRemaining { get; private set; }
    public int Charges { get; private set; }
    public int Level { get; private set; }

    public AbilityInstance(
        AbilityDefinition definition,
        AbilityOwner owner,
        int level)
    {
        Definition = definition;
        Owner = owner;
        Level = level;
        Charges = definition.maxCharges;
    }

    public void Tick(float deltaTime)
    {
        CooldownRemaining =
            Mathf.Max(0f, CooldownRemaining - deltaTime);
    }

    public void StartCooldown(float duration)
    {
        CooldownRemaining = duration;
    }
}
```

Do not place `CooldownRemaining`, current stacks or current targets on the `ScriptableObject`. Unity assets are shared by every entity referencing them.

---

## Ability context

All execution-time information travels through a context object.

```csharp
public sealed class AbilityContext
{
    public AbilityInstance Ability { get; init; }
    public AbilityOwner Caster { get; init; }

    public Vector3 AimPoint { get; init; }
    public Vector3 AimDirection { get; init; }
    public GameObject ExplicitTarget { get; init; }

    public IReadOnlyList<GameObject> Targets { get; set; }

    public int CastId { get; init; }
    public int RandomSeed { get; init; }

    public bool IsPrediction { get; init; }
}
```

A context avoids passing a growing list of parameters through every method. It also gives you a place to carry:

- Caster
- Ability level
- Target
- Aim direction
- Hit location
- Source item or buff
- Random seed
- Network cast ID
- Snapshot statistics
- Cancellation token

However, do not turn it into a global dictionary where arbitrary systems insert untyped objects. Keep its core fields explicit and strongly typed.

---

## Validation pipeline

Requirements answer whether an ability can be cast.

```csharp
public readonly struct RequirementResult
{
    public bool Success { get; }
    public string FailureReason { get; }

    public RequirementResult(bool success, string failureReason = null)
    {
        Success = success;
        FailureReason = failureReason;
    }

    public static RequirementResult Pass()
        => new(true);

    public static RequirementResult Fail(string reason)
        => new(false, reason);
}

public abstract class AbilityRequirement : ScriptableObject
{
    public abstract RequirementResult Evaluate(AbilityContext context);
}
```

Reusable requirements include:

- Caster is alive.
- Caster is not silenced.
- Target is an enemy.
- Target is in range.
- Target is visible.
- Resource is sufficient.
- Required weapon is equipped.
- Ability has a charge.
- Caster is on the ground.
- Target has or lacks a tag.

```csharp
[CreateAssetMenu(menuName = "Game/Abilities/Requirements/Mana")]
public sealed class ManaRequirement : AbilityRequirement
{
    public float amount;

    public override RequirementResult Evaluate(AbilityContext context)
    {
        if (context.Caster.Resources.Mana < amount)
            return RequirementResult.Fail("Not enough mana.");

        return RequirementResult.Pass();
    }
}
```

Validation should not mutate game state. Paying mana belongs in the commit or cost stage, not in a requirement.

---

## Composable actions and effects

An ability is a sequence of actions. Each action does one job.

```csharp
public abstract class AbilityAction : ScriptableObject
{
    public abstract AbilityExecution Execute(AbilityContext context);
}
```

`AbilityExecution` can represent synchronous, asynchronous or interruptible work.

```csharp
public interface IAbilityExecution
{
    bool IsComplete { get; }
    bool IsCancelled { get; }

    void Tick(float deltaTime);
    void Cancel();
}
```

Common actions include:

- Play animation
- Wait for cast point
- Spawn projectile
- Perform area query
- Apply effect
- Move caster
- Teleport
- Create persistent zone
- Repeat action
- Branch on condition
- Play audiovisual cue

Effects modify targets:

```csharp
public abstract class GameplayEffect : ScriptableObject
{
    public abstract void Apply(
        AbilityContext context,
        GameObject target);
}
```

### Damage effect

```csharp
[CreateAssetMenu(menuName = "Game/Effects/Damage")]
public sealed class DamageEffect : GameplayEffect
{
    public DamageType damageType;
    public ScalableFloat amount;

    public override void Apply(
        AbilityContext context,
        GameObject target)
    {
        var receiver = target.GetComponent<IDamageReceiver>();
        if (receiver == null)
            return;

        float baseDamage = amount.Evaluate(context.Ability.Level);

        var request = new DamageRequest
        {
            Source = context.Caster.gameObject,
            Target = target,
            AbilityId = context.Ability.Definition.abilityId,
            BaseAmount = baseDamage,
            Type = damageType,
            CastId = context.CastId
        };

        context.Caster.Services.Damage.Resolve(request);
    }
}
```

The damage effect should create a request and send it through a central damage resolver. It should not directly subtract health.

A simplified damage pipeline could be:

```text
Base damage
    ↓
Source modifiers
    ↓
Critical-hit calculation
    ↓
Target resistance
    ↓
Shields
    ↓
Final health change
    ↓
Damage event
    ↓
Death check
```

This creates consistent rules for every source of damage.

---

## Example: Fireball

A fireball definition could be assembled from:

```text
Requirements
- Caster is alive
- Caster is not silenced
- Has 50 mana
- Has a valid aim direction

Costs
- Spend 50 mana
- Consume one charge

Actions
- Face aim direction
- Play cast animation
- Wait 0.25 seconds
- Spawn projectile

Projectile behavior
- Travel at configured speed
- Stop at first enemy hit
- On hit:
    - Apply magical damage
    - Apply burning status for 4 seconds
    - Spawn impact visual
```

There is no `FireballController` that handles mana, animation, collision, damage, burning and visuals all in one class. Each component has a narrow responsibility.

---

## Passive abilities

Passive abilities subscribe to gameplay events when granted and unsubscribe when removed.

```csharp
public interface IGameplayEvent
{
}

public readonly struct DamageDealtEvent : IGameplayEvent
{
    public GameObject Source { get; init; }
    public GameObject Target { get; init; }
    public float Amount { get; init; }
    public DamageType DamageType { get; init; }
    public string AbilityId { get; init; }
    public int CastId { get; init; }
}
```

A passive consists of:

```text
Trigger
    ↓
Conditions
    ↓
Effects
```

Example: “After damaging an enemy with a spell, gain a shield. This can occur once every 10 seconds.”

```text
Trigger:
- DamageDealtEvent

Conditions:
- Event source is this owner
- Source ability has Ability.Spell
- Target is an enemy
- Internal cooldown is ready

Effects:
- Apply shield
- Start 10-second internal cooldown
```

```csharp
public interface IPassiveRuntime : System.IDisposable
{
    void Enable();
    void Disable();
}
```

```csharp
public sealed class SpellShieldPassive : IPassiveRuntime
{
    private readonly AbilityOwner owner;
    private readonly GameplayEventBus eventBus;
    private float internalCooldown;

    public SpellShieldPassive(
        AbilityOwner owner,
        GameplayEventBus eventBus)
    {
        this.owner = owner;
        this.eventBus = eventBus;
    }

    public void Enable()
    {
        eventBus.Subscribe<DamageDealtEvent>(OnDamageDealt);
    }

    public void Disable()
    {
        eventBus.Unsubscribe<DamageDealtEvent>(OnDamageDealt);
    }

    public void Dispose()
    {
        Disable();
    }

    private void OnDamageDealt(DamageDealtEvent evt)
    {
        if (evt.Source != owner.gameObject)
            return;

        if (internalCooldown > 0f)
            return;

        owner.Effects.ApplyShield(100f);
        internalCooldown = 10f;
    }
}
```

For production, event subscriptions should support ownership tokens so everything associated with a removed passive can be automatically unsubscribed.

---

## Status effects and buffs

Buffs, debuffs, damage-over-time effects and crowd control can use the same effect system.

A status definition typically contains:

```csharp
public enum DurationPolicy
{
    Instant,
    Timed,
    Infinite
}

public enum StackPolicy
{
    Replace,
    RefreshDuration,
    AddStacks,
    IndependentInstances
}

[CreateAssetMenu(menuName = "Game/Effects/Status")]
public sealed class StatusDefinition : ScriptableObject
{
    public string statusId;
    public DurationPolicy durationPolicy;
    public StackPolicy stackPolicy;

    public float duration;
    public int maxStacks;

    public List<StatModifierDefinition> statModifiers;
    public List<GameplayTag> grantedTags;
    public List<PassiveTriggerDefinition> triggers;
    public List<GameplayEffect> periodicEffects;

    public float tickInterval;
}
```

Runtime status instances own mutable information:

```csharp
public sealed class StatusInstance
{
    public StatusDefinition Definition { get; init; }
    public GameObject Source { get; init; }
    public GameObject Target { get; init; }

    public float RemainingDuration { get; set; }
    public float TimeUntilNextTick { get; set; }
    public int Stacks { get; set; }
}
```

This supports:

- Poison
- Burning
- Shields
- Stuns
- Silence
- Slows
- Auras
- Stat increases
- Infinite equipment bonuses
- Temporary transformations

---

## Handling exceptional abilities

Composition will cover most abilities, but not all. Some abilities alter the world or game rules in a unique way.

Provide a controlled escape hatch:

```csharp
public abstract class CustomAbilityAction : AbilityAction
{
}
```

For example:

```csharp
[CreateAssetMenu(menuName = "Game/Abilities/Actions/SwapHealth")]
public sealed class SwapHealthAction : CustomAbilityAction
{
    public override AbilityExecution Execute(AbilityContext context)
    {
        var casterHealth = context.Caster.Health;
        var targetHealth =
            context.ExplicitTarget.GetComponent<HealthComponent>();

        float casterValue = casterHealth.Current;
        float targetValue = targetHealth.Current;

        casterHealth.SetCurrent(targetValue);
        targetHealth.SetCurrent(casterValue);

        return AbilityExecution.Completed;
    }
}
```

The custom action still uses the system’s:

- Validation
- Targeting
- Cooldown
- Costs
- Cast state
- Cancellation
- Logging
- Networking
- Events

Only the unique behavior is custom.

That is not a failure of the architecture. A healthy ability system should deliberately support unique code without forcing that code into the central framework.

---

## Advantages

- Designer-friendly with custom inspectors.
- Easy to introduce incrementally.
- Good fit for `MonoBehaviour` projects.
- Abilities are assembled from reusable components.
- Unique abilities still have a clean extension point.
- Straightforward debugging when execution is explicit.

## Disadvantages

- Large numbers of nested `ScriptableObject` assets can be cumbersome.
- Complex branching sequences may need a graph editor.
- Event subscriptions require careful lifetime management.
- Polymorphic assets can create serialization and content-versioning issues.
- Very high entity counts may make object-heavy execution expensive.

---

# Design 2: Event-Driven ECS Ability System

This approach is useful when you have:

- Thousands of entities
- Large-scale battles
- Deterministic simulation
- Server-authoritative multiplayer
- Unity Entities or a custom ECS
- A strong need to separate simulation from presentation

Instead of invoking behavior through object hierarchies, abilities produce data commands processed by systems.

## Basic model

An ability activation creates a request:

```csharp
public struct ActivateAbilityRequest
{
    public Entity Caster;
    public Entity Ability;
    public Entity ExplicitTarget;

    public float3 AimPoint;
    public float3 AimDirection;

    public uint CastId;
}
```

Systems process the request in stages:

```text
ActivateAbilityRequest
    ↓
AbilityValidationSystem
    ↓
AbilityCostSystem
    ↓
AbilityCastSystem
    ↓
TargetQuerySystem
    ↓
EffectEmissionSystem
    ↓
DamageSystem / StatusSystem / MovementSystem
    ↓
GameplayEventSystem
    ↓
PassiveReactionSystem
```

No stage needs to know every possible spell.

## Ability as instruction data

An ability can compile into an instruction sequence:

```csharp
public enum AbilityOpcode : byte
{
    Wait,
    SelectTarget,
    SelectArea,
    DealDamage,
    Heal,
    ApplyStatus,
    SpawnProjectile,
    MoveCaster,
    BranchIf,
    Repeat,
    EmitEvent
}

public struct AbilityInstruction
{
    public AbilityOpcode Opcode;
    public int DataIndex;
    public int NextInstruction;
    public int FailureInstruction;
}
```

Authored ability assets are converted into efficient runtime blobs:

```text
Fireball
Instruction 0: Spend mana
Instruction 1: Wait for cast point
Instruction 2: Spawn projectile
Instruction 3: On collision, select hit entity
Instruction 4: Emit damage request
Instruction 5: Emit apply-status request
Instruction 6: Complete
```

A cast becomes a lightweight state machine:

```csharp
public struct ActiveCast
{
    public Entity Caster;
    public Entity Ability;

    public int InstructionPointer;
    public float WaitRemaining;

    public uint CastId;
    public CastState State;
}
```

---

## Effects as commands

Systems communicate using explicit commands.

```csharp
public struct DamageRequest
{
    public Entity Source;
    public Entity Target;
    public Entity Ability;

    public float BaseAmount;
    public DamageType Type;

    public uint CastId;
}

public struct ApplyStatusRequest
{
    public Entity Source;
    public Entity Target;
    public BlobAssetReference<StatusBlob> Status;

    public float Duration;
    public int Stacks;

    public uint CastId;
}
```

The damage system is the only system allowed to resolve damage. The status system is the only system allowed to add or stack statuses.

This prevents an ability from bypassing global rules accidentally.

---

## Passive reactions

Passive abilities are represented as reaction data:

```csharp
public enum GameplayEventType : byte
{
    DamageAttempted,
    DamageDealt,
    DamageReceived,
    AbilityStarted,
    AbilityCompleted,
    StatusApplied,
    EntityKilled,
    EntityDied,
    MovementStarted
}

public struct ReactionDefinition
{
    public GameplayEventType EventType;
    public ConditionSet Conditions;
    public EffectSequence Effects;
    public float InternalCooldown;
}
```

A passive reaction system evaluates relevant events:

```text
DamageDealt event
    ↓
Find reactions listening for DamageDealt
    ↓
Evaluate source, target and tag conditions
    ↓
Check internal cooldown
    ↓
Emit configured effects
```

For performance, reactions should be indexed by event type. Do not scan every passive in the game for every event.

---

## Advantages

- Data-oriented and scalable.
- Easier to run the simulation independently of Unity presentation.
- Better fit for deterministic or server-authoritative games.
- Explicit command streams are useful for replays and debugging.
- Systems own game rules instead of individual abilities.
- Good batch performance for large numbers of entities.

## Disadvantages

- Higher initial complexity.
- Authoring and debugging tools are essential.
- Unique abilities can be awkward without a custom instruction or system.
- Generic event streams can become difficult to inspect.
- Structural changes and command ordering need strict rules.
- Often excessive for a small action RPG.

---

# Comparison

| Concern | ScriptableObject composition | Event-driven ECS |
|---|---|---|
| Initial implementation | Easier | Harder |
| Unity inspector workflow | Natural | Requires conversion/tooling |
| Small-to-medium battles | Excellent | Good but possibly excessive |
| Thousands of entities | Potentially expensive | Strong |
| Unique spells | Custom action class | Custom opcode or system |
| Multiplayer simulation | Viable with discipline | Naturally suitable |
| Debugging | Familiar call stacks | Requires event tracing |
| Designer authoring | Good | Good only with tools |
| Runtime allocations | Must be controlled | Easier to eliminate |
| Determinism | Requires deliberate design | Easier to enforce |

---

# Recommended Hybrid

For most Unity games, I would use a hybrid of the two:

1. Author abilities as `ScriptableObject` graphs or lists.
2. Compile them into immutable runtime definitions.
3. Store cooldowns, charges and casts in runtime instances.
4. Execute abilities through a command pipeline.
5. Route damage, healing, movement and statuses through dedicated systems.
6. Use typed events for passive reactions.
7. Permit custom actions for genuinely unique mechanics.
8. Keep animation and VFX outside the authoritative gameplay simulation.

The layers would look like this:

```text
Authoring layer
- ScriptableObject definitions
- Custom inspectors
- Optional node graph
- Validation tools

Simulation layer
- Ability instances
- Active cast state machines
- Target queries
- Damage system
- Status system
- Stat system
- Gameplay events
- Passive reactions

Presentation layer
- Animation
- VFX
- Audio
- Camera
- UI
- Floating combat text
```

The simulation should emit presentation events:

```csharp
public readonly struct ProjectileSpawnedPresentationEvent
{
    public int CastId { get; init; }
    public Vector3 Start { get; init; }
    public Vector3 End { get; init; }
    public string VisualId { get; init; }
}
```

Presentation consumes those events but does not decide whether damage occurred.

---

# Essential Supporting Systems

<details>
<summary><strong>Stat system</strong></summary>

Do not let every spell calculate statistics independently.

Use a central stat system with modifiers:

```csharp
public enum ModifierOperation
{
    FlatAdd,
    PercentAdd,
    Multiply,
    Override
}

public readonly struct StatModifier
{
    public StatId Stat { get; init; }
    public ModifierOperation Operation { get; init; }
    public float Magnitude { get; init; }
    public object Source { get; init; }
    public int Priority { get; init; }
}
```

Define a documented evaluation order. For example:

$$
V_{\text{final}}
=
\left(V_{\text{base}} + \sum F\right)
\left(1 + \sum P\right)
\prod M
$$

Every stat should use the same rule unless a particular stat explicitly defines another one.

Modifiers need source handles so removing a buff can remove all modifiers it added.

</details>

<details>
<summary><strong>Targeting system</strong></summary>

Target selection should be separate from effect application.

Common target selectors include:

- Self
- Explicit unit
- Raycast
- Cone
- Circle
- Box
- Chain
- Nearest valid target
- Lowest-health ally
- Random enemy
- Previous hit target
- Entities with required or blocked tags

A target query should return candidates, and filters should reduce them:

```text
Circle query
    ↓
Alive filter
    ↓
Enemy filter
    ↓
Visible filter
    ↓
Not invulnerable filter
    ↓
Sort by distance
    ↓
Take first 3
```

This is more reusable than writing a new physics query for each spell.

</details>

<details>
<summary><strong>Cast state machine</strong></summary>

Treat activation as a state machine:

```text
Requested
    ↓
Validated
    ↓
Committed
    ↓
Casting
    ↓
Channeling, if applicable
    ↓
Executing
    ↓
Completed
```

Cancellation may occur during specific phases:

```text
Casting → Cancelled
Channeling → Interrupted
Executing → Cancelled, only if the action permits it
```

Separate:

- **Cast time** — delay before the ability takes effect.
- **Channel time** — period requiring continued casting.
- **Recovery time** — period before another action.
- **Cooldown** — time before the same ability can be used again.

Define exactly when costs and cooldowns are committed. For example:

- Mana is paid when casting begins.
- A charge is consumed when casting begins.
- Cooldown begins when execution succeeds.
- A cancelled cast refunds mana but not the charge.

These are game rules, so make them configurable policies rather than scattered conditionals.

</details>

<details>
<summary><strong>Event safety and recursion</strong></summary>

Reactive passives can create infinite chains:

```text
Damage causes heal
→ heal causes damage
→ damage causes heal
→ ...
```

Every event should carry provenance:

```csharp
public readonly struct EffectProvenance
{
    public int RootCastId { get; init; }
    public int ParentEffectId { get; init; }
    public int Depth { get; init; }
    public EffectFlags Flags { get; init; }
}
```

Useful flags include:

```text
CannotTriggerPassives
IsPeriodic
IsReflected
IsSecondary
IsItemEffect
IsCritical
```

Also implement:

- Maximum reaction depth
- Per-event trigger limits
- Internal cooldowns
- “Cannot proc itself” rules
- Stable event ordering
- Deferred event processing

Prefer an event queue over unrestricted nested event calls. Finish the current operation, enqueue resulting events, and then process them in a defined order.

</details>

<details>
<summary><strong>Networking and prediction</strong></summary>

For multiplayer, separate the cast request from authoritative acceptance:

```text
Client:
- Predict cast
- Play immediate animation
- Send cast request

Server:
- Validate state, target, range and resources
- Assign authoritative cast ID
- Execute simulation
- Replicate results

Client:
- Reconcile prediction
- Correct rejected or changed outcomes
```

Do not trust client-provided damage, targets or cooldown state.

Use stable IDs rather than references to Unity assets across the network. Random effects should use an authoritative seed stored in the cast context.

</details>

---

# Rules That Prevent Spaghetti

## 1. Abilities orchestrate; systems enforce rules

A spell may request damage, but the damage system resolves it.

A spell may request movement, but the movement system determines whether it is legal.

A spell may request a status, but the status system handles immunity, stacking and duration.

## 2. Data assets are immutable

Mutable state belongs to runtime instances, not `ScriptableObject` definitions.

## 3. Prefer composition, but permit custom code

Trying to create a fully generic language for every possible spell often produces a worse system than ordinary code.

A good target is:

- Most abilities are purely compositional.
- Some abilities combine composition with one custom action.
- A small number use dedicated mechanics.

## 4. Use typed events

Avoid a universal event such as:

```csharp
Publish("SomethingHappened", Dictionary<string, object> data);
```

Prefer typed payloads such as:

```csharp
Publish(new DamageDealtEvent { ... });
```

Typed events are searchable, refactorable and testable.

## 5. Do not let visual code own gameplay

Animation events can notify the simulation that a presentation marker was reached, but authoritative damage should not exist only inside an animation callback.

## 6. Give every effect a source

Every modifier, status, damage request and summoned entity should know:

- Who created it
- Which ability created it
- Which cast created it
- Which parent effect created it

This is critical for cleanup, attribution, assists, death reports and debugging.

## 7. Build tracing from the beginning

A cast trace might look like:

```text
Cast 1842: Fireball
- Validation passed
- Spent 50 mana
- Started 6-second cooldown
- Projectile 992 created
- Projectile hit Entity 71
- Damage requested: 120 magical
- Resistance reduced damage to 84
- Shield absorbed 20
- Health reduced by 64
- Burning applied for 4 seconds
- Passive 31 triggered from DamageDealt
```

Without tracing, event-driven ability systems become extremely difficult to debug.

---

# Suggested Unity Project Layout

```text
Assets/Game/Abilities/
├── Authoring/
│   ├── AbilityDefinition.cs
│   ├── TargetingDefinition.cs
│   └── Editors/
├── Runtime/
│   ├── AbilityController.cs
│   ├── AbilityInstance.cs
│   ├── AbilityContext.cs
│   ├── ActiveCast.cs
│   └── AbilityRunner.cs
├── Actions/
│   ├── ApplyEffectAction.cs
│   ├── SpawnProjectileAction.cs
│   ├── WaitAction.cs
│   ├── AreaQueryAction.cs
│   └── Custom/
├── Effects/
│   ├── DamageEffect.cs
│   ├── HealEffect.cs
│   ├── ApplyStatusEffect.cs
│   └── ModifyResourceEffect.cs
├── Statuses/
│   ├── StatusDefinition.cs
│   ├── StatusInstance.cs
│   └── StatusSystem.cs
├── Targeting/
│   ├── TargetQuery.cs
│   ├── TargetFilter.cs
│   └── TargetingSystem.cs
├── Events/
│   ├── GameplayEventBus.cs
│   └── EventTypes/
├── Passives/
│   ├── PassiveDefinition.cs
│   ├── PassiveRuntime.cs
│   └── PassiveReactionSystem.cs
└── Tests/
```

---

# Practical Implementation Order

Build the system vertically rather than attempting the entire framework first:

1. Implement an instant targeted damage ability.
2. Add costs and cooldowns.
3. Add area targeting.
4. Add projectiles.
5. Add timed statuses and stacking.
6. Add typed gameplay events.
7. Add one passive triggered by damage.
8. Add cast times, channels and cancellation.
9. Add stat modifiers.
10. Add execution tracing.
11. Add custom editor validation.
12. Add networking or ECS conversion only if required.

Create several deliberately different test abilities:

- Instant heal
- Projectile with damage-over-time
- Channeled beam
- Dash that damages enemies crossed
- Aura that buffs allies
- Passive that reacts to critical hits
- Toggle that drains mana
- Summon with inherited statistics
- Ability that recasts within a time window

If these can coexist without editing central switch statements, the architecture is moving in the right direction.

## Bottom line

For most Unity RPGs or MOBAs, start with **ScriptableObject-authored, composable abilities backed by runtime instances and dedicated gameplay systems**. Add a typed event/reaction layer for passives, and provide custom actions for unusual mechanics.

Choose the ECS version when simulation scale, determinism or server architecture justifies the additional complexity. The most robust production solution is usually a hybrid: friendly object-based authoring compiled into an explicit, command-driven runtime.