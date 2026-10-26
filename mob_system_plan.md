# Mob System Plan for RubyArena

## Locked Design Decisions

| Question | Your Answer | Implication |
|----------|-------------|-------------|
| Death behavior | **B** — Death animation, then despawn | `MobUnitResource` triggers death anim, waits `deathDespawnDelay`, then `NetworkObject.Despawn()` |
| Mob attacks/config | **Config-driven** like player character selection | `MobConfigSO` defines model, stats, attacks, skills, AI behavior |
| AI disposition | **Passive / Neutral / Aggressive** (Minecraft-style) | `MobDisposition` enum + `MobAI` state machine |
| Existing minions | **New system alongside** | Keep Brawl minions as-is; build separate `Mob` system |
| Scope | **Plan only** for now | No code yet |

---

## Core Philosophy: Mirror the Player System

Just as a player has:
> `PlayerScript` (runtime core) + `CharacterModelSO` (config) → model, stats, skills, auto-attack

A mob will have:
> `MobCore` (runtime core) + `MobConfigSO` (config) → model, stats, attacks, AI behavior

The player system uses `PlayerSkillHolder` as the "glue" component that skills reference. Mobs will use `MobSkillHolder` — same surface, different source (AI instead of input).

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                      MobConfigSO (Asset)                     │
│  ├─ modelPrefab, baseStats                                  │
│  ├─ basicAttack: damage, range, cooldown, type              │
│  ├─ optionalSkills: List<SkillBase prefabs>                 │
│  ├─ disposition: Passive | Neutral | Aggressive             │
│  ├─ aggroRange, wanderRadius, deathDespawnDelay             │
│  └─ deathVFX, animationTriggers...                          │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│  MobCore (NetworkBehaviour) — spawned server-side            │
│  ├─ MobCombatEntity : CombatEntity                          │
│  ├─ MobUnitResource : UnitResource                          │
│  ├─ EntityStats (networked stats)                           │
│  ├─ MobSkillHolder (glue for skills/attacks)                │
│  ├─ MobAnimationScript (animator wrapper)                   │
│  ├─ MobMovement (NavMeshAgent wrapper)                      │
│  └─ MobAI (the brain — state machine)                       │
└─────────────────────────────────────────────────────────────┘
```

---

## New Types (Detailed)

### 1. MobConfigSO

```csharp
[CreateAssetMenu(fileName = "MobConfig", menuName = "ScriptableObjects/MobConfig")]
public class MobConfigSO : ScriptableObject
{
    public string mobName;
    public GameObject modelPrefab;
    public Stats baseStats;

    [Header("Basic Attack")]
    public int attackDamage = 10;
    public float attackRange = 2f;
    public float attackCooldown = 1.5f;
    public MobAttackType attackType; // Melee, RangedProjectile, AOE

    [Header("AI Behavior")]
    public MobDisposition disposition;
    public float aggroRange = 10f;
    public float wanderRadius = 5f;
    public float attackWindup = 0.3f;

