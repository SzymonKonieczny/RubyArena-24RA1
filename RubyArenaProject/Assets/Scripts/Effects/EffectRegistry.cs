using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Index of every GameplayEffectSO in the game, by wire id. Same singleton pattern as
/// CharacterList: one instance, assigned in the scene, found by everyone who needs it.
///
/// The wire id is the effect's index in <see cref="effects"/> — a ushort on the wire
/// instead of a string. Order must be identical on server and client (it is: same
/// prefab, same list), so entries can only be APPENDED, never reordered or removed.
/// </summary>
public class EffectRegistry : MonoBehaviour
{
    public static EffectRegistry Instance { get; private set; }

    [SerializeField] List<GameplayEffectSO> effects = new();

    Dictionary<ushort, GameplayEffectSO> byId;

    public IReadOnlyList<GameplayEffectSO> All => effects;

    // Clear the stale static when domain reload is disabled in the editor.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Instance = null;

    void Awake()
    {
        Instance = this;
        Rebuild();
    }

    void OnValidate() => Rebuild();

    void Rebuild()
    {
        byId = new Dictionary<ushort, GameplayEffectSO>();
        for (int i = 0; i < effects.Count; i++)
        {
            if (effects[i] == null) continue;
            effects[i].registryId = (ushort)i;
            byId[(ushort)i] = effects[i];
        }
    }

    /// <summary>Resolve a wire id to its definition. Returns null (and logs) on mismatch,
    /// which means a client is running an older build than the server.</summary>
    public GameplayEffectSO Get(ushort id)
    {
        if (byId != null && byId.TryGetValue(id, out var so)) return so;

        Debug.LogError($"EffectRegistry: no effect with registryId {id}. " +
                       "A client is likely running a different build than the server.");
        return null;
    }

    /// <summary>Reconstruct the client-side mirror of a snapshot. The SO supplies the
    /// subclass and its config; the snapshot supplies the per-instance state.</summary>
    public GameplayEffect FromSnapshot(EffectSnapshot snapshot)
    {
        GameplayEffectSO so = Get(snapshot.registryId);
        if (so == null) return null;

        GameplayEffect effect = so.CreateEffect();
        effect.Definition = so;
        effect.CopyFromSnapshot(snapshot);
        return effect;
    }
}
