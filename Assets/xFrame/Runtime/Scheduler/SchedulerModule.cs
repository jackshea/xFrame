using xFrame.Runtime.Logging;

namespace xFrame.Runtime.Scheduler
{
    /// <summary>
    /// 调度器模块
    /// 负责初始化和配置调度系统，集成到VContainer依赖注入框架
    /// </summary>
    public class SchedulerModule
    {
        private readonly IXLogManager _logManager;
        private readonly IXLogger _moduleLogger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logManager">日志管理器</param>
        public SchedulerModule(IXLogManager logManager)
        {
            _logManager = logManager ?? throw new System.ArgumentNullException(nameof(logManager));
            _moduleLogger = _logManager.GetLogger<SchedulerModule>();
        }

        /// <summary>
        /// 模块名称
        /// </summary>
        public string ModuleName { get; } = nameof(SchedulerModule);

        /// <summary>
        /// 优先级
        /// </summary>
        public int Priority { get; } = 2;

        /// <summary>
        /// 初始化调度器模块
        /// </summary>
        public void OnInit()
        {
            _moduleLogger.Info("调度器模块初始化开始...");

            try
            {
                _moduleLogger.Info("调度器模块初始化完成");
            }
            catch (System.Exception ex)
            {
                _moduleLogger.Error("调度器模块初始化失败", ex);
                throw;
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void OnDestroy()
        {
            _moduleLogger?.Info("调度器模块正在关闭...");
        }
    }
}
