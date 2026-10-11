# New Character Skill — "Mirage" (Invisibility + Dash + Decoy)

## What the skill does
On use:
1. Character becomes **invisible** for **4 seconds**.
2. Character **dashes forward** a little.
3. A **decoy model** is left behind at the start position.
4. The decoy **explodes and damages nearby enemies** when it is **hit/damaged** by an enemy.

## Decisions locked in (from user answers)
- **Invisibility:** Disable the character's SkinnedMeshRenderer(s) for 4s (simplest, no shader dependency). Synced via ClientRPC. Enemies still need to be told the player is invisible — handled by the same ClientRPC so all clients hide the model. (If they want other clients to *not* see health/predicted position, that's a separate concern.)
- **Dash:** `Movement.startDash()` (the WeissDashSkill precedent). Dash direction follows current movement input, matching the Weiss dash.
- **Decoy trigger:** Explode when the decoy is **hit/damaged** — implemented by making the decoy a real damageable networked entity that uses the standard `DamageTakenEvent` flow.

## Files to create
1. `RubyArenaProject/Assets/Scripts/Skills/NewSkillSystem/MirageSkill.cs` — the skill carrier (extends `SkillBase`).
2. `RubyArenaProject/Assets/Scripts/Skills/NewSkillSystem/DecoyEntity.cs` — the decoy NetworkBehaviour (damageable, explodes on damage).
3. A new skill-carrier prefab under `Resources/Prefabs/NewSkillSystemCarriers/Mirage/` and a decoy prefab (set up in editor — see Hookup).
4. A `DecoyUnitResource` — a tiny `UnitResource` subclass so the standard `playerResources.damage(data)` call can damage the decoy (needed because the existing damage path terminates on `UnitResource`).

## Implementation notes (code pattern follows the existing skill system)

### `MirageSkill : SkillBase`
- **Update()**: same gate as all skills — `if (InputCollector == null || combatManagerRef == null || isOnCooldown() || !combatManagerRef.IsOwner) return; if (spellTriggeringFlag.value) Use();`
- **Use()**: play windup (`animationScript.PlayState/Trigger`), `combatManagerRef.SetStunTimer(windupTime)`, then `ServerSideUseServerRPC(new Vector3())` (no aim input needed, dash is input-direction based).
- **ServerSideUseServerRPC()**: guard `!IsServer` + cooldown → `setCooldown(cooldown)` → `StartCoroutine(Execute())`.
  - `Execute()` waits `windupTime`, then:
    - **Spawn the decoy** at the player's position: `Instantiate(decoyPrefab)`, get `NetworkObject`, spawn with the player's owner, parent to world at the current position. Pass the player's `NetworkObjectId` as the decoy's owner so the explosion's damage is credited to the right player.
    - `ServerAnnounceSpellCastClientRPC()`.
- **ServerAnnounceSpellCastClientRPC()** (runs on all clients, incl. owner):
  - Owner: play acknowledge animation + call `combatManagerRef.playerMove.startDash()` and set the dash look direction to the camera (so the dash is forward). Start an invisibility timer coroutine.
  - Non-owner: play windup/acknowledge.
- **Invisibility**: a shared coroutine that hides the model for 4s. Because the renderers live on the character model that *all* clients can see, hiding must happen on every client → triggered from `ServerAnnounceSpellCastClientRPC`. Uses `combatManagerRef.GetComponentsInChildren<SkinnedMeshRenderer>(true)` and toggles `.enabled`. Timer driven by `NetworkManager.ServerTime.Time` to stay in sync, or simply `yield return new WaitForSeconds(4)`.

### `DecoyEntity : NetworkBehaviour`
- Fields: `[SerializeField] DecoyUnitResource resource;` , `NetworkVariable<float> Hp` (or reuse the UnitResource), AOE radius, `DamageTakenEvent` subscription via VContainer `[Inject] EventBus<DamageTakenEvent>`.
- **OnNetworkSpawn()**: register `EventBinding<DamageTakenEvent>`; filter `e.recieverNetworkObjectId == NetworkObjectId`; when matched → server runs the explosion.
- **Explosion (server only)**: `Physics.OverlapSphere(transform.position, radius)`, collect `Player`-tagged `PlayerCombatManager`s except self/owner, call `playerResources.damage(new SkillInstanceData { damage = this.damage, ownerNetworkObjectId = ownerId })` on each. Then despawn the decoy (`.NetworkObject.Despawn()`) and play an explosion visual via a ClientRPC (or a local ParticleSystem).
- To be *hit by existing attacks*, the decoy:
  - carries a `DecoyUnitResource : UnitResource` that overrides `damage(SkillInstanceData)` (server only) to decrement `Hp` + raise `DamageTakenEvent` (so the decoy subscribes to damage normally).
  - is tagged so auto-attack OverlapBox / skillshot collisions can hit it. **Note tradeoff:** this is the honest "hit by attacks" path and reuses `DamageTakenEvent`. The decoy should be tagged `"Player"`? No — safer to add it so it can be a target without breaking the game-mode's player registration. This part is implemented so any attack path that calls `UnitResource.damage()` on the decoy will trigger it.

> **Simplification note:** Because `GameMode.CanDamage` returns `true` and damage flows through `PlayerResources`/`UnitResource`, the decoy is made a lightweight `UnitResource` so the *existing* attack code (`AutoAttackSkillCarrier`, `SkillInstanceData.damage`) can damage it and raise a `DamageTakenEvent` that the decoy listens for. That keeps "explode when attacked" honest and network-correct.

## Hookup in the Unity Editor

