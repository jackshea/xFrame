# MessagePipe

[![GitHub Actions](https://github.com/Cysharp/MessagePipe/workflows/Build-Debug/badge.svg)](https://github.com/Cysharp/MessagePipe/actions) [![Releases](https://img.shields.io/github/release/Cysharp/MessagePipe.svg)](https://github.com/Cysharp/MessagePipe/releases)

MessagePipe 是一个高性能的内存/分布式消息管道，适用于 .NET 和 Unity。它支持 Pub/Sub 使用的所有场景、用于 CQRS 的中介者模式、Prism 的事件聚合器（视图与视图模型解耦）、进程间通信（IPC）-远程过程调用（RPC）等。

*   依赖注入优先
*   过滤管道
*   更好的事件
*   同步/异步
*   有键/无键
*   缓冲/无缓冲
*   单例/作用域
*   广播/响应（可多次）
*   内存中/进程间/分布式

MessagePipe 比标准 C# 事件更快，比 Prism 的 EventAggregator 快 78 倍。

![](https://user-images.githubusercontent.com/46207/115984507-5d36da80-a5e2-11eb-9942-66602906f499.png)

当然，每次发布操作的内存分配更少（为零）。

![](https://user-images.githubusercontent.com/46207/115814615-62542800-a430-11eb-9041-1f31c1ac8464.png)

还提供 Roslyn 分析器以防止订阅泄漏。

![](https://user-images.githubusercontent.com/46207/117535259-da753d00-b02f-11eb-9818-0ab5ef3049b1.png)

## 快速开始

对于 .NET，请使用 NuGet。对于 Unity，请阅读 [Unity](#unity) 章节。

> PM> Install-Package [MessagePipe](https://www.nuget.org/packages/MessagePipe)

MessagePipe 构建在 `Microsoft.Extensions.DependencyInjection` （对于 Unity，可使用 `VContainer`、`Zenject` 或 `Builtin Tiny DI`）之上，因此可在 [.NET 通用主机](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host) 中通过 `ConfigureServices` 进行设置。通用主机在 .NET 中被广泛使用，例如 ASP.NET Core、[MagicOnion](https://github.com/Cysharp/MagicOnion/)、[ConsoleAppFramework](https://github.com/Cysharp/ConsoleAppFramework/)、MAUI、WPF（需外部支持）等，因此很容易进行配置。

```csharp
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;

Host.CreateDefaultBuilder()
    .ConfigureServices((ctx, services) =>
    {
        services.AddMessagePipe(); // AddMessagePipe(options => { }) for configure options
    })
```

获取用于发布的 `IPublisher<T>`，获取用于订阅的 `ISubscribe<T>`，类似于 `Logger<T>`。`T` 可以是任何类型，原始类型（int、string 等）、结构体、类、枚举等。

```csharp
using MessagePipe;

public struct MyEvent { }

public class SceneA
{
    readonly IPublisher<MyEvent> publisher;
    
    public SceneA(IPublisher<MyEvent> publisher)
    {
        this.publisher = publisher;
    }

    void Send()
    {
        this.publisher.Publish(new MyEvent());
    }
}

public class SceneB
{
    readonly ISubscriber<MyEvent> subscriber;
    readonly IDisposable disposable;

    public SceneB(ISubscriber<MyEvent> subscriber)
    {
        var bag = DisposableBag.CreateBuilder(); // composite disposable for manage subscription
        
        subscriber.Subscribe(x => Console.WriteLine("here")).AddTo(bag);

        disposable = bag.Build();
    }

    void Close()
    {
        disposable.Dispose(); // unsubscribe event, all subscription **must** Dispose when completed
    }
}
```

它类似于事件，但通过类型作为键进行解耦。`Subscribe` 的返回值是 `IDisposable`，这使得取消订阅比事件更容易。你可以通过 `DisposableBag`（`CompositeDisposable`）一次性释放多个订阅。有关更多详细信息，请参阅 [管理订阅和诊断](#managing-subscription-and-diagnostics) 部分。

发布者/订阅者（内部我们称为 MessageBroker）由依赖注入（DI）管理，可以为每个作用域配置不同的代理。同时，当作用域被释放时，所有订阅会自动取消，从而防止订阅泄漏。

> 默认是单例，你可以将 `MessagePipeOptions.InstanceLifetime` 配置为 `Singleton` 或 `Scoped`。

`IPublisher<T>/ISubscriber<T>` 是无键的（仅类型），然而 MessagePipe 提供了类似的接口 `IPublisher<TKey, TMessage>/ISubscriber<TKey, TMessage>` ，该接口是有键（主题）的接口。

例如，我们的真实使用场景：有一个应用程序连接 Unity 和 [MagicOnion](https://github.com/Cysharp/MagicOnion/)（一种类似 SignalR 的实时通信框架），并通过 Blazor 在浏览器中进行传输。当时我们需要一个方式来连接 Blazor 的页面（浏览器生命周期）与 MagicOnion 的 Hub（连接生命周期）以传输数据。同时，我们还需要按连接的 ID 来分发连接。

`Browser <-> Blazor <- [MessagePipe] -> MagicOnion <-> Unity`

我们用以下代码解决了这个问题。

```csharp
// MagicOnion(similar as SignalR, realtime event framework for .NET and Unity)
public class UnityConnectionHub : StreamingHubBase<IUnityConnectionHub, IUnityConnectionHubReceiver>, IUnityConnectionHub
{
    readonly IPublisher<Guid, UnitEventData> eventPublisher;
    readonly IPublisher<Guid, ConnectionClose> closePublisher;
    Guid id;

    public UnityConnectionHub(IPublisher<Guid, UnitEventData> eventPublisher, IPublisher<Guid, ConnectionClose> closePublisher)
    {
        this.eventPublisher = eventPublisher;
        this.closePublisher = closePublisher;
    }

    override async ValueTask OnConnected()
    {
        this.id = Guid.Parse(Context.Headers["id"]);
    }

    override async ValueTask OnDisconnected()
    {
        this.closePublisher.Publish(id, new ConnectionClose()); // publish to browser(Blazor)
    }

    // called from Client(Unity)
    public Task<UnityEventData> SendEventAsync(UnityEventData data)
    {
        this.eventPublisher.Publish(id, data); // publish to browser(Blazor)
    }
}

// Blazor
public partial class BlazorPage : ComponentBase, IDisposable
{
    [Parameter]
    public Guid ID { get; set; }

    [Inject]
    ISubscriber<Guid, UnitEventData> UnityEventSubscriber { get; set; }

    [Inject]
    ISubscriber<Guid, ConnectionClose> ConnectionCloseSubscriber { get; set; }

    IDisposable subscription;

    protected override void OnInitialized()
    {
        // receive event from MagicOnion(that is from Unity)
        var d1 = UnityEventSubscriber.Subscribe(ID, x =>
        {
            // do anything...
        });

        var d2 = ConnectionCloseSubscriber.Subscribe(ID, _ =>
        {
            // show disconnected thing to view...
            subscription?.Dispose(); // and unsubscribe events.
        });

        subscription = DisposableBag.Create(d1, d2); // combine disposable.
    }
    
    public void Dispose()
    {
        // unsubscribe event when browser is closed.
        subscription?.Dispose();
    }
}
```

> Reactive Extensions 的 Subject 的主要区别是没有 `OnCompleted`。OnCompleted 可能会使用，也可能不使用，这使得观察者（订阅者）很难确定发布者的意图。此外，我们通常会从同一个（不同事件类型）发布者订阅多个事件，在这种情况下处理重复的 OnCompleted 非常困难。基于这个原因，MessagePipe 只提供简单的 Publish(OnNext)。如果你想传达完成，请接收单独的事件并在其中执行专门的处理。

> 换句话说，这相当于 [RxSwift 中的 Relay](https://github.com/ReactiveX/RxSwift/blob/main/Documentation/Subjects.md)。

除了标准的发布/订阅，MessagePipe 还支持异步处理程序、带有返回值的中介者模式处理程序，以及用于执行前后自定义的过滤器。

此图是所有这些接口之间连接关系的可视化图。

![image](https://user-images.githubusercontent.com/46207/122254092-bf87c980-cf07-11eb-8bdd-039c87309db6.png)

你可能会被众多接口的数量弄得有些困惑，但许多功能都可以用类似且统一的 API 来编写。

## 发布/订阅

发布/订阅接口包含有键（主题）和无键、同步和异步接口。

```csharp
// keyless-sync
public interface IPublisher<TMessage>
{
    void Publish(TMessage message);
}

public interface ISubscriber<TMessage>
{
    IDisposable Subscribe(IMessageHandler<TMessage> handler, params MessageHandlerFilter<TMessage>[] filters);
}

// keyless-async
public interface IAsyncPublisher<TMessage>
{
    // async interface's publish is fire-and-forget
    void Publish(TMessage message, CancellationToken cancellationToken = default(CancellationToken));
    ValueTask PublishAsync(TMessage message, CancellationToken cancellationToken = default(CancellationToken));
    ValueTask PublishAsync(TMessage message, AsyncPublishStrategy publishStrategy, CancellationToken cancellationToken = default(CancellationToken));
}

public interface IAsyncSubscriber<TMessage>
{
    IDisposable Subscribe(IAsyncMessageHandler<TMessage> asyncHandler, params AsyncMessageHandlerFilter<TMessage>[] filters);
}

// keyed-sync
public interface IPublisher<TKey, TMessage>
    where TKey : notnull
{
    void Publish(TKey key, TMessage message);
}

public interface ISubscriber<TKey, TMessage>
    where TKey : notnull
{
    IDisposable Subscribe(TKey key, IMessageHandler<TMessage> handler, params MessageHandlerFilter<TMessage>[] filters);
}

// keyed-async
public interface IAsyncPublisher<TKey, TMessage>
    where TKey : notnull
{
    void Publish(TKey key, TMessage message, CancellationToken cancellationToken = default(CancellationToken));
    ValueTask PublishAsync(TKey key, TMessage message, CancellationToken cancellationToken = default(CancellationToken));
    ValueTask PublishAsync(TKey key, TMessage message, AsyncPublishStrategy publishStrategy, CancellationToken cancellationToken = default(CancellationToken));
}

public interface IAsyncSubscriber<TKey, TMessage>
    where TKey : notnull
{
    IDisposable Subscribe(TKey key, IAsyncMessageHandler<TMessage> asyncHandler, params AsyncMessageHandlerFilter<TMessage>[] filters);
}
```

在依赖注入（DI）中，所有内容都可以通过 `IPublisher/Subscribe<T>` 的形式使用。异步处理程序可以通过 `await PublishAsync` 等待所有订阅者完成。异步方法可以按顺序或并行运行，具体取决于 `AsyncPublishStrategy`（默认是 `Parallel`，可通过 `MessagePipeOptions` 或在发布时指定进行更改）。如果不需要等待，可以调用 `void Publish` 来实现“发出即忘”的效果。

通过传递自定义过滤器，可以更改执行的前后过程。详情请参见[过滤器](#filter)部分。

如果发生错误，它会被传播到调用方，并停止后续订阅者。可以通过编写过滤器来忽略错误，从而更改此行为。

## ISingleton\*\*\*，IScoped\*\*\*

I(Async)Publisher(Subscriber) 的生命周期属于 `MessagePipeOptions.InstanceLifetime` 。但是，如果使用 `ISingletonPublisher<TMessage>`/ `ISingletonSubscriber<TKey, TMessage>` 、 `ISingletonAsyncPublisher<TMessage>` / `ISingletonAsyncSubscriber<TKey, TMessage>` 声明，则使用单例生命周期。同时，`IScopedPublisher<TMessage>`/ `IScopedSubscriber<TKey, TMessage>` 、 `IScopedAsyncPublisher<TMessage>` / `IScopedAsyncSubscriber<TKey, TMessage>` 使用作用域生命周期。

## 缓冲

`IBufferedPublisher<TMessage>/IBufferedSubscriber<TMessage>` 对类似于 `BehaviorSubject` 或 Reactive Extensions（更接近于 RxSwift 的 `BehaviorRelay`）。它会在 `Subscribe` 时返回最新值。

```csharp
var p = provider.GetRequiredService<IBufferedPublisher<int>>();
var s = provider.GetRequiredService<IBufferedSubscriber<int>>();

p.Publish(999);

var d1 = s.Subscribe(x => Console.WriteLine(x)); // 999
p.Publish(1000); // 1000

var d2 = s.Subscribe(x => Console.WriteLine(x)); // 1000
p.Publish(9999); // 9999, 9999

DisposableBag.Create(d1, d2).Dispose();
```

> 如果 `TMessage` 是类并且没有最新值（null），则在订阅时不会发送值。

> 带键的缓冲发布者/订阅者不存在，因为很难避免（未使用）键的内存泄漏并保持最新值。

## 事件工厂

使用 `EventFactory`，你可以创建通用的 `IPublisher/ISubscriber`， `IAsyncPublisher/IAsyncSubscriber` 、 `IBufferedPublisher/IBufferedSubscriber` 、 `IBufferedAsyncPublisher/IBufferedAsyncSubscriber` ，类似于 C# 事件，每个实例都绑定一个订阅者，而不是按类型分组。

MessagePipe 比普通的 C# 事件具有更好的特性

*   使用 Subscribe/Dispose 代替 `+=`、`-=`，可以轻松管理订阅
*   同时支持同步和异步
*   同时支持无缓冲和有缓冲
*   支持通过 publisher.dispose 取消所有订阅
*   通过 Filter 附加调用管道行为
*   通过 `MessagePipeDiagnosticsInfo` 监控订阅泄漏
*   防止通过 `MessagePipe.Analyzer` 产生订阅泄漏

```csharp
public class BetterEvent : IDisposable
{
    // using MessagePipe instead of C# event/Rx.Subject
    // store Publisher to private field(declare IDisposablePublisher/IDisposableAsyncPublisher)
    IDisposablePublisher<int> tickPublisher;

    // Subscriber is used from outside so public property
    public ISubscriber<int> OnTick { get; }

    public BetterEvent(EventFactory eventFactory)
    {
        // CreateEvent can deconstruct by tuple and set together
        (tickPublisher, OnTick) = eventFactory.CreateEvent<int>();

        // also create async event(IAsyncSubscriber) by `CreateAsyncEvent`
        // eventFactory.CreateAsyncEvent
    }

    int count;
    void Tick()
    {
        tickPublisher.Publish(count++);
    }

    public void Dispose()
    {
        // You can unsubscribe all from Publisher.
        tickPublisher.Dispose();
    }
}
```

如果你想在依赖注入之外创建事件，请参阅[全局提供程序](#global-provider)部分。

```csharp
IDisposablePublisher<int> tickPublisher;
public ISubscriber<int> OnTick { get; }

ctor()
{
    (tickPublisher, OnTick) = GlobalMessagePipe.CreateEvent<int>();
}
```

## 请求/响应/全部

与 [MediatR](https://github.com/jbogard/MediatR) 类似，实现对中介者模式的支持。

```csharp
public interface IRequestHandler<in TRequest, out TResponse>
{
    TResponse Invoke(TRequest request);
}

public interface IAsyncRequestHandler<in TRequest, TResponse>
{
    ValueTask<TResponse> InvokeAsync(TRequest request, CancellationToken cancellationToken = default);
}
```

例如，为 Ping 类型声明处理程序。

```csharp
public readonly struct Ping { }
public readonly struct Pong { }

public class PingPongHandler : IRequestHandler<Ping, Pong>
{
    public Pong Invoke(Ping request)
    {
        Console.WriteLine("Ping called.");
        return new Pong();
    }
}
```

你可以这样获取处理程序。

```csharp
class FooController
{
    IRequestHandler<Ping, Pong> requestHandler;

    // automatically instantiate PingPongHandler.
    public FooController(IRequestHandler<Ping, Pong> requestHandler)
    {
        this.requestHandler = requestHandler;
    }

    public void Run()
    {
        var pong = this.requestHandler.Invoke(new Ping());
        Console.WriteLine("PONG");
    }
}
```

对于更复杂的实现模式， [此 Microsoft 文档](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/microservice-application-layer-implementation-web-api#implement-the-command-process-pipeline-with-a-mediator-pattern-mediatr)适用。

声明多个请求处理程序时，可以使用 `IRequestAllHandler`、`IAsyncRequestAllHandler` 来替代单个处理程序。

```csharp
public interface IRequestAllHandler<in TRequest, out TResponse>
{
    TResponse[] InvokeAll(TRequest request);
    IEnumerable<TResponse> InvokeAllLazy(TRequest request);
}

public interface IAsyncRequestAllHandler<in TRequest, TResponse>
{
    ValueTask<TResponse[]> InvokeAllAsync(TRequest request, CancellationToken cancellationToken = default);
    ValueTask<TResponse[]> InvokeAllAsync(TRequest request, AsyncPublishStrategy publishStrategy, CancellationToken cancellationToken = default);
    IAsyncEnumerable<TResponse> InvokeAllLazyAsync(TRequest request, CancellationToken cancellationToken = default);
}
```

```csharp
public class PingPongHandler1 : IRequestHandler<Ping, Pong>
{
    public Pong Invoke(Ping request)
    {
        Console.WriteLine("Ping1 called.");
        return new Pong();
    }
}

public class PingPongHandler2 : IRequestHandler<Ping, Pong>
{
    public Pong Invoke(Ping request)
    {
        Console.WriteLine("Ping1 called.");
        return new Pong();
    }
}

class BarController
{
    IRequestAllHandler<Ping, Pong> requestAllHandler;

    public FooController(IRequestAllHandler<Ping, Pong> requestAllHandler)
    {
        this.requestAllHandler = requestAllHandler;
    }

    public void Run()
    {
        var pongs = this.requestAllHandler.InvokeAll(new Ping());
        Console.WriteLine("PONG COUNT:" + pongs.Length);
    }
}
```

## 订阅扩展

`ISubscriber`(`IAsyncSubscriber`) 接口要求使用 `IMessageHandler<T>` 来处理消息。

```csharp
public interface ISubscriber<TMessage>
{
    IDisposable Subscribe(IMessageHandler<TMessage> handler, params MessageHandlerFilter<TMessage>[] filters);
}
```

然而，扩展方法允许你直接编写 `Action<T>`。

```csharp
public static IDisposable Subscribe<TMessage>(this ISubscriber<TMessage> subscriber, Action<TMessage> handler, params MessageHandlerFilter<TMessage>[] filters)
public static IDisposable Subscribe<TMessage>(this ISubscriber<TMessage> subscriber, Action<TMessage> handler, Func<TMessage, bool> predicate, params MessageHandlerFilter<TMessage>[] filters)
public static IObservable<TMessage> AsObservable<TMessage>(this ISubscriber<TMessage> subscriber, params MessageHandlerFilter<TMessage>[] filters)
public static IAsyncEnumerable<TMessage> AsAsyncEnumerable<TMessage>(this IAsyncSubscriber<TMessage> subscriber, params AsyncMessageHandlerFilter<TMessage>[] filters)
public static ValueTask<TMessage> FirstAsync<TMessage>(this ISubscriber<TMessage> subscriber, CancellationToken cancellationToken, params MessageHandlerFilter<TMessage>[] filters)
public static ValueTask<TMessage> FirstAsync<TMessage>(this ISubscriber<TMessage> subscriber, CancellationToken cancellationToken, Func<TMessage, bool> predicate, params MessageHandlerFilter<TMessage>[] filters)
```

此外，`Func<TMessage, bool>` 的重载可以通过谓词筛选消息（内部通过 PredicateFilter 实现，其中 Order 为 int.MinValue，并且总是优先检查）。

`AsObservable` 可以将消息管道转换为 `IObservable<T>`，它可以通过 Reactive Extensions 进行处理（在 Unity 中，你可以使用 `UniRx`）。`AsObservable` 存在于同步订阅者（无键、有键、带缓冲）。

`AsAsyncEnumerable` 可以将消息管道转换为 `IAsyncEnumerable<T>`，它可以通过异步 LINQ 和异步 foreach 进行处理。`AsAsyncEnumerable` 存在于异步订阅者（无键、有键、带缓冲）。

`FirstAsync` 获取消息的第一个值。它类似于 `AsObservable().FirstAsync()`， `AsObservable().Where().FirstAsync()` 。如果使用 `CancellationTokenSource(TimeSpan)` ，则类似于 `AsObservable().Timeout().FirstAsync()` 。需要传入 `CancellationToken` 参数以避免任务泄漏。

```csharp
// for Unity, use cts.CancelAfterSlim(TIimeSpan) instead.
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
var value = await subscriber.FirstAsync(cts.Token);
```

`FirstAsync` 同时存在于同步和异步订阅者（无键、有键、带缓冲）中。

## 过滤器

过滤器系统可以在方法调用之前和之后进行挂钩。它采用中间件模式实现，这种模式允许你使用类似的语法编写同步和异步代码。MessagePipe 提供不同类型的过滤器——同步 (`MessageHandlerFilter<T>`)、异步 (`AsyncMessageHandlerFilter<T>`)、请求（ `RequestHandlerFilter<TReq, TRes>` ）以及异步请求（ `AsyncRequestHandlerFilter<TReq, TRes>` ）。要实现其他具体的过滤器，可以扩展上述过滤器类型。

过滤器可以在三个位置指定——全局（由 `MessagePipeOptions.AddGlobalFilter` 指定）、每个处理程序类型以及每个订阅。过滤器会根据各自指定的顺序进行排序，并在订阅时生成。

由于过滤器是按每个订阅生成的，因此过滤器可以有状态。

```csharp
public class ChangedValueFilter<T> : MessageHandlerFilter<T>
{
    T lastValue;

    public override void Handle(T message, Action<T> next)
    {
        if (EqualityComparer<T>.Default.Equals(message, lastValue))
        {
            return;
        }

        lastValue = message;
        next(message);
    }
}

// uses(per subscribe)
subscribe.Subscribe(x => Console.WriteLine(x), new ChangedValueFilter<int>(){ Order = 100 });

// add per handler type(use generics filter, write open generics)
[MessageHandlerFilter(typeof(ChangedValueFilter<>), 100)]
public class WriteLineHandler<T> : IMessageHandler<T>
{
    public void Handle(T message) => Console.WriteLine(message);
}

// add per global
Host.CreateDefaultBuilder()
    .ConfigureServices((ctx, services) =>
    {
        services.AddMessagePipe(options =>
        {
            options.AddGlobalMessageHandlerFilter(typeof(ChangedValueFilter<>), 100);
        });
    });
```

通过属性使用过滤器，您可以使用这些属性： `[MessageHandlerFilter(type, order)]` 、 `[AsyncMessageHandlerFilter(type, order)]` 、 `[RequestHandlerFilter(type, order)]` 、 `[AsyncRequestHandlerFilter(type, order)]` 。

这些是过滤器的示例展示。

```csharp
public class PredicateFilter<T> : MessageHandlerFilter<T>
{
    private readonly Func<T, bool> predicate;

    public PredicateFilter(Func<T, bool> predicate)
    {
        this.predicate = predicate;
    }

    public override void Handle(T message, Action<T> next)
    {
        if (predicate(message))
        {
            next(message);
        }
    }
}
```

```csharp
public class LockFilter<T> : MessageHandlerFilter<T>
{
    readonly object gate = new object();

    public override void Handle(T message, Action<T> next)
    {
        lock (gate)
        {
            next(message);
        }
    }
}
```

```csharp
public class IgnoreErrorFilter<T> : MessageHandlerFilter<T>
{
    readonly ILogger<IgnoreErrorFilter<T>> logger;

    public IgnoreErrorFilter(ILogger<IgnoreErrorFilter<T>> logger)
    {
        this.logger = logger;
    }

    public override void Handle(T message, Action<T> next)
    {
        try
        {
            next(message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ""); // error logged, but do not propagate
        }
    }
}
```

```csharp
public class DispatcherFilter<T> : MessageHandlerFilter<T>
{
    readonly Dispatcher dispatcher;

    public DispatcherFilter(Dispatcher dispatcher)
    {
        this.dispatcher = dispatcher;
    }

    public override void Handle(T message, Action<T> next)
    {
        dispatcher.BeginInvoke(() =>
        {
            next(message);
        });
    }
}
```

```csharp
public class DelayRequestFilter : AsyncRequestHandlerFilter<int, int>
{
    public override async ValueTask<int> InvokeAsync(int request, CancellationToken cancellationToken, Func<int, CancellationToken, ValueTask<int>> next)
    {
        await Task.Delay(TimeSpan.FromSeconds(request));
        var response = await next(request, cancellationToken);
        return response;
    }
}
```

## 订阅与诊断管理

订阅返回 `IDisposable`；调用 `Dispose` 时取消订阅。相比事件，一个更好的理由是取消订阅更容易。为了管理多个 IDisposable，可以在 Rx(UniRx) 中使用 `CompositeDisposable`，或者使用 MessagePipe 中包含的 `DisposableBag`。

```csharp
IDisposable disposable;

void OnInitialize(ISubscriber<int> subscriber)
{
    var d1 = subscriber.Subscribe(_ => { });
    var d2 = subscriber.Subscribe(_ => { });
    var d3 = subscriber.Subscribe(_ => { });

    // static DisposableBag: DisposableBag.Create(1~7(optimized) or N);
    disposable = DisposableBag.Create(d1, d2, d3);
}

void Close()
{
    // dispose all subscription
    disposable?.Dispose();
}
```

```csharp
IDisposable disposable;

void OnInitialize(ISubscriber<int> subscriber)
{
    // use builder pattern, you can use subscription.AddTo(bag)
    var bag = DisposableBag.CreateBuilder();

    subscriber.Subscribe(_ => { }).AddTo(bag);
    subscriber.Subscribe(_ => { }).AddTo(bag);
    subscriber.Subscribe(_ => { }).AddTo(bag);

    disposable = bag.Build(); // create final composite IDisposable
}

void Close()
{
    // dispose all subscription
    disposable?.Dispose();
}
```

```csharp
IDisposable disposable;

void OnInitialize(ISubscriber<int> subscriber)
{
    var bag = DisposableBag.CreateBuilder();

    // calling once(or x count), you can use DisposableBag.CreateSingleAssignment to hold subscription reference.
    var d = DisposableBag.CreateSingleAssignment();
    
    // you can invoke Dispose in handler action.
    // assign disposable, you can use `SetTo` and `AddTo` bag.
    // or you can use d.Disposable = subscriber.Subscribe();
    subscriber.Subscribe(_ => { d.Dispose(); }).SetTo(d).AddTo(bag);

    disposable = bag.Build();
}

void Close()
{
    disposable?.Dispose();
}
```

返回的 `IDisposable` 值**必须**进行处理。如果忽略它，会造成泄漏。然而在 WPF 中广泛使用的弱引用是一种反模式。所有订阅都应显式管理。

您可以通过 `MessagePipeDiagnosticsInfo` 监控订阅数量。它可以通过服务提供者（或依赖注入）获取。

```csharp
public sealed class MessagePipeDiagnosticsInfo
{
    /// <summary>Get current subscribed count.</summary>
    public int SubscribeCount { get; }

    /// <summary>
    /// When MessagePipeOptions.EnableCaptureStackTrace is enabled, list all stacktrace on subscribe.
    /// </summary>
    public StackTraceInfo[] GetCapturedStackTraces(bool ascending = true);

    /// <summary>
    /// When MessagePipeOptions.EnableCaptureStackTrace is enabled, groped by caller of subscribe.
    /// </summary>
    public ILookup<string, StackTraceInfo> GetGroupedByCaller(bool ascending = true)
}
```

如果监控 SubscribeCount，您可以检查订阅的泄漏情况。

```csharp
public class MonitorTimer : IDisposable
{
    CancellationTokenSource cts = new CancellationTokenSource();

    public MonitorTimer(MessagePipeDiagnosticsInfo diagnosticsInfo)
    {
        RunTimer(diagnosticsInfo);
    }

    async void RunTimer(MessagePipeDiagnosticsInfo diagnosticsInfo)
    {
        while (!cts.IsCancellationRequested)
        {
            // show SubscribeCount
            Console.WriteLine("SubscribeCount:" + diagnosticsInfo.SubscribeCount);
            await Task.Delay(TimeSpan.FromSeconds(5), cts.Token);
        }
    }

    public void Dispose()
    {
        cts.Cancel();
    }
}
```

此外，通过启用 MessagePipeOptions.EnableCaptureStackTrace（默认情况下为禁用），可以显示订阅位置的所在位置，从而在出现泄漏时更容易找到问题位置。

检查 GroupedByCaller 的计数，如果其中任何一个显示异常值，那么堆栈跟踪就是它发生的位置，并且你可能忽略了 Subscription。

对于 Unity， `Window -> MessagePipe Diagnostics` 窗口在监控订阅时非常有用。它可视化了 `MessagePipeDiagnosticsInfo`。

![image](https://user-images.githubusercontent.com/46207/116953319-e2e41580-acc7-11eb-88c9-a4704bf3e3c9.png)

要启用 MessagePipeDiagnostics 窗口，需要设置 `GlobalMessagePipe`。

```csharp
// VContainer
public class MessagePipeDemo : VContainer.Unity.IStartable
{
    public MessagePipeDemo(IObjectResolver resolver)
    {
        // require this line.
        GlobalMessagePipe.SetProvider(resolver.AsServiceProvider());
    }
}

// Zenject
void Configure(DiContainer container)
{
    GlobalMessagePipe.SetProvider(container.AsServiceProvider());
}

// builtin
var prodiver = builder.BuildServiceProvider();
GlobalMessagePipe.SetProvider(provider);
```

## 分析器

在上一节中，我们介绍了 `The returned IDisposable value **must** be handled` 。为了防止订阅泄漏，我们提供了 Roslyn 分析器。

> PM> Install-Package [MessagePipe.Analyzer](https://www.nuget.org/packages/MessagePipe.Analyzer)

![](https://user-images.githubusercontent.com/46207/117535259-da753d00-b02f-11eb-9818-0ab5ef3049b1.png)

这将针对未处理的 `Subscribe` 引发错误。

此分析器可在 Unity 2020.2 及更高版本使用（参见：[Roslyn 分析器和规则集文件](https://docs.unity3d.com/2020.2/Documentation/Manual/roslyn-analyzers.html) 文档）。`MessagePipe.Analyzer.dll` 位于 [发布页面](https://github.com/Cysharp/MessagePipe/releases/) 。

![](https://user-images.githubusercontent.com/46207/117535248-d5b08900-b02f-11eb-8add-33101a71033a.png)

目前 Unity 的分析器支持尚不完整。我们正在通过编辑器扩展补充分析器支持，请查看 [Cysharp/CsprojModifier](https://github.com/Cysharp/CsprojModifier)。

![](https://github.com/Cysharp/CsprojModifier/raw/master/docs/images/Screen-01.png)

## IDistributedPubSub / MessagePipe.Redis

对于分布式（网络）Pub/Sub，你可以使用 `IDistributedPublisher<TKey, TMessage>` 、 `IDistributedSubscriber<TKey, TMessage>` 来代替 `IAsyncPublisher`。

```csharp
public interface IDistributedPublisher<TKey, TMessage>
{
    ValueTask PublishAsync(TKey key, TMessage message, CancellationToken cancellationToken = default);
}

public interface IDistributedSubscriber<TKey, TMessage>
{
    // and also without filter overload.
    public ValueTask<IAsyncDisposable> SubscribeAsync(TKey key, IMessageHandler<TMessage> handler, MessageHandlerFilter<TMessage>[] filters, CancellationToken cancellationToken = default);
    public ValueTask<IAsyncDisposable> SubscribeAsync(TKey key, IAsyncMessageHandler<TMessage> handler, AsyncMessageHandlerFilter<TMessage>[] filters, CancellationToken cancellationToken = default);
}
```

`IAsyncPublisher` 表示内存中的 Pub/Sub。由于通过网络进行处理在本质上是不同的，你需要使用不同的接口以避免混淆。

Redis 可作为标准的网络提供程序使用。

> PM> Install-Package [MessagePipe.Redis](https://www.nuget.org/packages/MessagePipe.Redis)

使用 `AddMessagePipeRedis` 来启用 Redis 提供程序。

```csharp
Host.CreateDefaultBuilder()
    .ConfigureServices((ctx, services) =>
    {
        services.AddMessagePipe()
            .AddRedis(IConnectionMultiplexer | IConnectionMultiplexerFactory, configure);
    })
```

`IConnectionMultiplexer` 重载，你可以直接传递 [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis) 的 `ConnectionMultiplexer`。实现自己的 `IConnectionMultiplexerFactory` 以允许按键分布并从连接池中使用。

`MessagePipeRedisOptions`，你可以配置序列化。

```csharp
public sealed class MessagePipeRedisOptions
{
    public IRedisSerializer RedisSerializer { get; set; }
}

public interface IRedisSerializer
{
    byte[] Serialize<T>(T value);
    T Deserialize<T>(byte[] value);
}
```

默认情况下使用 [MessagePack for C#](https://github.com/neuecc/MessagePack-CSharp) 的 `ContractlessStandardResolver`。你可以通过 `new MessagePackRedisSerializer(options)` 更改为使用其他 `MessagePackSerializerOptions`，或者自行实现序列化器包装。

MessagePipe 为本地测试用途提供了内存版的 IDistributedPublisher/Subscriber。

```csharp
Host.CreateDefaultBuilder()
    .ConfigureServices((ctx, services) =>
    {
        var config = ctx.Configuration.Get<MyConfig>();

        var builder = services.AddMessagePipe();
        if (config.IsLocal)
        {
            // use in-memory IDistributedPublisher/Subscriber in local.
            builder.AddInMemoryDistributedMessageBroker();   
        }
        else
        {
            // use Redis IDistributedPublisher/Subscriber
            builder.AddRedis();
        }
    });
```

## 进程间发布订阅，IRemoteAsyncRequest / MessagePipe.Interprocess

对于进程间（命名管道/UDP/TCP）的发布/订阅（IPC），你可以使用 `IDistributedPublisher<TKey, TMessage>` 、 `IDistributedSubscriber<TKey, TMessage>` ，类似于 `MessagePipe.Redis`。

> PM> Install-Package MessagePipe.Interprocess

MessagePipe.Interprocess 在 Unity 上也存在（不包括命名管道）。

使用 `AddUdpInterprocess`、`AddTcpInterprocess`、`AddNamedPipeInterprocess`、`AddUdpInterprocessUds`、`AddTcpInterprocessUds` 来启用进程间提供程序（Uds 是 Unix 域套接字，是性能最佳的选项）。

```csharp
Host.CreateDefaultBuilder()
    .ConfigureServices((ctx, services) =>
    {
        services.AddMessagePipe()
            .AddUdpInterprocess("127.0.0.1", 3215, configure); // setup host and port.
            // .AddTcpInterprocess("127.0.0.1", 3215, configure);
            // .AddNamedPipeInterprocess("messagepipe-namedpipe", configure);
            // .AddUdpInterprocessUds("domainSocketPath")
            // .AddTcpInterprocessUds("domainSocketPath")
    })
```

```csharp
public async P(IDistributedPublisher<string, int> publisher)
{
    // publish value to remote process.
    await publisher.PublishAsync("foobar", 100);
}

public async S(IDistributedSubscriber<string, int> subscriber)
{
    // subscribe remote-message with "foobar" key.
    await subscriber.SubscribeAsync("foobar", x =>
    {
        Console.WriteLine(x);
    });
}
```

当注入 `IDistributedPublisher` 时，进程将作为 `server`，开始监听客户端；当注入 `IDistributedSubscriber` 时，进程将作为 `client`，开始连接到服务器。当 DI 作用域关闭时，服务器/客户端连接将被关闭。

UDP 是无连接协议，因此在客户端连接之前不需要服务器启动。然而由于协议限制，不能发送超过 64K 的消息。我们建议当消息不大时使用此协议。

NamedPipe 是 1:1 连接，无法连接多个订阅者。

TCP 没有这些限制，是所有选项中最灵活的。

默认使用 [MessagePack for C#](https://github.com/neuecc/MessagePack-CSharp) 的 `ContractlessStandardResolver` 进行消息序列化。你可以通过 MessagePipeInterprocessOptions.MessagePackSerializerOptions 更换为其他 `MessagePackSerializerOptions`。

```csharp
builder.AddUdpInterprocess("127.0.0.1", 3215, options =>
{
    // You can configure other options, `InstanceLifetime` and `UnhandledErrorHandler`.
    options.MessagePackSerializerOptions = StandardResolver.Options;
});
```

对于 IPC-RPC，你可以使用 `IRemoteRequestHandler<in TRequest, TResponse>` 调用远程 `IAsyncRequestHandler<TRequest, TResponse>` 。启用 `TcpInterprocess` 或 `NamedPipeInterprocess` 即可实现。

```csharp
Host.CreateDefaultBuilder()
    .ConfigureServices((ctx, services) =>
    {
        services.AddMessagePipe()
            .AddTcpInterprocess("127.0.0.1", 3215, x =>
            {
                x.HostAsServer = true; // if remote process as server, set true(otherwise false(default)).
            });
    });
```

```csharp
// example: server handler
public class MyAsyncHandler : IAsyncRequestHandler<int, string>
{
    public async ValueTask<string> InvokeAsync(int request, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1);
        if (request == -1)
        {
            throw new Exception("NO -1");
        }
        else
        {
            return "ECHO:" + request.ToString();
        }
    }
}
```

```csharp
// client
async void A(IRemoteRequestHandler<int, string> remoteHandler)
{
    var v = await remoteHandler.InvokeAsync(9999);
    Console.WriteLine(v); // ECHO:9999
}
```

在 Unity 中，需要导入 MessagePack-CSharp 包，并进行稍微不同的配置。

```csharp
// example of VContainer
var builder = new ContainerBuilder();
var options = builder.RegisterMessagePipe(configure);

var messagePipeBuilder = builder.ToMessagePipeBuilder(); // require to convert ServiceCollection to enable Intereprocess

var interprocessOptions = messagePipeBuilder.AddTcpInterprocess();

// register manually.
// IDistributedPublisher/Subscriber
messagePipeBuilder.RegisterTcpInterprocessMessageBroker<int, int>(interprocessOptions);
// RemoteHandler
builder.RegisterAsyncRequestHandler<int, string, MyAsyncHandler>(options); // for server
messagePipeBuilder.RegisterTcpRemoteRequestHandler<int, string>(interprocessOptions); // for client
```

## MessagePipe 选项

你可以在 `AddMessagePipe(Action<MMessagePipeOptions> configure)` 中通过 `MessagePipeOptions` 配置 MessagePipe 的行为。

```csharp
Host.CreateDefaultBuilder()
    .ConfigureServices((ctx, services) =>
    {
        // var config = ctx.Configuration.Get<MyConfig>(); // optional: get settings from configuration(use it for options configure)

        services.AddMessagePipe(options =>
        {
            options.InstanceLifetime = InstanceLifetime.Scoped;
#if DEBUG
            // EnableCaptureStackTrace slows performance, so recommended to use only in DEBUG and in profiling, disable it.
            options.EnableCaptureStackTrace = true;
#endif
        });
    })
```

Option 具有以下属性（和方法）。

```csharp
public sealed class MessagePipeOptions
{
    AsyncPublishStrategy DefaultAsyncPublishStrategy; // default is Parallel
    HandlingSubscribeDisposedPolicy HandlingSubscribeDisposedPolic; // default is Ignore
    InstanceLifetime InstanceLifetime; // default is Singleton
    InstanceLifetime RequestHandlerLifetime; // default is Scoped
    bool EnableAutoRegistration;  // default is true
    bool EnableCaptureStackTrace; // default is false

    void SetAutoRegistrationSearchAssemblies(params Assembly[] assemblies);
    void SetAutoRegistrationSearchTypes(params Type[] types);
    void AddGlobal***Filter<T>();
}

public enum AsyncPublishStrategy
{
    Parallel, Sequential
}

public enum InstanceLifetime
{
    Singleton, Scoped, Transient
}

public enum HandlingSubscribeDisposedPolicy
{
    Ignore, Throw
}
```

### 默认异步发布策略

`IAsyncPublisher` 有 `PublishAsync` 方法。如果 AsyncPublishStrategy 为 Sequential，则依次等待每个订阅者；如果为 Parallel，则使用 WhenAll。

```csharp
public interface IAsyncPublisher<TMessage>
{
    // using Default AsyncPublishStrategy
    ValueTask PublishAsync(TMessage message, CancellationToken cancellationToken = default);
    ValueTask PublishAsync(TMessage message, AsyncPublishStrategy publishStrategy, CancellationToken cancellationToken = default);
    // snip others...
}

public interface IAsyncPublisher<TKey, TMessage>
    where TKey : notnull
{
    // using Default AsyncPublishStrategy
    ValueTask PublishAsync(TKey key, TMessage message, CancellationToken cancellationToken = default);
    ValueTask PublishAsync(TKey key, TMessage message, AsyncPublishStrategy publishStrategy, CancellationToken cancellationToken = default);
    // snip others...
}

public interface IAsyncRequestAllHandler<in TRequest, TResponse>
{
    // using Default AsyncPublishStrategy
    ValueTask<TResponse[]> InvokeAllAsync(TRequest request, CancellationToken cancellationToken = default);
    ValueTask<TResponse[]> InvokeAllAsync(TRequest request, AsyncPublishStrategy publishStrategy, CancellationToken cancellationToken = default);
    // snip others...
}
```

`MessagePipeOptions.DefaultAsyncPublishStrategy` 的默认值是 `Parallel`。

### 订阅处理释放策略

当在 MessageBroker（发布者/订阅者管理器）被释放后（例如作用域已释放），调用 `ISubscriber.Subscribe` 时，可选择 `Ignore`（返回空的 `IDisposable`）或抛出 `Throw` 异常。默认值为 `Ignore`。

### 实例生命周期

配置 MessageBroker（发布者/订阅者管理器）在 DI 容器中的生命周期。可选择 `Singleton` 或 `Scoped`。默认是 `Singleton`。当选择 `Scoped` 时，每个消息代理管理不同的订阅者，且在作用域被释放时，取消订阅所有管理的订阅者。

### 请求处理程序生命周期

配置 DI 容器中 IRequestHandler/IAsyncRequestHandler 的生命周期。您可以选择 `Singleton`、`Scoped` 或 `Transient`。默认值为 `Scoped`。

### 启用自动注册/设置自动注册搜索程序集/设置自动注册搜索类型

在启动时自动将 `IRequestHandler`、`IAsyncHandler` 以及过滤器注册到 DI 容器。默认值为 `true`，默认搜索目标是 CurrentDomain 的所有程序集和类型。然而，这有时会无法检测到被剔除的程序集。在这种情况下，您可以通过显式将其添加到 `SetAutoRegistrationSearchAssemblies` 或 `SetAutoRegistrationSearchTypes` 来启用搜索。

`[IgnoreAutoRegistration]` 特性可禁用其所附加的自动注册功能。

### 启用捕获堆栈跟踪

详见[管理订阅和诊断](#managing-subscription-and-diagnostics)章节，如果设置为 `true`，则在订阅时会捕获堆栈跟踪。这对调试非常有用，但会影响性能。默认值为 `false`，建议仅在调试时启用。

### 添加全局\*\*\*过滤器

添加全局过滤器，例如日志过滤器会很有用。

```csharp
public class LoggingFilter<T> : MessageHandlerFilter<T>
{
    readonly ILogger<LoggingFilter<T>> logger;

    public LoggingFilter(ILogger<LoggingFilter<T>> logger)
    {
        this.logger = logger;
    }

    public override void Handle(T message, Action<T> next)
    {
        try
        {
            logger.LogDebug("before invoke.");
            next(message);
            logger.LogDebug("invoke completed.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "error");
        }
    }
}
```

要启用所有类型，请使用开放泛型。

```csharp
Host.CreateDefaultBuilder()
    .ConfigureServices((ctx, services) =>
    {
        services.AddMessagePipe(options =>
        {
            // use typeof(Filter<>, order);
            options.AddGlobalMessageHandlerFilter(typeof(LoggingFilter<>), -10000);
        });
    });
```

## 全局提供者

如果你想在全局范围获取发布者/订阅者/处理器，请在运行前获取 `IServiceProvider`，并设置到名为 `GlobalMessagePipe` 的静态辅助类中。

```csharp
var host = Host.CreateDefaultBuilder()
    .ConfigureServices((ctx, x) =>
    {
        x.AddMessagePipe();
    })
    .Build(); // build host before run.

GlobalMessagePipe.SetProvider(host.Services); // set service provider

await host.RunAsync(); // run framework.
```

`GlobalMessagePipe` 拥有这些静态方法（`GetPublisher<T>`、`GetSubscriber<T>`、`CreateEvent<T>` 等），因此你可以在全局范围内获取它们。

![image](https://user-images.githubusercontent.com/46207/116521078-7c00de00-a90e-11eb-85c0-2c62c140c51d.png)

## 与其他 DI 库集成

所有（流行的）DI 库都有 `Microsoft.Extensions.DependencyInjection` 桥接功能，因此可以通过 MS.E.DI 配置，并在需要时使用桥接。

## 与 Channels 比较

[System.Threading.Channels](https://docs.microsoft.com/en-us/dotnet/api/system.threading.channels)（对于 Unity 使用 `UniTask.Channels`）内部使用队列，生产者不会受到消费者性能的影响，且消费者可以控制流速（背压）。这与 MessagePipe 的发布/订阅用法不同。

## Unity

你需要安装核心库，并在运行时选择 [VContainer](https://github.com/hadashiA/VContainer/) 或 [Zenject](https://github.com/modesttree/Zenject) 或 `BuiltinContainerBuilder`。你可以通过 UPM git URL 包或在 [MessagePipe/releases](https://github.com/Cysharp/MessagePipe/releases) 页面提供的资源包（MessagePipe.\*.unitypackage）进行安装。

*   核心 `https://github.com/Cysharp/MessagePipe.git?path=src/MessagePipe.Unity/Assets/Plugins/MessagePipe`
*   VContainer `https://github.com/Cysharp/MessagePipe.git?path=src/MessagePipe.Unity/Assets/Plugins/MessagePipe.VContainer`
*   Zenject `https://github.com/Cysharp/MessagePipe.git?path=src/MessagePipe.Unity/Assets/Plugins/MessagePipe.Zenject`

此外，需要安装 [UniTask](https://github.com/Cysharp/UniTask)，在 .NET 中的所有 `ValueTask` 声明都会替换为 `UniTask`。

> \[!NOTE\] Unity 版本不支持开放泛型（针对 IL2CPP），且不支持自动注册。因此，所有需要的类型都必须手动注册。

VContainer 的安装示例。

```csharp
public class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // RegisterMessagePipe returns options.
        var options = builder.RegisterMessagePipe(/* configure option */);
        
        // Setup GlobalMessagePipe to enable diagnostics window and global function
        builder.RegisterBuildCallback(c => GlobalMessagePipe.SetProvider(c.AsServiceProvider()));

        // RegisterMessageBroker: Register for IPublisher<T>/ISubscriber<T>, includes async and buffered.
        builder.RegisterMessageBroker<int>(options);

        // also exists RegisterMessageBroker<TKey, TMessage>, RegisterRequestHandler, RegisterAsyncRequestHandler

        // RegisterMessageHandlerFilter: Register for filter, also exists RegisterAsyncMessageHandlerFilter, Register(Async)RequestHandlerFilter
        builder.RegisterMessageHandlerFilter<MyFilter<int>>();

        builder.RegisterEntryPoint<MessagePipeDemo>(Lifetime.Singleton);
    }
}

public class MessagePipeDemo : VContainer.Unity.IStartable
{
    readonly IPublisher<int> publisher;
    readonly ISubscriber<int> subscriber;

    public MessagePipeDemo(IPublisher<int> publisher, ISubscriber<int> subscriber)
    {
        this.publisher = publisher;
        this.subscriber = subscriber;
    }

    public void Start()
    {
        var d = DisposableBag.CreateBuilder();
        subscriber.Subscribe(x => Debug.Log("S1:" + x)).AddTo(d);
        subscriber.Subscribe(x => Debug.Log("S2:" + x)).AddTo(d);

        publisher.Publish(10);
        publisher.Publish(20);
        publisher.Publish(30);

        var disposable = d.Build();
        disposable.Dispose();
    }
}
```

> \[!TIP\] 如果您使用的是 Unity 2022.1 或更高版本，并且 VContainer 1.14.0 或更高版本，则不需要使用 `RegsiterMessageBroker<>`。包括 `ISubscriber<>`、`IPublisher<>` 及其异步版本在内的一组类型将会自动解析。请注意，`IRequesthandler<>` 和 `IRequestAllHanlder<>` 仍需手动注册。

Unity 版本不支持开放泛型（针对 IL2CPP），且不支持自动注册。因此，所有必需的类型都需要手动注册。

Zenject 的安装示例。

```csharp
void Configure(DiContainer builder)
{
    // BindMessagePipe returns options.
    var options = builder.BindMessagePipe(/* configure option */);
    
    // BindMessageBroker: Register for IPublisher<T>/ISubscriber<T>, includes async and buffered.
    builder.BindMessageBroker<int>(options);

    // also exists BindMessageBroker<TKey, TMessage>, BindRequestHandler, BindAsyncRequestHandler

    // BindMessageHandlerFilter: Bind for filter, also exists BindAsyncMessageHandlerFilter, Bind(Async)RequestHandlerFilter
    builder.BindMessageHandlerFilter<MyFilter<int>>();

    // set global to enable diagnostics window and global function
    GlobalMessagePipe.SetProvider(builder.AsServiceProvider());
}
```

> 由于 Zenject 的限制，Zenject 版本不支持 `InstanceScope.Singleton`。默认值为 `Scoped`，且无法更改。

`BuiltinContainerBuilder` 是 MessagePipe 内置的最小化 DI 库，使用 MessagePipe 不需要其他 DI 库。以下是安装示例。

```csharp
var builder = new BuiltinContainerBuilder();

builder.AddMessagePipe(/* configure option */);

// AddMessageBroker: Register for IPublisher<T>/ISubscriber<T>, includes async and buffered.
builder.AddMessageBroker<int>(options);

// also exists AddMessageBroker<TKey, TMessage>, AddRequestHandler, AddAsyncRequestHandler

// AddMessageHandlerFilter: Register for filter, also exists RegisterAsyncMessageHandlerFilter, Register(Async)RequestHandlerFilter
builder.AddMessageHandlerFilter<MyFilter<int>>();

// create provider and set to Global(to enable diagnostics window and global fucntion)
var provider = builder.BuildServiceProvider();
GlobalMessagePipe.SetProvider(provider);

// --- to use MessagePipe, you can use from GlobalMessagePipe.
var p = GlobalMessagePipe.GetPublisher<int>();
var s = GlobalMessagePipe.GetSubscriber<int>();

var d = s.Subscribe(x => Debug.Log(x));

p.Publish(10);
p.Publish(20);
p.Publish(30);

d.Dispose();
```

> BuiltinContainerBuilder 不支持作用域（始终为 `InstanceScope.Singleton`）、 `IRequestAllHandler/IAsyncRequestAllHandler` ，以及许多 DI 功能，因此在使用 BuiltinContainerBuilder 时，我们建议通过 `GlobalMessagePipe` 来使用。

添加全局过滤器时，你无法使用开放泛型过滤器，因此建议创建这些辅助方法。

```csharp
// Register IPublisher<T>/ISubscriber<T> and global filter.
static void RegisterMessageBroker<T>(IContainerBuilder builder, MessagePipeOptions options)
{
    builder.RegisterMessageBroker<T>(options);

    // setup for global filters.
    options.AddGlobalMessageHandlerFilter<MyMessageHandlerFilter<T>>();
}

// Register IRequestHandler<TReq, TRes>/IRequestAllHandler<TReq, TRes> and global filter.
static void RegisterRequest<TRequest, TResponse, THandler>(IContainerBuilder builder, MessagePipeOptions options)
    where THandler : IRequestHandler
{
    builder.RegisterRequestHandler<TRequest, TResponse, THandler>(options);
    
    // setup for global filters.
    options.AddGlobalRequestHandlerFilter<MyRequestHandlerFilter<TRequest, TResponse>>();
}
```

你也可以使用 `GlobalMessagePipe` 和 `MessagePipe Diagnostics` 窗口。参见： [全局提供者](#global-provider) 和 [管理订阅与诊断](#managing-subscription-and-diagnostics) 章节。

## 许可协议

此库采用 MIT 许可证。