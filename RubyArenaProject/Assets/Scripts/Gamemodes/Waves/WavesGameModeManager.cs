using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Events;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using VContainer;

/// <summary>
/// Vampire-Survivors style wave survival game mode.
/// The server spawns a wave of random mobs; when every mob in the wave is dead the next
/// wave spawns with more mobs. If every player is dead at the same time, the run is lost.
/// </summary>
public class WavesGameModeManager : NetworkBehaviour, IGameMode
{
    [Header("Waves")]
    [SerializeField] List<MobConfigSO> mobConfigs = new();
    [SerializeField] int baseMobsPerWave = 3;
    [SerializeField] int mobsAddedPerWave = 2;
    [SerializeField] float timeBeforeFirstWave = 3f;
    [SerializeField] float timeBetweenWaves = 5f;
    [SerializeField] List<Transform> mobSpawnPoints = new();
    [Tooltip("Radius used to spread mobs around a spawn point so they don't all stack on one spot.")]
    [SerializeField] float mobSpawnScatterRadius = 3f;

    [Header("Players")]
    [SerializeField] Transform playerSpawn;

    [Header("UI")]
    [Tooltip("Shows the wave number. Falls back to scoreDisplay if left empty.")]
    [SerializeField] TMP_Text waveDisplay;
    [Tooltip("Shows wave info and end-of-run messages.")]
    [SerializeField] TMP_Text scoreDisplay;
    [SerializeField] float returnToLobbyDelay = 5f;

    /// <summary>
    /// Mirrors the server's wave number so clients can display it.
    /// </summary>
    readonly NetworkVariable<int> currentWave = new(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    MobSpawner mobSpawner;

    /// <summary>
    /// Mob network ids spawned by the current wave, removed as they die.
    /// </summary>
    readonly HashSet<ulong> aliveMobIds = new();

    readonly HashSet<ulong> alivePlayerIds = new();

    Coroutine waveRoutine;
    bool runEnded;

    [Inject]
    EventBus<EntityDeathEvent> entityDeathEvent;

    EventBinding<EntityDeathEvent> deathEventBinding;

    // ---------------- IGameMode ----------------

    public bool CanDamage(ulong networkId) => true;

    public void RegisterPlayer(ulong networkId)
    {
        if (!IsServer) return;

        alivePlayerIds.Add(networkId);

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkId, out NetworkObject playerNO))
        {
            MoveToPlayerSpawn(playerNO);
        }

