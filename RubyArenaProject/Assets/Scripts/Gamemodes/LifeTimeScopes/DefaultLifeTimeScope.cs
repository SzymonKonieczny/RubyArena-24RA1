using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;
using VContainer;
using VContainer.Unity;

public class DefaultLifeTimeScope : LifetimeScope
{
    protected override void Awake()
    {
        base.Awake();
        Debug.LogWarning($"[{nameof(DefaultLifeTimeScope)}] AwakeCalled");

        DIHelper.SetResolver(Container); // Capture the container on boot
    }
    protected override void Configure(IContainerBuilder builder)
    {
        base.Configure(builder);
        builder.Register(typeof(EventBus<>), Lifetime.Singleton);
        builder.RegisterComponentInHierarchy<PlayerObjectSpawner>();
        builder.RegisterComponentInHierarchy<MobSpawner>();
        builder.RegisterComponentInHierarchy<TestGameModeManager>().As<IGameMode>();
}    }

