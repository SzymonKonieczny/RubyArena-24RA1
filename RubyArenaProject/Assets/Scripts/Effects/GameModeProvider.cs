using UnityEngine;

/// <summary>
/// Access to the active IGameMode from things VContainer doesn't inject — mobs, decoys
/// and other runtime-spawned entities build their StatModifierHolder context directly.
///
/// Set by whichever gamemode manager is in the scene; the EffectContext uses it to run
/// the same CanDamage check direct damage already respects.
/// </summary>
public static class GameModeProvider
{
    public static IGameMode Current { get; private set; }

    /// <summary>True if a gamemode is present. Effects should treat a missing gamemode
    /// as "no restriction" (same as PlayerResources does).</summary>
    public static bool TryGet(out IGameMode gameMode)
    {
        gameMode = Current;
        return gameMode != null;
    }

    public static void Register(IGameMode gameMode) => Current = gameMode;

    public static void Unregister(IGameMode gameMode)
    {
        if (ReferenceEquals(Current, gameMode)) Current = null;
    }

    // Clear stale statics when domain reload is disabled in the editor.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Current = null;
}