        RefreshScoreDisplay();
    }

    public void RegisterNetworkedObject(ulong networkId)
    {
        // Mob registration is handled internally by MobSpawner; nothing to do here.
    }

    // ---------------- Lifecycle ----------------

    void Start()
    {
        // So effects (DoTs on mobs/players) can run the gamemode's damage restriction.
        GameModeProvider.Register(this);

        MobSpawner.FindSpawner((spawner) => mobSpawner = spawner);
        currentWave.OnValueChanged += (_, __) => RefreshScoreDisplay();

        if (!IsServer)
        {
            RefreshScoreDisplay();
            return;
        }

        deathEventBinding = new EventBinding<EntityDeathEvent>(OnEntityDeath);
        entityDeathEvent.Register(deathEventBinding);

        RefreshScoreDisplay();

        if (mobConfigs.Count == 0 || mobConfigs.Contains(null))
        {
            Debug.LogError($"{name}: {nameof(mobConfigs)} must contain at least one MobConfigSO and no empty entries.");
            return;
        }
        if (mobSpawnPoints.Count == 0)
        {
            Debug.LogError($"{name}: no {nameof(mobSpawnPoints)} assigned - mobs have nowhere to spawn.");
            return;
        }

        waveRoutine = StartCoroutine(WaveLoop());
    }

    public override void OnDestroy()
    {
        if (IsServer && entityDeathEvent != null && deathEventBinding != null)
        {
            entityDeathEvent.Unregister(deathEventBinding);
            deathEventBinding = null;
        }

        base.OnDestroy();
    }

    // ---------------- Wave loop ----------------

    IEnumerator WaveLoop()
    {
        yield return new WaitForSeconds(timeBeforeFirstWave);

        while (!runEnded)
        {
            int wave = currentWave.Value + 1;
            currentWave.Value = wave;

            int mobCount = baseMobsPerWave + (wave - 1) * mobsAddedPerWave;
            SpawnWave(mobCount);

            // Wait until this wave is fully cleared (or the run ends).
            while (!runEnded && aliveMobIds.Count > 0)
            {
                yield return null;
            }

            if (runEnded) yield break;

            yield return new WaitForSeconds(timeBetweenWaves);
        }
    }

    void SpawnWave(int mobCount)
    {
        if (mobSpawner == null)
        {
            Debug.LogError($"{name}: no MobSpawner in the scene - cannot spawn wave {currentWave.Value}.");
            return;
        }

        aliveMobIds.Clear();

        for (int i = 0; i < mobCount; i++)
        {
            MobConfigSO config = mobConfigs[Random.Range(0, mobConfigs.Count)];
            Transform spawnPoint = mobSpawnPoints[Random.Range(0, mobSpawnPoints.Count)];

            Vector2 scatter = Random.insideUnitCircle * mobSpawnScatterRadius;
            Vector3 position = spawnPoint.position + new Vector3(scatter.x, 0f, scatter.y);

            MobCore mob = mobSpawner.SpawnMob(config, position, Quaternion.identity);
            if (mob != null)
            {
                aliveMobIds.Add(mob.NetworkObjectId);
            }
        }

        Debug.Log($"{name}: wave {currentWave.Value} spawned with {aliveMobIds.Count} mobs.");
    }

    // ---------------- Death handling ----------------

    void OnEntityDeath(EntityDeathEvent eventData)
    {
        if (!IsServer || runEnded) return;

        ulong killedId = eventData.killedNetworkObjectId;
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(killedId, out NetworkObject killedNO))
        {
            if (killedNO.GetComponent<PlayerScript>() != null)
            {
                OnPlayerDied(killedId, killedNO);
                return;
            }
        }

        if (aliveMobIds.Remove(killedId) && aliveMobIds.Count == 0)
        {
            Debug.Log($"{name}: wave {currentWave.Value} cleared.");
            RefreshScoreDisplay();
        }
    }

    void OnPlayerDied(ulong networkId, NetworkObject playerNO)
    {
        alivePlayerIds.Remove(networkId);
        RefreshScoreDisplay();

        if (alivePlayerIds.Count > 0) return;

        // Nobody left standing - the run is over. If a player reconnects or respawns
        // before the scene change, keep the defeat.
        Debug.Log($"{name}: all players are dead on wave {currentWave.Value}.");
        EndRun(victory: false);
    }

    void EndRun(bool victory)
    {
        if (runEnded) return;
        runEnded = true;

        if (waveRoutine != null)
        {
            StopCoroutine(waveRoutine);
            waveRoutine = null;
        }

        mobSpawner?.DespawnAllMobs();
        aliveMobIds.Clear();

        if (victory)
        {
            AnnounceEndClientRPC($"Victory! You survived {currentWave.Value} waves.");
        }
        else
        {
            AnnounceEndClientRPC($"Defeat! You fell on wave {currentWave.Value}.");
        }

        RefreshScoreDisplay();
        StartCoroutine(ReturnToLobbyAfterDelay());
    }

    IEnumerator ReturnToLobbyAfterDelay()
    {
        yield return new WaitForSeconds(returnToLobbyDelay);
        NetworkManager.Singleton.SceneManager.LoadScene("MultiplayerLobby", LoadSceneMode.Single);
    }

    [ClientRpc]
    void AnnounceEndClientRPC(string message)
    {
        TMP_Text target = scoreDisplay != null ? scoreDisplay : waveDisplay;
        if (target != null)
        {
            target.text = message + "\nYou will return to the lobby shortly";
        }
    }

    // ---------------- Helpers ----------------

    void MoveToPlayerSpawn(NetworkObject playerNO)
    {
        if (playerSpawn == null) return;

        PlayerScript playerScript = playerNO.GetComponent<PlayerScript>();
        if (playerScript == null) return;

        playerScript.playerMove.RequestTeleportClientRPC(playerSpawn.position);

        if (playerScript.playerResources != null)
        {
            playerScript.playerResources.Hp.Value = playerScript.playerResources.getMaxHP();
        }
    }

    void RefreshScoreDisplay()
    {
        TMP_Text target = waveDisplay != null ? waveDisplay : scoreDisplay;
        if (target == null) return;

        int wave = currentWave.Value;
        string text = wave <= 0 ? "Wave: starting..." : $"Wave: {wave}";

        if (IsServer)
        {
            int remaining = aliveMobIds.Count;
            text += $"\nEnemies remaining: {remaining}";
            text += $"\nPlayers alive: {alivePlayerIds.Count}";
        }

        target.text = text;
    }
}