    [Header("Death")]
    public float deathDespawnDelay = 2f;
    public GameObject deathVFX;
    public string deathAnimationTrigger = "Death";
}
```

### 2. MobDisposition (enum)

| Value | Behavior |
|-------|----------|
| `Passive` | Never seeks targets. Only fights back if attacked. |
| `Neutral` | No target until damaged. Then retaliates against attacker only. |
| `Aggressive` | Periodic scan for targets in `aggroRange`. Attacks closest valid target. |

### 3. MobCore : NetworkBehaviour

Runtime core. Analogous to `PlayerScript`:
- `OnNetworkSpawn()` → instantiate model from config, set up all sub-components
- `InitializeMob(MobConfigSO config)` → called by spawner before spawn
- Tracks `spawnedObjects` (skills, like `PlayerScript` does)

### 4. MobCombatEntity : CombatEntity

- `CanCombat` → controlled by `MobAI` state
- `AnimationScript` → returns `MobAnimationScript`
- `PlayerMove` → returns `MobMovement` (NavMesh-based)
- `SetStunTimer(float time)` → freezes `MobAI` into `Stunned` state

### 5. MobUnitResource : UnitResource

- `damage(SkillInstanceData)` → server-side HP reduction, raises `DamageTakenEvent`
- On `Hp <= 0` → raises `EntityDeathEvent` (see Phase 0), notifies `MobAI`
- **No UI bindings** — no sliders, no camera shake, no bleeding particles
- Death handling: triggers death animation, starts despawn timer

### 6. MobAnimationScript

Minimal animator wrapper compatible with `PlayerAnimationScript`'s interface:

```csharp
public void PlayState(string name) => animator?.Play(name);
public void Trigger(string name) => animator?.SetTrigger(name);
public void SetFloat(string name, float v) => animator?.SetFloat(name, v);
```

Skills and attacks can use either player or mob animation scripts interchangeably.

### 7. MobMovement : NetworkBehaviour

- Wraps `NavMeshAgent`
- Server-authoritative destination setting
- Methods: `SetDestination(Vector3)`, `Stop()`, `IsAtDestination()`
- Position synced via `NetworkTransform` component

### 8. MobAI : NetworkBehaviour — The Brain

**State Machine:**

```csharp
enum MobAIState { Idle, Chase, Attack, Stunned, Dead }
```

| State | Behavior |
|-------|----------|
| `Idle` | Wander within `wanderRadius` or stand still. Periodic target scan if Aggressive. |
| `Chase` | `NavMeshAgent.SetDestination(target.position)`. Transition to Attack when in range. |
| `Attack` | Stop movement. Face target. Windup → hit detection → damage → cooldown → re-evaluate. |
| `Stunned` | Freeze. `NavMeshAgent` disabled. Auto-transitions when stun timer expires. |
| `Dead` | Play death animation. Disable colliders/agent. Wait for despawn. |

**Target Selection** (based on `MobDisposition`):
- `Passive`: no scan. Target only set when `OnDamaged` event fires.
- `Neutral`: no scan. Target set on first damage taken. Retaliates only.
- `Aggressive`: `OverlapSphere(aggroRange)` every scan interval. Picks closest `CombatEntity` not on same team.

### 9. MobSkillHolder : MonoBehaviour

The "glue" component, analogous to `PlayerSkillHolder`:

```csharp
public class MobSkillHolder : MonoBehaviour
{
    public MobCombatEntity mobCombatManager;
    public MobAnimationScript animationScript;
    public EntityStats Stats;
    public StatModifierHolder statModifierHolder;
    // No InputCollector — AI drives everything
}
```

### 10. MobSpawner : NetworkBehaviour

- `SpawnMob(MobConfigSO config, Vector3 position, ulong? ownerClientId = null)`
- DI injection: `container.InjectGameObject(mobGO)` before `NetworkObject.Spawn()`
- Server-only spawning authority
- Dictionary tracking for cleanup
- Optional wave/timer-based spawning API

---

## Mob Attack System (How Mobs Deal Damage)

Mobs use a **simpler attack system than player skills** — AI-driven, no input-based `Update()` loops.

**Execution flow in `MobAI.Attack` state:**

```
1. Face target
2. Play windup animation (wait attackWindup seconds)
3. Hit detection:
   - Melee: OverlapSphere(attackRange) → find CombatEntity targets
   - Ranged: Instantiate projectile → it handles collision/damage like RootProjectileEntity
   - AOE: OverlapSphere + immediate damage to all in range
4. Apply damage: target.GetComponent<UnitResource>().damage(skillInstanceData)
5. Play attack animation trigger
6. Wait attackCooldown
7. Re-evaluate: target still in range? → Attack. Target out of range? → Chase. No target? → Idle.
```

This is intentionally simpler than `SkillBase` — mobs don't need windup input validation, camera aiming, or complex ClientRpc animation syncing (the mob model is visible to all clients already).

---

## Player Skills → Mobs (How Existing Skills Hit Mobs)

**The Problem:** All current skills filter targets with `CompareTag("Player")`.

**The Fix:** Replace tag checks with `CombatEntity` component checks.

| File | Current Filter | New Filter |
|------|---------------|------------|
| `YangBlastSkill.cs:180` | `CompareTag("Player")` | `GetComponent<CombatEntity>() != null` |
| `RootProjectileEntitiy.cs:79` | `CompareTag("Player")` | `GetComponent<CombatEntity>() != null` |
| Auto-attack (melee) | `CompareTag("Player")` + `GetComponent<PlayerCombatManager>()` | `GetComponent<CombatEntity>() != null` + `GetComponent<UnitResource>() != null` |

Mobs are tagged `"Mob"`, not `"Player"`. The `CombatEntity` check is the robust, semantically correct approach.

---

## Lifecycle: Spawn → Combat → Death → Despawn

```
Spawn (server)
    │
    ▼
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   Idle      │────▶│   Chase     │────▶│   Attack    │
│  (wander)   │     │ (NavMesh)   │     │ (deal dmg)  │
└─────────────┘     └─────────────┘     └──────┬──────┘
    ▲                                            │
    └────────────────────────────────────────────┘ (cooldown loop)
                           │
                    (takes damage / stun)
                           │
                           ▼
                    ┌─────────────┐
                    │   Stunned   │────▶ returns to previous state
                    │  (frozen)   │
                    └─────────────┘
                           │
                    (HP <= 0)
                           ▼
                    ┌─────────────┐
                    │    Dead     │
                    │ (death anim)│────▶ wait deathDespawnDelay
                    └─────────────┘      ────▶ NetworkObject.Despawn()
