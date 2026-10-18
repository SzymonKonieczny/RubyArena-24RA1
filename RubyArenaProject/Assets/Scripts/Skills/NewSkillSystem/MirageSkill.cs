using System.Collections;
using UnityEngine;
using Unity.Netcode;
using VContainer;
using VContainer.Unity;

/// <summary>
/// Mirage: on use the character turns invisible for a few seconds, dashes forward and
/// leaves behind a decoy model that explodes when it is attacked, damaging enemies nearby.
///
/// Network flow follows the standard skill pattern:
///   Use() (owner, input) -> ServerSideUseServerRPC (server executes + spawns decoy)
///        -> ServerAnnounceSpellCastClientRPC (every client: visuals, dash, invisibility)
/// </summary>
public class MirageSkill : SkillBase
{
    [Header("Mirage Settings")]
    [SerializeField] float invisibilityDuration = 4f;
    [SerializeField] GameObject decoyPrefab;

    // Resolver is injected (the carrier is DI-injected by PlayerScript.addSkillPrefab).
    // We use it to inject the decoy before spawning — plain Instantiate + Spawn on a
    // runtime entity does NOT run VContainer injection, so its [Inject] fields would
    // otherwise be null.
    [Inject]
    IObjectResolver container;

    private void OnTransformParentChanged()
    {
        Init();
    }

    private void Start()
    {
        Init();
    }

    private void Update()
    {
        if (InputCollector == null || combatManagerRef == null || isOnCooldown() || !combatManagerRef.IsOwner)
            return;

        if (spellTriggeringFlag.value)
        {
            Use();
        }
    }

    public override bool Use()
    {
        animationScript.PlayState("WindUp");
        combatManagerRef.SetStunTimer(windupTime);

        ServerSideUseServerRPC(new Vector3()); // dash is input-direction based, no aim input needed
        return true;
    }

    [ServerRpc]
    public override void ServerSideUseServerRPC(Vector3 lookDir, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        if (isOnCooldown()) return;
        setCooldown(cooldown);

        StartCoroutine(ExecuteServer());
    }

    IEnumerator ExecuteServer()
    {
        yield return new WaitForSeconds(windupTime);

        ulong ownerId = combatManagerRef.NetworkObjectId;

        // Spawn the decoy at the player's current position (the pre-dash spot),
        // owned by the same client so the explosion is credited to them.
        if (decoyPrefab != null)
        {
            GameObject decoyGO = Instantiate(decoyPrefab, combatManagerRef.PlayerMove.Model.position + new Vector3(0,-1,0), combatManagerRef.PlayerMove.Model.rotation);

            // Inject [Inject] fields on the decoy (e.g. its EventBus) BEFORE spawning:
            // OnNetworkSpawn runs during Spawn, so injection must happen first.
            container.InjectGameObject(decoyGO);

            DecoyEntity decoy = decoyGO.GetComponent<DecoyEntity>();
            decoy?.Setup(ownerId, damage);

            NetworkObject decoyNetwork = decoyGO.GetComponent<NetworkObject>();
            if (decoyNetwork != null)
            {
                decoyNetwork.SpawnWithOwnership(combatManagerRef.NetworkObject.OwnerClientId);
            }
        }

        ServerAnnounceSpellCastClientRPC();
    }

    [ClientRpc]
    public override void ServerAnnounceSpellCastClientRPC()
    {
        if (animationScript == null)
        {
            Init();
        }

        if (IsOwner)
        {
            // Dash forward and turn invisible on all clients (they can all see the model).
            combatManagerRef.PlayerMove.startDash();
            animationScript.Trigger("SpellAcknowledge1");
        }
        else
        {
            animationScript.Trigger("WindUp");
            animationScript.Trigger("SpellAcknowledge1");
        }

        StartCoroutine(SetInvisibleForDuration(invisibilityDuration));
    }

    /// <summary>
    /// Hides/shows the character model. Runs on every client (called from the ClientRPC),
    /// because both the owner and other players can see the model. The dash happens first
    /// so the decoy (already spawned by the server) is left behind.
    /// The decoy itself is a separate object and is untouched by this.
    /// </summary>
    IEnumerator SetInvisibleForDuration(float duration)
    {
        SkinnedMeshRenderer[] renderers = combatManagerRef.GetComponentsInChildren<SkinnedMeshRenderer>(true);

        foreach (SkinnedMeshRenderer r in renderers)
        {
            r.enabled = false;
        }

        yield return new WaitForSeconds(duration);

        if (combatManagerRef == null) yield break; // player could have been despawned

        foreach (SkinnedMeshRenderer r in renderers)
        {
            if (r != null) r.enabled = true;
        }
    }
}
