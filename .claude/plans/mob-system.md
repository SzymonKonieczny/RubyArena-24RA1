# Mob System Plan for RubyArena

## Current State Analysis

### Combat Architecture
- **`CombatEntity`** (`NetworkBehaviour`) — abstract base for all combat-capable entities. Already designed with mobs in mind (comment says "players, mobs, etc."). Provides `CanCombat`, `SkillshotSpawnPoint`, `AnimationScript`, `PlayerMove`, `SetStunTimer()`.
- **`UnitResource`** (`NetworkBehaviour`) — abstract base for HP/Mana. `PlayerResources` extends it with `damage(SkillInstanceData)`, death event raising, and UI bindings.
- **`PlayerCombatManager : CombatEntity`** — the player-specific implementation. Handles stun via `InputCollectorScript`, server-authoritative `CanCombat` via trigger zones.
- **Damage flow**: `SkillInstanceData` carries `damage`, `ownerNetworkObjectId`, `spellCarrierNetworkObjectId`. Damage is applied server-side via `UnitResource.damage()`. `PlayerResources` raises `DamageTakenEvent` on the `EventBus`, and `PlayerDeathEvent` when HP <= 0.
- **Skills**: `SkillBase` (carrier, not `NetworkBehaviour`... wait, it IS `NetworkBehaviour` — `SkillBase : NetworkBehaviour`) handles input → `ServerSideUseServerRPC` → `ServerAnnounceSpellCastClientRPC`. Skills reference `CombatEntity`/`PlayerSkillHolder` for stats and positioning.
- **Gamemodes**: `IGameMode.CanDamage(ulong networkId)` gates whether an entity can take damage. `TestGameModeManager` and `BrawlGameMode` implement this.

### Existing "Minion" Skeleton
- `Minion.cs` — bare `NetworkBehaviour` with `NavMeshAgent`, `target` Transform, `targetTeam`. No health, no combat, no spawning integration beyond `MinionSpawner`/`MinionSpawnRequester`.
- `MinionSpawner.cs` — server-side dictionary tracking spawned minions, can `KillAllMinions()`.
- `MinionSpawnRequester.cs` — spawns minions on a timer toward a team base. Used only in `BrawlGameMode`.

### Key Observations
1. `CombatEntity` is already mob-ready — it just needs a concrete non-player implementation.
2. `UnitResource` is already the damage sink — a mob just needs its own subclass.
3. The `DamageTakenEvent`/`PlayerDeathEvent` EventBus pattern is clean and mob-agnostic.
4. The current `PlayerDeathEvent` is **misnamed** — it fires on any `UnitResource` death, but listeners (`TestGameModeManager`, `RiposteSkill`) assume it's a player. This needs generalization.
5. Skills like `YangBlastSkill` use `GetEntitiesInCone()` and filter with `CompareTag("Player")` — mobs won't be hit unless this is broadened or mobs share the tag.
6. `PlayerSkillHolder` is a `MonoBehaviour` (not `NetworkBehaviour`) that caches `PlayerCombatManager`, `InputCollectorScript`, `animationScript`, `StatModifierHolder`, `EntityStats`. Mobs won't have most of these.
7. `EntityStats` holds `basicStats`/`modifiedStats` (`Stats` struct: speed, canFly, AttackDamage, AttackSpeed, AttackRange). This is useful for mobs too.

---

## Proposal: Mob System Architecture

### Goal
Introduce server-authoritative mobs (AI-controlled combat entities) that can:
- Take damage from player skills and auto-attacks
- Die and raise events
- Potentially deal damage back to players
- Integrate with existing gamemodes (e.g., Brawl minions become real threats)

### Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Mob base class | `MobCombatEntity : CombatEntity` | Reuses the existing abstraction; fills the gap CombatEntity was designed for |
| Mob resources | `MobUnitResource : UnitResource` | Mirrors `PlayerResources` but without player-specific UI (sliders, camera shake) |
| Mob stats | Reuse `EntityStats` component | `Stats` struct already has AttackDamage, AttackSpeed, AttackRange — perfect for mobs |
| Mob AI | Simple `MonoBehaviour` driver | Not a full GOAP/BehaviorTree yet. Start with: target acquisition, move-to, attack-on-cooldown |
| Mob spawning | Extend `MinionSpawner` or replace | Keep `MinionSpawner` as the spawn manager; mobs register with it |
| Tag for hit detection | Keep `"Player"` for players, add `"Mob"` for mobs, OR unify to `"CombatEntity"` | Skills need to hit both. See Approach A vs B below |
| Death event | Rename `PlayerDeathEvent` → `EntityDeathEvent` | Cleanest long-term fix. All death listeners update |

### Two Approaches for Skill Targeting

**Approach A: Minimal Tag Change (Recommended)**
- Mobs are tagged `"Mob"`.
- Update skill hit detection (`YangBlastSkill.GetEntitiesInCone`, auto-attack, etc.) to check for `CombatEntity` component instead of `CompareTag("Player")`.
- Pro: Clean separation, no tag collision. Con: Touch every skill's targeting code.

