using Unity.Netcode;
using VContainer;

/// <summary>
/// Provides a single static resolver for NetworkBehaviour self-injection.
///
/// Problem: VContainer's InjectGameObject() only runs on the server when a NetworkObject
/// is spawned via NGO. The replicated instances on clients never get their [Inject] fields
/// resolved, causing null references in OnNetworkSpawn() (e.g. EventBus registration).
///
/// Solution: Each active LifetimeScope registers its IObjectResolver here via SetResolver().
/// NetworkBehaviours call DIHelper.TryInject(this) in Start() to self-resolve from
/// whichever scope is currently alive in the scene. If it returns false, wait
/// in a coroutine until DIHelper.IsReady becomes true, then inject.
///
/// This is game-mode-agnostic: DefaultLifeTimeScope, WavesGameModeLifeTimeScope, or any
/// future scope all use the same helper.
/// </summary>
public static class DIHelper
{
    static IObjectResolver resolver;

    public static bool IsReady => resolver != null;

    /// <summary>
    /// Called from LifetimeScope.Awake() to register the active container.
    /// </summary>
    public static void SetResolver(IObjectResolver r)
    {
        resolver = r;
    }

    /// <summary>
    /// Injects a NetworkBehaviour on whatever peer it spawned on (server or client).
    /// Call from Start() on any NetworkBehaviour that has [Inject] fields.
    /// Safe to call even if injection already happened server-side via InjectGameObject.
    /// </summary>
    public static void Inject(NetworkBehaviour behaviour)
    {
        resolver?.Inject(behaviour);
    }

    /// <summary>
    /// Attempts injection immediately. Returns true if the resolver was ready.
    /// If false, caller should StartCoroutine a wait loop and retry each frame.
    /// </summary>
    public static bool TryInject(NetworkBehaviour behaviour)
    {
        if (resolver == null) return false;
        resolver.Inject(behaviour);
        return true;
    }
}
