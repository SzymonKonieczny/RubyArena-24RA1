using VContainer;
using VContainer.Unity;
public class DefaultLifeTimeScope : LifetimeScope
{

    protected override void Configure(IContainerBuilder builder)
    {
        base.Configure(builder);
        builder.Register(typeof(EventBus<>), Lifetime.Singleton);
        builder.RegisterComponentInHierarchy<PlayerObjectSpawner>();
        builder.RegisterComponentInHierarchy<TestGameModeManager>().As<IGameMode>();
    }
}
