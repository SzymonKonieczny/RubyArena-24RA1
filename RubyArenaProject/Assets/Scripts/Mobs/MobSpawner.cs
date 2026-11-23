using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using VContainer;
using VContainer.Unity;
using System.Collections;

/// <summary>
/// Server-authoritative mob spawner. Creates mobs with DI injection and tracks them for cleanup.
/// </summary>
public class MobSpawner : NetworkBehaviour
{
    [SerializeField] GameObject mobCorePrefab;

    Dictionary<ulong, NetworkObject> spawnedMobs = new();

    /// <summary>
    /// The spawner currently present in the scene, if any. Set on network spawn so other
    /// systems (e.g. game modes) can reach it without a serialized reference.
    /// </summary>
    public static MobSpawner Instance { get; private set; }

    public IReadOnlyDictionary<ulong, NetworkObject> SpawnedMobs => spawnedMobs;

    [Inject]
    IObjectResolver container;

    /// <summary>
    /// Invokes the callback with the scene's MobSpawner, or null if none exists yet.
    /// Used by systems that cannot rely on the spawner's spawn order.
    /// </summary>
    public static void FindSpawner(System.Action<MobSpawner> callback)
    {
        if (Instance != null)
        {
            callback(Instance);
            return;
        }

        Instance = FindObjectOfType<MobSpawner>();
        callback(Instance);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Instance = this;
        if (!IsServer) return;
    }

    public override void OnDestroy()
    {
        if (Instance == this) Instance = null;
        base.OnDestroy();
    }

    /// <summary>
    /// Spawns a mob at the given position with the specified configuration.
    /// </summary>
    public MobCore SpawnMob(MobConfigSO config, Vector3 position, Quaternion? rotation = null)
    {
        if (!IsServer)
        {
            Debug.LogWarning("SpawnMob called on client — only the server can spawn mobs.");
            return null;
        }

        if (mobCorePrefab == null)
        {
            Debug.LogError("MobSpawner has no mobCorePrefab assigned!");
            return null;
        }

        GameObject mobGO = Instantiate(mobCorePrefab, position, rotation ?? Quaternion.identity);

        //Also same issue as below, this happens only serverside. It in general isnt very bad, clients dont have to listen to events as much, but still it stinks
        // Inject dependencies before spawn (so OnNetworkSpawn has access to EventBus etc.)
        container?.InjectGameObject(mobGO);

        MobCore mobCore = mobGO.GetComponent<MobCore>();
        if (mobCore != null)
        {
            //TODO: BUG
            //currently only doing this on the serverside, which is bad coz it means that on client it calls its Start and NetworkSpawn methods on a null config.
            //This is not a super fast fix coz you would need to add a network wire to tell the client which config to spawn.
            //Its not super urgent as the gamemode is supposed to initially only be singleplayer, but it is something worth investing some thought into later. (if "later" ever comes xdd)
            mobCore.InitializeMob(config);
        }

        NetworkObject mobNO = mobGO.GetComponent<NetworkObject>();
        if (mobNO != null)
        {
            mobNO.Spawn();
            spawnedMobs.Add(mobNO.NetworkObjectId, mobNO);
        }

        return mobCore;
    }

    /// <summary>
    /// Despawns a specific mob.
    /// </summary>
    public void DespawnMob(MobCore mob, float delay = 0)
    {
        if (!IsServer || mob == null) return;

        NetworkObject mobNO = mob.GetComponent<NetworkObject>();
        if (mobNO != null && mobNO.IsSpawned)
        {
            spawnedMobs.Remove(mobNO.NetworkObjectId);
            if (delay > 0)
            {
                StartCoroutine(DespawnWithDelay(mobNO, delay));
            }
            else mobNO.Despawn();
        }
    }

    IEnumerator DespawnWithDelay(NetworkObject mobNO, float delay)
    {
        yield return new WaitForSeconds(delay);
        mobNO.Despawn();
    }
    /// <summary>
    /// Despawns all tracked mobs.
    /// </summary>
    public void DespawnAllMobs()
    {
        if (!IsServer) return;

        foreach (var pair in spawnedMobs)
        {
            if (pair.Value != null && pair.Value.IsSpawned)
            {
                pair.Value.Despawn();
            }
        }
        spawnedMobs.Clear();
    }

    public override void OnNetworkDespawn()
    {
        DespawnAllMobs();
        base.OnNetworkDespawn();
    }
}
