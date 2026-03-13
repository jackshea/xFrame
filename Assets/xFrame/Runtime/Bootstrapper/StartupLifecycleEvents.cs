using System.Threading;
using System.Threading.Tasks;
using xFrame.Runtime.EventBus;

namespace xFrame.Runtime.Startup
{
    /// <summary>
    ///     启动生命周期事件基类。
    /// </summary>
    public interface IStartupLifecycleEvent : IEvent
    {
        /// <summary>
        ///     对应的生命周期快照。
        /// </summary>
        StartupLifecycleSnapshot Snapshot { get; }
    }

    /// <summary>
    ///     启动开始事件。
    /// </summary>
    public readonly struct StartupStartedEvent : IStartupLifecycleEvent
    {
        public StartupStartedEvent(StartupLifecycleSnapshot snapshot)
        {
            Snapshot = snapshot;
        }

        public StartupLifecycleSnapshot Snapshot { get; }
    }

    /// <summary>
    ///     启动成功事件。
    /// </summary>
    public readonly struct StartupSucceededEvent : IStartupLifecycleEvent
    {
        public StartupSucceededEvent(StartupLifecycleSnapshot snapshot)
        {
            Snapshot = snapshot;
        }

        public StartupLifecycleSnapshot Snapshot { get; }
    }

    /// <summary>
    ///     启动失败事件。
    /// </summary>
    public readonly struct StartupFailedEvent : IStartupLifecycleEvent
    {
        public StartupFailedEvent(StartupLifecycleSnapshot snapshot)
        {
            Snapshot = snapshot;
        }

        public StartupLifecycleSnapshot Snapshot { get; }
    }

    /// <summary>
    ///     启动停止事件。
    /// </summary>
    public readonly struct StartupStoppedEvent : IStartupLifecycleEvent
    {
        public StartupStoppedEvent(StartupLifecycleSnapshot snapshot)
        {
            Snapshot = snapshot;
        }

        public StartupLifecycleSnapshot Snapshot { get; }
    }

    /// <summary>
    ///     启动生命周期事件总线桥接器。
    ///     将纯 C# 生命周期快照单向转发到现有 xFrame 事件总线。
    /// </summary>
    public sealed class StartupLifecycleEventBusBridge : IStartupLifecycleHandler
    {
        /// <summary>
        ///     根据生命周期阶段转发对应事件。
        /// </summary>
        public Task HandleAsync(
            StartupLifecycleSnapshot snapshot,
            StartupTaskContext context,
            CancellationToken cancellationToken)
        {
            switch (snapshot.Stage)
            {
                case StartupLifecycleStage.Starting:
                    xFrameEventBus.Raise(new StartupStartedEvent(snapshot));
                    break;
                case StartupLifecycleStage.Succeeded:
                    xFrameEventBus.Raise(new StartupSucceededEvent(snapshot));
                    break;
                case StartupLifecycleStage.Failed:
                    xFrameEventBus.Raise(new StartupFailedEvent(snapshot));
                    break;
                case StartupLifecycleStage.Stopped:
                    xFrameEventBus.Raise(new StartupStoppedEvent(snapshot));
                    break;
            }

            return Task.CompletedTask;
        }
    }
}