> **Why two prefabs?** They fill different roles.
> - **`Decoy.prefab`** is a *world entity*: a standalone networked object placed in the arena that enemies damage and that explodes. It is **not** parented to a player.
> - **`MirageSkill.prefab`** is the *skill carrier*: the component the player **equips** (it is parented under the player's `PlayerSkillHolder` by `PlayerScript.addSkillPrefab`, reads input/stats/cooldown via `SkillBase`, and *spawns* the decoy at cast time).
> This is the same split the existing skills use — `TestSkillNewSystem` (carrier) spawns `BlakeShot` (entity); `RootSkill` (carrier) spawns the `RootProjectileEntitiy`.
>
> In short: the **carrier** is what you assign to the character (`CharacterModelSO.SkillPrefab1/2`); the **decoy entity** is only referenced from the carrier's `decoyPrefab` field and is spawned mid-fight.

### 1. Create the `SkillDataSO`
- `Assets → Create → ScriptableObjects → SkillData`.
- Set `damage` (the decoy explosion damage), cast time/icon. Assign it to the carrier in step 3.

### 2. Create the `Decoy` prefab (the world entity)
- In the Project window: right-click → `Create → Empty`, name it `MirageDecoy`.
- **Add components:**
  - `NetworkObject`
  - `DecoyUnitResource` (script) — makes it damageable via the standard damage path
  - `DecoyEntity` (script) — subscribes to damage + explodes
- **Give it a visual:** add a child object cloned from the character model, or a placeholder Capsule/MeshRenderer (slightly transparent material reads better). Optional `Explosion VFX` (child particle system or a sprite).
- **Add a trigger Collider** (`Sphere Collider` with `Is Trigger = true`, or the character clone's existing collider). Note the decoy must be **hittable** — see the caveat at the bottom about the `"Player"` tag for melee to connect.
- **Fill the `DecoyEntity` inspector:**
  - `Explosion Radius` (e.g. 6)
  - `Lifetime` (fallback despawn, e.g. 8)
  - `Explosion VFX` (optional)
- Save it: drag into `Assets/Resources/Prefabs/NewSkillSystemCarriers/Mirage/ → MirageDecoy.prefab`.

### 3. Create the `MirageSkill` carrier prefab
- Right-click → `Create → Empty`, name it `MirageSkill`.
- **Add components:** `NetworkObject` + `MirageSkill` (script).
- **Fill the `MirageSkill` inspector** (from `SkillBase` + the skill):
  - `Cooldown` (e.g. 8)
  - `Windup Time` (the stun/cast delay before the decoy spawns, e.g. 0.3)
  - `Damage` (explosion damage; also override on the SkillDataSO)
  - `Invisibility Duration` = `4`
  - `Decoy Prefab` = the `MirageDecoy` prefab from step 2
  - `Skill Data SO` = the asset from step 1
  - `Cast Type` = `EClick` (E key) or `QClick` (Q key). `SkillBase.Init()` binds the trigger flag automatically from this.
  - `Ray Cast Mask` — leave to the character's default (skills use it for aim raycasts; Mirage ignores it, but it's a base field).
- Save as `Assets/Resources/Prefabs/NewSkillSystemCarriers/Mirage/MirageSkill.prefab`.

### 4. Register both prefabs for networking (mandatory)
- Open the `NetworkManager` in the scene → `General → Prefabs List` (or the project's `DefaultNetworkPrefabs.asset`).
- Add **both** `MirageSkill` and `MirageDecoy` to the list. Networked objects spawned at runtime (`SpawnWithOwnership`) will only appear on other clients if their prefab is registered here.

### 5. Assign the carrier to a character
- Open the character's `CharacterModelSO` asset (or `Create → ScriptableObjects → CharacterModelSO` for a new character).
- Set **`Skill Prefab 2`** (or `Skill Prefab 1`) = the `MirageSkill` prefab. These two slots are the character's active skills.
- Make the character selectable:
  - `Create → ScriptableObjects → CharacterCard` (`CharacterCardSO`): set `image`, `name`, `characterID`, `characterModel` = the `CharacterModelSO`.
  - Add that card to the scene's `CharacterList.Characters` list.
  - `PlayerScript` looks up `CharacterList.Instance.Characters[characterID]` when a player spawns and calls `addSkillPrefab(...)` on the slots — so `characterID` must be a valid index into that list.

### 6. (Verification) In-editor checks
- After importing, ensure `MirageSkill.cs`, `DecoyEntity.cs`, `DecoyUnitResource.cs` compile without errors (Console panel).
- Press Play as Host: press E → the player should turn invisible ~4s, dash, and a decoy should remain where they were. In two-client/Host+Client testing, hitting the decoy with an enemy attack should make it explode and damage the enemy, while the caster is not hurt.

### Caveat: making melee connect to the decoy
The decoy explodes whenever any attack calls `UnitResource.damage()` on it — works out of the box for spell/skillshot damage that targets a `UnitResource`. Melee auto-attack (`AutoAttackSkillCarrier`) additionally filters by `CompareTag("Player")` + `GetComponent<PlayerCombatManager>()`, so a bare decoy won't be hit by it. If you want **all** attack types to pop the decoy, tag the decoy root `"Player"` **and** add a `PlayerCombatManager`-compatible marker, or — simplest and recommended — also explode on a `"Player"`-tagged `OnTriggerEnter` (contact) alongside the damage event. Flag the latter on the `DecoyEntity` if desired.

## Design observations
- The invisibility is *visual only* (collider/model hidden). Netcode keeps the player fully functional. This matches the existing `RubyFormSkill`/`BlinkSkill` visual approach and is the simplest correct implementation. If "truly can't be seen on the minimap/healthbar" is needed, that's a follow-up (disable a network-synced visibility flag / hide the target indicator).
- The dash direction is input-relative (Weiss behavior). If a camera-relative "always dash toward crosshair" is preferred later, swap to `AddNetworkRbVelocityClientRPC(lookDir * strength)` (Yang pattern).
