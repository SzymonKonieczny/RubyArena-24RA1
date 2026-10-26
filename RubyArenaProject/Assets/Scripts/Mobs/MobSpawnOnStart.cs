using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Convenience component: spawns a mob when the network object spawns.
/// Attach to a GameObject in the scene with a MobSpawner reference.
/// </summary>
public class MobSpawnOnStart : NetworkBehaviour
{
    [SerializeField] MobSpawner spawner;
    [SerializeField] MobConfigSO mobConfig;
    [SerializeField] Transform spawnPoint;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer) return;

        if (spawner == null)
        {
            spawner = FindObjectOfType<MobSpawner>();
        }

        if (spawner == null)
        {
            Debug.LogError($"{name}: No MobSpawner found in scene!");
            return;
        }

        if (mobConfig == null)
        {
            Debug.LogError($"{name}: No MobConfigSO assigned!");
            return;
        }

        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        spawner.SpawnMob(mobConfig, pos);
    }
}
