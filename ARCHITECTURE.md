# Ruby Arena — Codebase Documentation

> **Version:** 0.0.2f (Pre-Alpha)  
> **Engine:** Unity (HDRP) with Netcode for GameObjects  
> **DI Container:** VContainer  
> **State:** Active development on the `general-refactor` branch  
> **Repository:** [GitHub — SzymonKonieczny/RubyArena-24RA1](https://github.com/SzymonKonieczny/RubyArena-24RA1)

---

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [Project Structure](#2-project-structure)
3. [Core Systems](#3-core-systems)
    - [Networking & Multiplayer](#31-networking--multiplayer)
    - [Player System](#32-player-system)
    - [Skill System (New)](#33-skill-system-new)
    - [Skill System (Legacy)](#34-skill-system-legacy)
    - [Stats & Stat Modifiers](#35-stats--stat-modifiers)
    - [Event Bus](#36-event-bus)
    - [Game Mode System](#37-game-mode-system)
    - [Minion System](#38-minion-system)
    - [UI System](#39-ui-system)
    - [Utility Layer](#310-utility-layer)
4. [Data Flow Diagrams](#4-data-flow-diagrams)
5. [Design Observations & Technical Debt](#5-design-observations--technical-debt)
6. [Refactoring Suggestions](#6-refactoring-suggestions)
7. [Getting Started for Contributors](#7-getting-started-for-contributors)

---

## 1. Architecture Overview

Ruby Arena is a multiplayer (PvP) arena game built on **Unity Netcode for GameObjects** (NGO). It uses a **Host-authoritative model** where one peer acts as both server and client. The project was built incrementally over 2 years with bursts of activity, which has resulted in two parallel skill systems, an evolving architecture, and several TODO-marked areas.

### Key Technology Choices
- **Networking:** Unity Netcode for GameObjects (NGO) — `NetworkBehaviour`, `NetworkVariable`, `[ServerRpc]` / `[ClientRpc]`
- **Dependency Injection:** VContainer — `LifetimeScope`, `[Inject]`, `IObjectResolver`
- **Transport:** Unity Transport (UTP) with **Relay** support (Unity Relay Service + Authentication)
- **Camera:** Cinemachine FreeLook, with a toggle between Walking / Aiming camera positions
- **Visuals:** Unity HDRP, MaterialPropertyBlock for per-instance shader properties

### High-Level Flow

```
[MenuScript] 
    │ StartHost/StartClient
    ▼
[ServerPlayerStateManager] 
    │ OnClientConnected → spawns LocalPlayerStateManager per client
    ▼
[PlayerObjectSpawner]
    │ OnClientConnected → spawns player prefab networked
    ▼
[PlayerScript]
    │ OnNetworkSpawn → Initializes: Model, Skills, Stats, UI references
    ▼
[Game Mode] (e.g. TestGameModeManager / BrawlGameMode)
    │ Implements IGameMode, registers players, manages objectives
    ▼
[Game Loop]
    │ InputCollector → SkillBase.Use() → ServerRpc → Execute → ClientRpc
```

---

## 2. Project Structure

```
RubyArenaProject/Assets/
├── Editor/                          # Custom Editors, DisableLightCookies
├── Resources/Prefabs/
│   └── NewSkillSystemCarriers/      # Skill prefabs with SkillBase on them
│       ├── AutoAttack/
│       ├── SniperShot/
│       └── ...
├── Scripts/
│   ├── Characters/
│   │   └── CharacterModelSO.cs      # ScriptableObject for character definitions
│   ├── Events/
│   │   ├── DamageTakenEvent.cs      # Data class: who damaged whom, how much
│   │   ├── GlobalStatsChangeEvent.cs
│   │   └── PlayerDeathEvent.cs
│   ├── Gamemodes/
│   │   ├── IGameMode.cs             # Interface: CanDamage, RegisterPlayer, RegisterNetworkedObject
│   │   ├── Brawl/                   # Team-vs-team base destruction
│   │   ├── LifeTimeScopes/          # VContainer LifetimeScope wiring
│   │   └── TestGameMode/            # Free-for-all capture-point game mode
│   ├── General Server/              # Server-authoritative managers
│   │   ├── LocalPlayerStateManager.cs
│   │   ├── PlayerObjectSpawner.cs
│   │   ├── ServerPlayerStateManager.cs
│   │   ├── SkillManagerScript.cs    # Legacy — largely unused
│   │   └── relayManager.cs          # Stub / placeholder
│   ├── Player/
│   │   ├── EntityStats.cs           # Networked Stats struct holder
│   │   ├── PlayerCombatManager.cs   # Combat coordination, stun, NonFightArea
│   │   ├── PlayerMove.cs            # Movement (Walking & Aiming modes)
│   │   ├── PlayerMoveWithPrediction.cs  # Client-prediction stub (incomplete)
│   │   ├── PlayerResources.cs       # HP/Mana, damage handling, UI binding
│   │   ├── PlayerScript.cs          # Main player orchestrator
│   │   ├── StatModifierHolder.cs    # Temporal stat effect management
│   │   └── UnitResource.cs          # Abstract base for damageable entities
│   ├── Services/
│   │   └── Implementations/
│   │       └── EventBus.cs          # Generic typed event bus
│   ├── Skills/
│   │   ├── AutoAttackSkill.cs       # Legacy — empty
│   │   ├── NewSkillSystem/          # Current skill implementation pattern
│   │   └── SkillClasses/            # Legacy skill entity system
│   ├── UI/
│   │   ├── CharacterCardScript.cs
│   │   ├── CharacterCardSO.cs
│   │   └── MenuScript.cs
│   └── Utility/
│       ├── ClientAuthNetworkTransform.cs
│       ├── ClientPredictionTypes.cs
│       ├── DebugUtils.cs
│       ├── DontDestroyOnLoad.cs
│       └── InputCollectorScript.cs
├── [Root-level files]
│   ├── CharacterList.cs             # Singleton of all characters
│   ├── CharacterSelectUIScript.cs   # Character selection grid
│   ├── InGameCanvasManager.cs       # Escape menu, cursor management
│   ├── LobbyCanvasManager.cs        # Lobby UI, player cards, start game
│   ├── PersistentCanvasManger.cs    # Singleton persistent HUD reference
│   ├── PlayerAnimationScript.cs     # Animator proxy
│   ├── PlayerSkillHolder.cs         # Aggregation point for player systems
│   ├── TeamBaseBrawlGameMode.cs     # Brawl base with health + minion tracking
│   ├── UISkillIcon.cs               # Cooldown display per skill slot
│   ├── SkillsUIManager.cs           # Layout for skill icons
│   └── ...various Skill prefab carriers
```

---

## 3. Core Systems

### 3.1 Networking & Multiplayer

**Architecture:**
- Uses Unity Netcode for GameObjects (NGO)
- **Host-authoritative:** The host is both server and client
- **Relay support:** The `MenuScript` wraps NGO with Unity Relay for P2P without port forwarding
- **Client-authoritative transform:** `ClientAuthNetworkTransform` overrides `NetworkTransform` to set `OnIsServerAuthoritative() => false` (but is NOT actually used — `PlayerMove` handles movement manually)

**Key classes:**

| Class | Role |
|---|---|
| `MenuScript` | Boot — initializes Unity Services, creates/joins Relay allocations, starts Host/Client |
| `ServerPlayerStateManager` | Singleton. Tracks `Dictionary<ulong, LocalPlayerStateManager>` per client. Spawns per-client state carriers |
| `LocalPlayerStateManager` | Per-client state: chosen character ID, lobby-ready flag, local player reference |
| `PlayerObjectSpawner` | Server-only. On `OnClientConnectedCallback`, instantiates player prefab, injects DI, spawns with ownership |
| `PlayerScript` | Main player NetworkBehaviour: initializes model, skills, combat, movement; passes character ID to spawn logic |

**Data flow for player connection:**
```
Client connects
  → ServerPlayerStateManager.OnConnectedPlayer
    → Spawns LocalPlayerStateManager (owned by that client)
  → PlayerObjectSpawner.SpawnPlayer
    → Instantiates player prefab
    → container.InjectGameObject (VContainer DI)
    → Sets characterID from the lobby selection
    → SpawnWithOwnership(clientId)
  → PlayerScript.OnNetworkSpawn
    → InitializePlayer() → InitializeCharacter()
      → Instantiates character model
      → Spawns skill carriers as child NetworkObjects
      → Wires up camera (owner only)
      → Connects to PersistentCanvasManger
      → Registers with GameMode on scene load complete
```

**Network synchronisation patterns used:**
- `NetworkVariable<T>` — for state that changes rarely (HP, cooldowns, stats, team health)
- `[ServerRpc]` / `[ClientRpc]` — for actions (skill usage, teleport requests, animations)
- Manual `Rigidbody.velocity` sync via `NetworkVariable<Vector3> RbVelocityNetworkVar` (owner writes, others read)

### 3.2 Player System

#### `PlayerScript` — The Player Orchestrator

`PlayerScript` (`RubyArenaProject/Assets/Scripts/Player/PlayerScript.cs`) is the root `NetworkBehaviour` for a player. It:

1. **Initializes** the character model from `CharacterList` using the `characterID` `NetworkVariable<int>`.
2. **Spawns skill carriers** for AutoAttack, Skill1, and Skill2 as child `NetworkObject`s with ownership.
3. **Wires up references** to `PersistentCanvasManger`, `LocalPlayerStateManager`, and camera systems.
4. **Registers the player** with the active `IGameMode` on scene load completion.

**Components on the player prefab:**
```
PlayerScript (NetworkBehaviour)
├── Movement (PlayerMove) — Rigidbody-based movement
├── PlayerCombatManager — combat coordination
├── PlayerResources (UnitResource) — HP/Mana, damage handling
├── PlayerAnimationScript — Animator proxy
├── PlayerSkillHolder — aggregation point (Stats, input, animation, modifiers)
├── InputCollectorScript — local input collection + stun
├── StatModifierHolder — temporal stat effects
├── EntityStats — base + modified stats as NetworkVariables
├── Rigidbody
└── Collider
```

#### `PlayerMove` — Movement System

**Two orientation modes:**
- **Walking:** Character moves relative to camera orientation; smooth model rotation via `Vector3.Slerp`.
- **Aiming:** Character faces the direction of a screen-center raycast; movement relative to model forward. Accessible via right-click toggle.

**Movement mechanics:**
- Manual velocity assignment on `Rigidbody.velocity` in `FixedUpdate`.
- Stun: `InputCollector.StunTime > 0` → velocity zeroed.
- Flight: when `canFly` stat is true, Space/Shift control vertical movement.
- Dash: coroutine-based, adds position offset over time.
- Server-requested velocity/force: accumulated in `RequestedVelocityToAdd` / `RequestedForceToAdd`, applied on next fixed update.

**Client-prediction:** `PlayerMoveWithPrediction` exists as a stub with an `InputState` struct and buffer queue, but is **incomplete and disabled** (`NetworkTransform` disabled in Start).

#### `PlayerCombatManager` — Combat Coordination

- Holds `SkillshotSpawnPoint` transform reference.
- Manages `canCombat` `NetworkVariable<bool>` (disabled in `NonFightArea` trigger zones).
- `SetStunTimer(float)` — accumulates stun time on the `InputCollectorScript`.
- Initialized from `PlayerScript` and from `OnTransformParentChanged` (a reparenting catch).

#### `PlayerResources` — HP & Mana

- Extends `UnitResource` (abstract base: `damage()`, `takeMana()`, `getHP()`, `getMana()`).
- **Damage flow:** `damage(SkillInstanceData)` → calls `gameMode.CanDamage()` → deducts HP → raises `DamageTakenEvent` → raises `PlayerDeathEvent` if HP ≤ 0.
- **HP/Mana** are `NetworkVariable<float>` — server-authoritative.
- **Client-side:** on `OnValueChanged`, updates slider UI + plays bleeding particles / camera impulse.
- Uses `VContainer [Inject]` for `IGameMode`, `EventBus<DamageTakenEvent>`, `EventBus<PlayerDeathEvent>`.

### 3.3 Skill System (New)

The **New Skill System** (`Assets/Scripts/Skills/NewSkillSystem/`) represents the current approach. Skills are `NetworkBehaviour` prefabs **parented to** `PlayerSkillHolder` at runtime.

#### `SkillBase` — Abstract Base

```csharp
public abstract class SkillBase : NetworkBehaviour
{
    public abstract bool Use();                              // Client-side: validate + request
    public virtual void ServerSideUseServerRPC(Vector3, ...)  // Server: execute
    public virtual void ServerAnnounceSpellCastClientRPC()     // Server→Client: confirm
}
```

**Common skill pattern:**
1. `Update()` checks `spellTriggeringFlag.value && !isOnCooldown()` and calls `Use()`.
2. `Use()` → plays windup animation, sets stun timer, computes look direction → calls `ServerSideUseServerRPC(LookDir)`.
3. `ServerSideUseServerRPC()` → validates (cooldown), sets cooldown, spawns entities/applies effects → calls `ServerAnnounceSpellCastClientRPC()`.
4. `ServerAnnounceSpellCastClientRPC()` → plays acknowledge animations, handles client-side VFX.

**Input wiring:**
- `InputCollectorScript` exposes `BoolRefType` for Q, E, left-click.
- `SkillBase.Init()` maps `castType` → `spellTriggeringFlag` via `setTriggerFlagRef()`.
- `BoolRefType` is a class wrapper (needed for reference semantics — input is written to the same object).

**Implemented skills:**

| Skill | Character | Behavior | Highlights |
|---|---|---|---|
| `RootSkill` | (Projectile) | Fires a forward projectile with windup delay | `StartCoroutine(SpawnEntityDelayed)` |
| `BlinkSkill` | (Blink) | Teleports to raycast point / max range | Dissolve shader via `PropertyBlockMaterial` |
| `WeissDashSkill` | Weiss | Forward dash | Calls `playerMove.startDash()` |
| `YangBlastSkill` | Yang | Cone AOE with stored damage | Listens to `DamageTakenEvent`, accumulates damage, applies it + knockback |
| `RubyFormSkill` | Ruby | Transform with speed boost + VFX | Dissolve in/out, stat modifier applied, ball model swap |
| `AutoAttackSkillCarrier` | (Universal) | Box-cast auto-attack | Equippable by any character, stats synced from `EntityStats` |
| `SniperShotSkillCarrier` | (Sniper) | Long-range raycast | Uses `ISkillEffect` interface for VFX |
| `TestSkillNewSystem` | (Testing) | Spawns an entity using legacy `BaseSkillEntityBehavior` | Bridges old entity system to new skill carrier |

#### `SkillDataSO` — ScriptableObject Data

Each skill carrier references a `SkillDataSO` instance containing:
- `Icon` (for UI)
- `damage`, `castTime` (windup), `cooldown`

Despite being a ScriptableObject, some skills override fields at runtime (`SkillDataSO.damage = damage`).

### 3.4 Skill System (Legacy)

`Assets/Scripts/Skills/SkillClasses/` contains an **older approach** that was never fully cleaned up:

| Class | Description |
|---|---|
| `BaseSkill` | Abstract class (NOT NetworkBehaviour) — `CanCast()`, `ChangeSkilRequestState()`, `OnRequestStateChange()` |
| `BaseSkillEntityBehavior` | NetworkBehaviour for world-placed skill entities (projectiles, AOE fields) |
| `BaseSkillEntityBehaviorSkillShot` | Extended base for skillshots: on collision → `GetAffectedObjectS()` → `AffectObjectS()` |
| `EntitySpawningSkill` | Stub — all methods throw `NotImplementedException` |
| `TestQSkill` | Concrete skillshot: moves forward, on hit does damage + knockback force via `ClientRpc` |

**Key difference from new system:** Legacy entities use `CollisionRegisteredOnServer` via `Hit` `NetworkVariable<bool>`, driving a separate execution path. The new system tends to execute server-side logic directly in `ServerSideUseServerRPC` and only use spawned entities for visual/state representation.

### 3.5 Stats & Stat Modifiers

#### `EntityStats` — Stat Holder

- Holds `basicStats` and `modifiedStats` as `NetworkVariable<Stats>`.
- `Stats` struct: `speed`, `canFly`, `AttackDamage`, `AttackSpeed`, `AttackRange`. Implements `INetworkSerializable`.
- On `basicStats.OnValueChanged`, copies to `modifiedStats`.
- `WriteModifiedStats()` sets `modifiedStats` and raises `GlobalStatsChangeEvent`.

#### `StatModifierHolder` — Temporal Modifiers

- Stores a `List<EntityStatisticsModifier>` with expiry timestamps.
- `EntityStatisticsModifier`: `speedMultiplier`, `AttackDamageBoost`, `AttackSpeedMultiplier`, `serverTimeEffectEnd`.
- `AddModifier()` → recalculates stats from base + all modifiers → calls `entityStats.WriteModifiedStats()`.
- `FixedUpdate()` removes expired modifiers and recalculates.

**Commented-out design:** `StatModifierHolder.cs` contains a commented-out `IGameplayEffect` interface proposal with `OnStart()`, `OnFinish()`, `OnFixedUpdate()` — indicating a planned pipeline pattern that was never implemented.

### 3.6 Event Bus

**File:** `Assets/Scripts/Services/Implementations/EventBus.cs`

A simple generic event bus:

```csharp
EventBus<T> where T : class
    .Register(EventBinding<T>)     // Subscribe
    .Unregister(EventBinding<T>)   // Unsubscribe
    .Raise(T arg)                  // Publish to all subscribers
```

- Registered as **Singleton** in `DefaultLifeTimeScope` via `builder.Register(typeof(EventBus<>), Lifetime.Singleton)`.
- Injected via `[Inject] EventBus<DamageTakenEvent> damageEventBus` (VContainer supports open generics).
- `EventBinding<T>` wraps `Action<T>`.

**Events defined:**

| Event | Fields | Publisher | Subscribers |
|---|---|---|---|
| `DamageTakenEvent` | `damagerNetworkObjectId`, `recieverNetworkObjectId`, `spellCarrierNetworkObjectId`, damage amounts, health before/after | `PlayerResources.damage()` | `YangBlastSkill` (accumulates damage), `TestGameModeManager` |
| `PlayerDeathEvent` | `playerKillingNetworkId`, `playerKilledNetworkId` | `PlayerResources.damage()` | `TestGameModeManager` |
| `GlobalStatsChangeEvent` | `recieverNetworkObjectId` | `EntityStats.WriteModifiedStats()` | `AutoAttackSkillCarrier` (re-reads stats) |

**Typo note:** `Unregister` is misspelt as `Unrgister` throughout the codebase.

### 3.7 Game Mode System

#### `IGameMode` Interface

```csharp
public interface IGameMode
{
    bool CanDamage(ulong networkId);        // Permission check before damage
    void RegisterPlayer(ulong networkId);   // Called when player joins mid-game
    void RegisterNetworkedObject(ulong networkId);  // Called for spawned entities
}
```

#### `TestGameModeManager` — Free-for-all Capture Point

- **Win condition:** First to `pointsToWin` (3) capture-point captures.
- **Capture point:** `GameModeCaptureOwnedObjective` — only the player currently assigned can capture it. On capture, teleports to a random location, assigns to the capturing player.
- **Death handling:** On `PlayerDeathEvent`, respawns at `playerRespawn`, heals to full, reassigns capture point to the killer.
- **Scoreboard:** `setScoreboardPointClientRPC` sends scores to all clients for display.
- **Victory:** Sets `winningPlayerNetworkObjectId` → `AnnounceWinner` shows text → 5-second coroutine loads lobby scene.
- **Wiring:** Registered in `DefaultLifeTimeScope` as `builder.RegisterComponentInHierarchy<TestGameModeManager>().As<IGameMode>()`.

#### `BrawlGameMode` — Team Base Destruction

- **Teams:** Red and Blue (`BrawlTeam` enum).
- **Base health:** `TeamBaseBrawlGameMode` — health decreases by minions-in-range × deltaTime.
- **Team health bars:** Networked `redTeamHealth` / `blueTeamHealth` as normalized `NetworkVariable<float>`, bound to `Slider` UI.
- **Minion spawning:** `MinionSpawnRequester` periodically spawns minions that pathfind to the enemy base.
- **Player assignment:** Alternates team assignment via `nextTeamToAssignTo ^= nextTeamToAssignTo` (note: this XORs the enum with itself → always 0, meaning all players are assigned to `Red`).
- **Object registration:** On spawn, finds all `GameModeRelatedObject`-tagged GameObjects.

#### `GameModeCaptureObjective` — Free-for-all Capture Point (Legacy)

An earlier, more generic capture-point implementation. Supports any number of players in the area simultaneously tracking their timers. Uses `pendingAdd`/`pendingRemove` pattern to avoid `Collection Modified` exceptions.

### 3.8 Minion System

| Class | Description |
|---|---|
| `MinionSpawner` | Manages minion lifecycle — spawn/despawn from a tracked dictionary |
| `MinionSpawnRequester` | Server-only timer-based spawner. Initialized by `BrawlGameMode` with team assignment |
| `Minion` | Simple `NetworkBehaviour` with `NavMeshAgent` — sets destination to enemy base on start |

**Flow:**
```
BrawlGameMode.RegisterMinionSpawner(spawner)
  → spawner.Initialize(this)  // gives the spawner a reference to the game mode
  → spawner.Update() (server only)
    → Timer ticks, instantiates Minion prefab
    → Sets minion.target = enemy base transform
    → Calls spawner.SpawnMinion(minion)
```

### 3.9 UI System

**Character Selection:**
- `CharacterList` — singleton holding `List<CharacterCardSO>`.
- `CharacterCardSO` — ScriptableObject linking a card image + `CharacterModelSO`.
- `CharacterModelSO` — ScriptableObject defining the character's model, skills, base stats.
- `CharacterSelectUIScript` — Dynamically generates card grid from `CharacterList`, handles selection.
- `CharacterCardScript` — UI card behaviour, raises `onSelected` event.

**Lobby:**
- `LobbyCanvasManager` — shows player cards, join code (host), start button (ready check), disconnect.
- `PlayerInLobbyIcon` — per-player lobby card.
- `startButton.interactable` — enabled when all players have chosen a character AND the current client is the host.

**In-Game:**
- `PersistentCanvasManger` — singleton HUD root. Holds crosshair, healthbar, character select references. `PlayerScript` sets `playerScript` on it during initialization.
- `InGameCanvasManager` — Escape menu (cursor lock toggle, leave server).
- `UISkillIcon` — Cooldown display per skill slot. Finds the matching `SkillBase` in children by `SkillCastType`.
- `SkillsUIManager` — Grid layout recalculation based on screen size.

**Menu:**
- `MenuScript` — Host/Client buttons with Relay toggle, join code input, scene transitions.

### 3.10 Utility Layer

| File | Description |
|---|---|
| `InputCollectorScript` | Collects WASD + Q/E/click input. Manages `StunTime` — zeroes all inputs while stunned. Exposes `BoolRefType` references for skill trigger binding |
| `Vector3Extenttion` | Extension method `ScaleVec(Vector3)` — component-wise multiplication |
| `DebugUtils` | `DrawBox(center, halfExtents, orientation, color)` — draws a debug wireframe box |
| `PropertyBlockMaterial` | Wrapper around `MaterialPropertyBlock` — applies shared property block to an array of renderers |
| `ClientAuthNetworkTransform` | One-liner: `NetworkTransform` with `OnIsServerAuthoritative() => false` |
| `ClientPredictionTypes` | `InputState` struct with `INetworkSerializable` — unused, prepared for client prediction |
| `DontDestroyOnLoad` | Utility behaviour — calls `DontDestroyOnLoad` in Awake |
| `SceneTransitioner` | Delayed scene transition + connection-lost fallback to start scene |
| `ParticleSystemGhostObject` | Smoothly follows a target transform with lerp — used for detached particles (client-side only, no sync cost) |
| `ExposeTriggerEventScript` | Exposes `OnTriggerEnter`/`Exit` as `Action<Collider>` events |
| `DespawnDestroyAfterDelay` | Destroys/despawns after a delay |
| `PlayFromStartScene` | Boot script |

---

## 4. Data Flow Diagrams

### 4.1 Skill Execution Flow

```
Client (Owner)
  InputCollectorScript.Update()
    → reads input → sets BoolRefType.value = true
  SkillBase.Update()
    → checks spellTriggeringFlag.value && !isOnCooldown()
    → SkillBase.Use()                    [abstract]
        → animationScript.Trigger("WindUp")
        → combatManagerRef.SetStunTimer(windupTime)
        → Vector3 LookDir = getLookDirection()
        → ServerSideUseServerRPC(LookDir)

Server (Host)
  ServerSideUseServerRPC()
    → validates: IsServer, cooldown, etc.
    → setCooldown(cooldown)
    → executes: spawn entities, apply damage, etc.
    → ServerAnnounceSpellCastClientRPC()

All Clients (including Owner)
  ServerAnnounceSpellCastClientRPC()
    → animationScript.Trigger("SpellAcknowledge...")
    → VFX / audio / gameplay effects
```

### 4.2 Damage Flow

```
AutoAttackSkillCarrier.ServerSideUseServerRPC()
  → Physics.OverlapBox(...) → get hit players
  → SkillInstanceData data = { damage, ownerNetworkObjectId }
  → playerResources.damage(data)

PlayerResources.damage(SkillInstanceData)
  → gameMode.CanDamage(NetworkObjectId)   [permission check]
  → Hp.Value -= skillData.damage
  → damageEventBus.Raise(DamageTakenEvent)
  → if Hp <= 0: playerDeathEvent.Raise(PlayerDeathEvent)

Client-side (HP OnValueChanged)
  → Update HP slider
  → Play bleeding particles
  → Camera impulse (CinemachineImpulseSource)

TestGameModeManager (DeathEventHandler)
  → Respawn killed player
  → Heal to full
  → Reassign capture point to killer
```

### 4.3 Dependency Injection Wiring

```
DefaultLifeTimeScope.Configure()
  ├── builder.Register(typeof(EventBus<>), Lifetime.Singleton)
  ├── builder.RegisterComponentInHierarchy<PlayerObjectSpawner>()
  └── builder.RegisterComponentInHierarchy<TestGameModeManager>().As<IGameMode>()

PlayerObjectSpawner [Inject] IObjectResolver container
  → container.InjectGameObject(playerPrefab)  // resolves [Inject] on PlayerResources etc.

PlayerScript [Inject] IObjectResolver container
  → container.InjectGameObject(skillpref)  // resolves [Inject] on skill carriers

PlayerResources [Inject]
  ├── IGameMode? gameMode
  ├── EventBus<DamageTakenEvent> damageEventBus
  └── EventBus<PlayerDeathEvent> playerDeathEvent

SkillBase subclasses [Inject]
  ├── EventBus<DamageTakenEvent> (YangBlastSkill)
  ├── EventBus<GlobalStatsChangeEvent> (AutoAttackSkillCarrier)
  └── etc.
```

---

## 5. Design Observations & Technical Debt

### 5.1 Two Coexisting Skill Systems

The **NewSkillSystem** (`SkillBase`, carrier prefabs) and **Legacy SkillClasses** (`BaseSkill`, `BaseSkillEntityBehavior`) coexist. `TestSkillNewSystem` bridges both — it's a `SkillBase` carrier that spawns a `BaseSkillEntityBehavior`. The legacy `EntitySpawningSkill` is entirely unimplemented.

### 5.2 Known Bugs & Issues

| Issue | File/Line | Impact |
|---|---|---|
| `nextTeamToAssignTo ^= nextTeamToAssignTo` XORs enum with itself → always `Red` | `BrawlGameMode.cs:127` | All players assigned to Red team |
| `LocalPlayerStateManager` null check inconsistency | `UISkillIcon.cs:20-28` | Fragile init ordering, event-based fallback suggests race conditions |
| Stun accumulates without cap on the server | `PlayerCombatManager.cs:36` | `SetStunTimer(windupTime)` called per-use, but no cap — stacking skills can produce long stuns |
| `BoolRefType` class wrapper for input | `SkillBase.cs:14-16` | A memory allocation per input axis; an event/callback pattern would be cleaner |
| `"Unrgister"` typo | `EventBus.cs:19` | Misspelled method name propagated across all usages |
| `PlayerMoveWithPrediction` stub | Full file | Disabled `NetworkTransform`, empty buffers — never completed |
| `SkillManagerScript` is a static list singleton | `SkillManagerScript.cs:13` | Legacy system, `testSkill` logs "SCRAP THIS SYSTEM" |
| `PersistentCanvasManger` singleton | `PersistentCanvasManger.cs` | Singleton pattern with no DontDestroyOnLoad — destroyed on scene transitions |
| Commented-out code throughout | Multiple files | Large blocks of disabled code (e.g., `RubyFormSkill.cs:77-96`) should be removed |
| `SkillDataSO` instantiated as fallback | `SkillBase.cs:124` | `ScriptableObject.CreateInstance<SkillDataSO>()` in runtime code — ScriptableObjects are assets, not runtime data objects |
| Non-FightArea uses tags | `PlayerCombatManager.cs:66-68` | Tag-based detection is fragile; layer-based collision filtering is standard |
| `InputCollector.rightClick` used for aiming toggle + as a `BoolRefType` | `InputCollectorScript.cs:28-31` | Right-click both switches camera style (in `Movement.Update()`) and has a `BoolRefType` — may cause conflicts |

### 5.3 Architectural Observations

- **No object pooling:** Skills and projectiles are `Instantiate`/`Destroy` — no pooling (could be a problem at scale).
- **All skills poll in Update:** Every `SkillBase` subclass runs its own `Update()` loop polling the flag — no central skill manager.
- **DI is partial:** VContainer is used for the EventBus and for injecting game modes, but most components use `GetComponent<T>()` or direct field assignment.
- **No unit tests or test framework** visible in the project.
- **Minion base damage:** `TeamBaseBrawlGameMode.Update()` directly subtracts `minionsInRange * Time.deltaTime` — no damage event raised, no game mode permission check.
- **Network serialization in two styles:** `AutoAttackParams` uses manual `FastBufferReader`/`FastBufferWriter`, while `Stats` uses `serializer.SerializeValue` — the latter is cleaner and recommended.

---

## 6. Refactoring Suggestions

### 6.1 High Priority

1. **Fix the Brawl team assignment bug** — `nextTeamToAssignTo ^= nextTeamToAssignTo` always evaluates to `0` (Red). It should use a ternary flip or a toggle method.

2. **Remove the legacy skill system** — Delete `Assets/Scripts/Skills/SkillClasses/` (except anything still referenced by `TestSkillNewSystem`, which can be migrated). This removes confusion and dead code.

3. **Unify stat effect application** — Implement the commented-out `IGameplayEffect` pipeline pattern in `StatModifierHolder.cs`. This gives skills a standard `OnStart`/`OnUpdate`/`OnFinish` lifecycle instead of ad-hoc coroutines and direct stat manipulation.

4. **Clean up commented-out code** — Remove dead code blocks throughout (particularly `RubyFormSkill`, `AutoAttackSkillCarrier`, `BlinkSkill`).

### 6.2 Medium Priority

5. **Convert `SkillDataSO` to a proper runtime `SkillConfig`** — Use ScriptableObjects for static configuration only, don't mutate them at runtime. Pass skill data via `NetworkVariable` or RPC parameters instead.

6. **Add an object pool for projectiles** — Skills like Root, TestQSkill, and YangBlast all `Instantiate`/`Destroy`. A simple `NetworkObjectPool` (NGO provides one via `NetworkObjectPool` in newer versions) would reduce GC pressure.

7. **Replace `BoolRefType` with events** — Instead of a polling `BoolRefType`, the `InputCollectorScript` should invoke callbacks or set a flag on a central component that skills register with.

8. **Move skill polling to a central `SkillManager`** — Instead of each `SkillBase.Update()` polling independently, have `PlayerSkillHolder` or a dedicated component manage a `List<SkillBase>` and dispatch input events.

9. **Make `PersistentCanvasManger` properly persistent** — Add `DontDestroyOnLoad` and ensure it only exists once across scene loads.

### 6.3 Lower Priority

10. **Implement client-side prediction properly** — Either finish `PlayerMoveWithPrediction` or remove it to avoid confusion about the movement authority model.

11. **Replace tag-based checks with layers** — `"Player"`, `"NonFightArea"`, `"Terrain"` tags should become Physics Layers in the project settings for performance and correctness.

12. **Add a proper `NetworkObjectPool`** — Pre-warm pools for frequently spawned objects (projectiles, minions, VFX ghost objects).

13. **Standardise network serialisation patterns** — Use `serializer.SerializeValue` throughout rather than manual `FastBufferReader`/`FastBufferWriter`.

14. **Add unit/integration tests** — At minimum for: damage calculation, stat modification stacking, game mode win condition logic, cooldown behaviour.

15. **Move root-level scripts into the `Scripts/` folder** — ~20 C# files at `Assets/` root break the clean folder structure.

---

## 7. Getting Started for Contributors

### Prerequisites
- Unity 2022.3+ (HDRP pipeline)
- Netcode for GameObjects 1.x
- VContainer 1.x (DI framework)
- Cinemachine 2.x

### First-time setup
1. Clone the repository.
2. Open `RubyArenaProject/` in Unity Hub.
3. Let the project import and compile (HDRP shaders may take time).
4. Open `Scenes/MultiplayerStartScene` — this has the `MenuScript`.
5. Press Play as Host → Game loads into the selected game mode scene.

### How to add a new skill
1. Create a new `.cs` file in `Assets/Scripts/Skills/NewSkillSystem/` extending `SkillBase`.
2. Override `Use()`, `ServerSideUseServerRPC()`, and `ServerAnnounceSpellCastClientRPC()`.
3. Add the Update() polling loop (or register with a future central skill manager).
4. Create a prefab with the new script, assign `SkillCastType`, `SkillDataSO`, and basic params.
5. Assign the prefab to a `CharacterModelSO`'s SkillPrefab1 or SkillPrefab2 slot.

### How to add a new character
1. Create a `CharacterModelSO` ScriptableObject (right-click → ScriptableObjects → CharacterModelSO).
2. Assign the model prefab, skill prefabs, auto-attack params, and base stats.
3. Create a `CharacterCardSO` referencing the `CharacterModelSO`.
4. Add the `CharacterCardSO` to the `CharacterList` singleton in the lobby scene.

### How to add a new game mode
1. Implement `IGameMode` as a `NetworkBehaviour`.
2. Register it in a `LifetimeScope` (see `DefaultLifeTimeScope`).
3. The game mode manager should handle `RegisterPlayer`, win conditions, and respawn logic.
4. Tag game-mode-specific objects with `"GameModeRelatedObject"` for auto-discovery.

---

*Documentation generated from code analysis — August 2026.*