**Approach B: Shared `"Player"` Tag**
- Mobs also use `"Player"` tag.
- Pro: Zero skill code changes. Con: Semantically wrong, may break gamemode logic that iterates "all players".

> **Recommendation: Approach A** — the `CombatEntity` check is more robust and was the intended design.

---

## Files to Create / Modify

### New Files
1. **`RubyArenaProject/Assets/Scripts/Mobs/MobCombatEntity.cs`**
   - Extends `CombatEntity`
   - No `InputCollectorScript` — stun is a no-op or freezes AI
   - `AnimationScript` returns a simple `MobAnimationScript` (or null if no animator)
   - `PlayerMove` returns a `MobMovement` (NavMesh-based) or null

2. **`RubyArenaProject/Assets/Scripts/Mobs/MobUnitResource.cs`**
   - Extends `UnitResource`
   - `damage()` applies HP reduction, raises `DamageTakenEvent`
   - On death: raises `EntityDeathEvent` (new, replacing `PlayerDeathEvent`)
   - No UI bindings (no sliders, no camera shake)

3. **`RubyArenaProject/Assets/Scripts/Mobs/MobMovement.cs`**
   - Extends `NetworkBehaviour` (or just `MonoBehaviour` if server-only)
   - Wraps `NavMeshAgent`
   - Server-authoritative position sync (or rely on `NetworkTransform`)

4. **`RubyArenaProject/Assets/Scripts/Mobs/MobAI.cs`**
   - Simple state machine: `Idle → Chase → Attack → Dead`
   - Target acquisition: nearest `CombatEntity` with different team/owner
   - Attack: uses `EntityStats.modifiedStats.Value.AttackDamage` on cooldown

5. **`RubyArenaProject/Assets/Scripts/Mobs/MobSpawner.cs`**
   - Replaces or extends `MinionSpawner`
   - Spawns mobs with DI injection (like `PlayerObjectSpawner`)
   - Wave/timer-based spawning

### Modified Files
6. **`PlayerDeathEvent.cs`** → rename to `EntityDeathEvent.cs`, update fields (`killerNetworkId`, `killedNetworkId` — remove "player" from names)

7. **`PlayerResources.cs`** — update death event raise to use `EntityDeathEvent`

8. **`TestGameModeManager.cs`** — update event binding type, `OnPlayerDeath` → `OnEntityDeath`, handle mob deaths gracefully (don't try to teleport a mob)

9. **`RiposteSkill.cs`** — update `DamageTakenEvent` filtering if needed (already checks `recieverNetworkObjectId`)

10. **`YangBlastSkill.cs`** — replace `CompareTag("Player")` with `GetComponent<CombatEntity>() != null`

11. **`RootProjectileEntitiy.cs`** — replace `CompareTag("Player")` with `CombatEntity` check

12. **`DefaultLifeTimeScope.cs`** — register `MobSpawner` or ensure mobs get DI

13. **`IGameMode.cs`** — optionally add `CanDamage(CombatEntity attacker, CombatEntity victim)` for team-based filtering

---

## Open Questions for User

**Q1: What should mobs do when they die?**
- Option A: Despawn immediately (simplest)
- Option B: Play death animation, then despawn after delay
- Option C: Leave a corpse (physics ragdoll) — more complex

**Q2: Should mobs deal damage via melee auto-attack, skills, or both?**
- Option A: Simple melee (overlap sphere on timer, call `UnitResource.damage()`)
- Option B: Mobs can cast prefab skills (reuse `SkillBase` system — complex, mobs don't have `PlayerSkillHolder`)
- Option C: Mobs are damage-on-contact (walk into player = damage)

**Q3: Team alignment for mobs?**
- Option A: Mobs are neutral (attack closest entity, player or mob)
- Option B: Mobs belong to a team (e.g., Red minions vs Blue minions, like Brawl)
- Option C: Mobs are PvE (always attack players, never each other)

**Q4: Should the existing `Minion`/`MinionSpawner` be retrofitted or replaced?**
- Option A: Refactor `Minion` → `MobCombatEntity` + `MobUnitResource` + `MobAI` (keeps Brawl working)
- Option B: Keep minions as-is (visual-only lane fodder), create new `Mob` system alongside for "real" mobs

**Q5: Do mobs need to be targetable by all current skills, or only a subset?**
- If all: every skill's targeting/filtering needs the `CombatEntity` check (Yang blast, auto-attack, etc.)
- If subset: we can start with just AOE skills and expand later

---

## Recommended First Slice (MVP)

If you want to start small, I suggest:

1. **Create `MobCombatEntity` + `MobUnitResource`** — make a mob that can stand there and take damage.
2. **Update one skill** (e.g., `YangBlastSkill`) to hit `CombatEntity` instead of `"Player"` tag.
3. **Generalize `PlayerDeathEvent` → `EntityDeathEvent`** — update listeners.
4. **Spawn a test mob** in `TestGameMode` — verify damage/death flow works.
5. **Add `MobAI`** — basic chase + melee attack.
6. **Retrofit Brawl minions** — replace the dummy `Minion` with the new mob system.

This gives you a working mob in ~6 steps, each independently testable.