```

---

## Files to Create

| File | Path |
|------|------|
| `MobConfigSO.cs` | `Assets/Scripts/Mobs/` |
| `MobCore.cs` | `Assets/Scripts/Mobs/` |
| `MobCombatEntity.cs` | `Assets/Scripts/Mobs/` |
| `MobUnitResource.cs` | `Assets/Scripts/Mobs/` |
| `MobAnimationScript.cs` | `Assets/Scripts/Mobs/` |
| `MobMovement.cs` | `Assets/Scripts/Mobs/` |
| `MobAI.cs` | `Assets/Scripts/Mobs/` |
| `MobSkillHolder.cs` | `Assets/Scripts/Mobs/` |
| `MobSpawner.cs` | `Assets/Scripts/Mobs/` |

---

## Files to Modify

| File | Change |
|------|--------|
| `PlayerDeathEvent.cs` | **Rename** → `EntityDeathEvent.cs`. Update field names (`playerKillingNetworkId` → `killerNetworkObjectId`, etc.) |
| `PlayerResources.cs` | Raise `EntityDeathEvent` instead of `PlayerDeathEvent` |
| `TestGameModeManager.cs` | Update event binding type; `OnEntityDeath` handles both player and mob deaths |
| `RiposteSkill.cs` | No change needed (listens to `DamageTakenEvent`, not death) |
| `YangBlastSkill.cs` | Replace `CompareTag("Player")` with `CombatEntity` check |
| `RootProjectileEntitiy.cs` | Replace `CompareTag("Player")` with `CombatEntity` check |
| `AutoAttackSkill.cs` | Replace tag check with `CombatEntity` + `UnitResource` check |
| `DefaultLifeTimeScope.cs` | Register `MobSpawner` for DI |
| `IGameMode.cs` | Optional: add `CanDamage(CombatEntity attacker, CombatEntity victim)` overload |

---

## Networking Notes

- **Spawn authority:** Server only (`MobSpawner` guards with `if (!IsServer) return`)
- **DI injection:** Same pattern as `PlayerObjectSpawner` — `container.InjectGameObject(mobGO)` before `NetworkObject.Spawn()`
- **Position sync:** `NetworkTransform` component on `MobCore` prefab
- **Stat sync:** `EntityStats.basicStats` / `modifiedStats` are already `NetworkVariable<Stats>`
- **HP sync:** `MobUnitResource.Hp` is `NetworkVariable<float>` — auto-synced by Netcode
- **Animation:** `MobAnimationScript` runs on all clients; server triggers animations via `ClientRpc` or by syncing state variables that drive Animator parameters

---

## Implementation Phases

### Phase 0: Cleanup (Prerequisite)

- Rename `PlayerDeathEvent` → `EntityDeathEvent`
- Update all listeners (`TestGameModeManager`, `PlayerResources`)
- Update skill targeting: `CompareTag("Player")` → `CombatEntity` check (YangBlast, RootProjectile, auto-attack)

### Phase 1: MVP — "Punching Bag Mob"

- Create `MobConfigSO`, `MobCore`, `MobCombatEntity`, `MobUnitResource`, `MobAnimationScript`, `MobMovement`
- Create a test mob prefab (e.g., "Training Dummy")
- Create `MobSpawner`, spawn one dummy in `TestGameMode`
- Verify: player can damage dummy → dummy dies → raises `EntityDeathEvent` → despawns

### Phase 2: AI — "Aggressive Mob"

- Add `MobAI` with `Aggressive` disposition
- Mob chases and melee-attacks nearest player
- Verify: mob spawns → walks to player → attacks on cooldown → player takes damage

### Phase 3: Dispositions — "Passive & Neutral"

- Implement `Passive` and `Neutral` disposition logic
- Verify: Passive mob ignores player until hit. Neutral mob retaliates only.

### Phase 4: Death Polish

- Death animation + VFX + delayed despawn
- Verify: mob plays "Death" trigger → collapses → poof VFX → gone after 2s

### Phase 5: Config Variety

- Create multiple `MobConfigSO` assets (e.g., fast weak mob, slow tanky mob, ranged mob)
- Verify different stats, attack ranges, dispositions all work

### Phase 6: Integration (Optional Future)

- Brawl mode can optionally use mobs instead of dummy minions
- New gamemodes (PvE waves, boss fights) become possible

---

## Unity Editor Hookup (Future Implementation Notes)

1. **Create `MobConfigSO` assets** via `Assets → Create → ScriptableObjects → MobConfig`
2. **Create mob prefab** with `NetworkObject`, `MobCore`, `MobCombatEntity`, `MobUnitResource`, `EntityStats`, `MobSkillHolder`, `MobAnimationScript`, `MobMovement`, `MobAI`, `NetworkTransform`
3. **Model setup:** Child object with model prefab, Animator, Collider
4. **Tag the mob root:** `"Mob"` (not `"Player"`)
5. **Register prefab** in `NetworkManager` prefab list
6. **Place `MobSpawner`** in scene (or register in `DefaultLifeTimeScope`)
7. **Test:** Spawn mob → verify it appears on all clients → verify damage/death flow

---

## Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| `SkillBase` skills won't work on mobs (expects `PlayerSkillHolder`) | **Accepted.** Mobs use simpler `MobAI`-driven attacks. If boss skills needed later, extract `ISkillCaster` interface. |
| NavMeshAgent + `NetworkTransform` conflict | Use `NetworkTransform` with interpolation; `MobMovement` sets agent destination, position sync is visual-only |
| Existing minion system collision | New `Mob` system is separate. Brawl minions untouched. No collision. |
| Death event rename breaks gamemode scoring | Phase 0 updates all listeners atomically. Test in `TestGameMode` before Brawl. |
