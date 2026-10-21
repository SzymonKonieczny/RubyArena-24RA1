using UnityEngine.Rendering.VirtualTexturing;
using VContainer;
using VContainer.Unity;
public class DefaultLifeTimeScope : LifetimeScope
{
    public static IObjectResolver Resolver { get; private set; }
    protected override void Awake()
    {
        base.Awake();
        Resolver = Container; // Capture the container on boot
    }
    protected override void Configure(IContainerBuilder builder)
    {
        base.Configure(builder);
        builder.Register(typeof(EventBus<>), Lifetime.Singleton);
        builder.RegisterComponentInHierarchy<PlayerObjectSpawner>();
        builder.RegisterComponentInHierarchy<TestGameModeManager>().As<IGameMode>();
}    }

