using System;
using System.Threading;
using System.Threading.Tasks;

namespace xFrame.Runtime.Startup
{
    /// <summary>
    ///     启动生命周期阶段。
    /// </summary>
    public enum StartupLifecycleStage
    {
        Idle = 0,
        Starting = 1,
        Succeeded = 2,
        Failed = 3,
        Stopped = 4
    }

    /// <summary>
    ///     启动生命周期快照。
    ///     统一描述启动流程当前所处阶段及最近一次运行结果。
    /// </summary>
    public readonly struct StartupLifecycleSnapshot
    {
        public StartupLifecycleSnapshot(
            StartupLifecycleStage stage,
            BootEnvironment environment,
            StartupPipelineResult pipelineResult)
        {
            Stage = stage;
            Environment = environment;
            PipelineResult = pipelineResult;
        }

        /// <summary>
        ///     当前生命周期阶段。
        /// </summary>
        public StartupLifecycleStage Stage { get; }

        /// <summary>
        ///     当前或最近一次运行所使用的启动环境。
        /// </summary>
        public BootEnvironment Environment { get; }

        /// <summary>
        ///     最近一次启动执行结果。
        /// </summary>
        public StartupPipelineResult PipelineResult { get; }
    }

    /// <summary>
    ///     启动生命周期只读状态接口。
    ///     供业务层订阅统一的启动完成/失败出口，而不直接依赖 Unity 生命周期。
    /// </summary>
    public interface IStartupLifecycleState
    {
        /// <summary>
        ///     当前生命周期快照。
        /// </summary>
        StartupLifecycleSnapshot Current { get; }

        /// <summary>
        ///     生命周期快照变更事件。
        /// </summary>
        event Action<StartupLifecycleSnapshot> Changed;
    }

    /// <summary>
    ///     启动生命周期写入接口。
    /// </summary>
    public interface IStartupLifecycleSink
    {
        /// <summary>
        ///     发布最新的生命周期快照。
        /// </summary>
        void Publish(StartupLifecycleSnapshot snapshot);
    }

    /// <summary>
    ///     启动生命周期处理器。
    ///     用于在纯 C# 层对启动成功、失败、停止等状态做业务级响应。
    /// </summary>
    public interface IStartupLifecycleHandler
    {
        /// <summary>
        ///     处理启动生命周期快照变更。
        /// </summary>
        Task HandleAsync(
            StartupLifecycleSnapshot snapshot,
            StartupTaskContext context,
            CancellationToken cancellationToken);
    }

    /// <summary>
    ///     启动失败恢复处理器。
    ///     负责承接启动失败后的纯 C# 恢复、降级或错误上报逻辑。
    /// </summary>
    public interface IStartupFailureHandler
    {
        /// <summary>
        ///     处理启动失败结果。
        /// </summary>
        Task HandleFailureAsync(
            StartupLifecycleSnapshot snapshot,
            StartupTaskContext context,
            CancellationToken cancellationToken);
    }

    /// <summary>
    ///     启动错误呈现服务。
    ///     用于将纯 C# 启动失败信息交给宿主层展示。
    /// </summary>
    public interface IStartupErrorPresentationService
    {
        /// <summary>
        ///     展示启动失败信息。
        /// </summary>
        Task PresentErrorAsync(
            StartupLifecycleSnapshot snapshot,
            StartupTaskContext context,
            CancellationToken cancellationToken);
    }

    /// <summary>
    ///     默认的启动生命周期状态存储。
    /// </summary>
    public sealed class StartupLifecycleStateStore : IStartupLifecycleState, IStartupLifecycleSink
    {
        /// <summary>
        ///     当前生命周期快照。
        /// </summary>
        public StartupLifecycleSnapshot Current { get; private set; } =
            new(StartupLifecycleStage.Idle, BootEnvironment.DevFull, default);

        /// <summary>
        ///     生命周期快照变更事件。
        /// </summary>
        public event Action<StartupLifecycleSnapshot> Changed;

        /// <summary>
        ///     发布并保存新的生命周期快照。
        /// </summary>
        public void Publish(StartupLifecycleSnapshot snapshot)
        {
            Current = snapshot;
            Changed?.Invoke(snapshot);
        }
    }
}
