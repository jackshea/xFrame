using VContainer;
using VContainer.Unity;

public class xFrameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // builder.Register<HelloWorldService>(Lifetime.Singleton);
    }
}