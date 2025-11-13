using System;

namespace xFrame.Runtime.EventBus
{
    /// <summary>
    /// 事件总线工厂
    /// 提供便捷的事件总线创建方法
    /// </summary>
    public static class EventBusFactory
    {
        /// <summary>
        /// 创建标准事件总线
        /// </summary>
        /// <param name="maxHistorySize">最大历史记录数量</param>
        /// <param name="historyEnabled">是否启用历史记录</param>
        /// <returns>事件总线实例</returns>
        public static IEventBus Create(int maxHistorySize = 100, bool historyEnabled = true)
        {
            return new EventBus(maxHistorySize, historyEnabled);
        }

        /// <summary>
        /// 创建线程安全的事件总线
        /// </summary>
        /// <param name="maxConcurrentAsync">最大并发异步处理数量</param>
        /// <param name="maxHistorySize">最大历史记录数量</param>
        /// <param name="historyEnabled">是否启用历史记录</param>
        /// <returns>线程安全的事件总线实例</returns>
        public static IEventBus CreateThreadSafe(int maxConcurrentAsync = 10, int maxHistorySize = 100,
            bool historyEnabled = true)
        {
            return new ThreadSafeEventBus(maxConcurrentAsync, maxHistorySize, historyEnabled);
        }

        /// <summary>
        /// 创建高性能事件总线（针对高频事件优化）
        /// </summary>
        /// <param name="maxConcurrentAsync">最大并发异步处理数量</param>
        /// <param name="maxHistorySize">最大历史记录数量</param>
        /// <returns>高性能事件总线实例</returns>
        public static IEventBus CreateHighPerformance(int maxConcurrentAsync = 50, int maxHistorySize = 50)
        {
            // 高性能模式：禁用历史记录，增加并发数
            return new ThreadSafeEventBus(maxConcurrentAsync, maxHistorySize, false);
        }

        /// <summary>
        /// 创建调试模式事件总线（包含详细的调试信息）
        /// </summary>
        /// <param name="maxHistorySize">最大历史记录数量</param>
        /// <returns>调试模式事件总线实例</returns>
        public static IEventBus CreateDebug(int maxHistorySize = 500)
        {
            // 调试模式：启用历史记录，增大历史记录数量
            return new ThreadSafeEventBus(10, maxHistorySize);
        }

        /// <summary>
        /// 创建轻量级事件总线（最小内存占用）
        /// </summary>
        /// <returns>轻量级事件总线实例</returns>
        public static IEventBus CreateLightweight()
        {
            // 轻量级模式：最小配置
            return new EventBus(10, false);
        }
    }

    /// <summary>
    /// 事件总线配置
    /// </summary>
    public class EventBusConfig
    {
        /// <summary>
        /// 是否线程安全
        /// </summary>
        public bool IsThreadSafe { get; set; } = true;

        /// <summary>
        /// 最大并发异步处理数量
        /// </summary>
        public int MaxConcurrentAsync { get; set; } = 10;

        /// <summary>
        /// 最大历史记录数量
        /// </summary>
        public int MaxHistorySize { get; set; } = 100;

        /// <summary>
        /// 是否启用历史记录
        /// </summary>
        public bool HistoryEnabled { get; set; } = true;

        /// <summary>
        /// 是否启用性能监控
        /// </summary>
        public bool PerformanceMonitoringEnabled { get; set; }

        /// <summary>
        /// 是否启用调试模式
        /// </summary>
        public bool DebugMode { get; set; }

        /// <summary>
        /// 默认配置
        /// </summary>
        public static EventBusConfig Default => new();

        /// <summary>
        /// 高性能配置
        /// </summary>
        public static EventBusConfig HighPerformance => new()
        {
            IsThreadSafe = true,
            MaxConcurrentAsync = 50,
            MaxHistorySize = 50,
            HistoryEnabled = false,
            PerformanceMonitoringEnabled = false,
            DebugMode = false
        };

        /// <summary>
        /// 调试配置
        /// </summary>
        public static EventBusConfig Debug => new()
        {
            IsThreadSafe = true,
            MaxConcurrentAsync = 10,
            MaxHistorySize = 500,
            HistoryEnabled = true,
            PerformanceMonitoringEnabled = true,
            DebugMode = true
        };

        /// <summary>
        /// 轻量级配置
        /// </summary>
        public static EventBusConfig Lightweight => new()
        {
            IsThreadSafe = false,
            MaxConcurrentAsync = 5,
            MaxHistorySize = 10,
            HistoryEnabled = false,
            PerformanceMonitoringEnabled = false,
            DebugMode = false
        };
    }

    /// <summary>
    /// 事件总线构建器
    /// 提供链式配置方式
    /// </summary>
    public class EventBusBuilder
    {
        private readonly EventBusConfig _config;

        /// <summary>
        /// 构造函数
        /// </summary>
        public EventBusBuilder()
        {
            _config = new EventBusConfig();
        }

        /// <summary>
        /// 构造函数（使用指定配置）
        /// </summary>
        /// <param name="config">初始配置</param>
        public EventBusBuilder(EventBusConfig config)
        {
            _config = config ?? new EventBusConfig();
        }

        /// <summary>
        /// 设置线程安全模式
        /// </summary>
        /// <param name="isThreadSafe">是否线程安全</param>
        /// <returns>构建器实例</returns>
        public EventBusBuilder WithThreadSafety(bool isThreadSafe = true)
        {
            _config.IsThreadSafe = isThreadSafe;
            return this;
        }

        /// <summary>
        /// 设置最大并发异步处理数量
        /// </summary>
        /// <param name="maxConcurrentAsync">最大并发数量</param>
        /// <returns>构建器实例</returns>
        public EventBusBuilder WithMaxConcurrentAsync(int maxConcurrentAsync)
        {
            _config.MaxConcurrentAsync = Math.Max(1, maxConcurrentAsync);
            return this;
        }

        /// <summary>
        /// 设置历史记录配置
        /// </summary>
        /// <param name="enabled">是否启用历史记录</param>
        /// <param name="maxSize">最大历史记录数量</param>
        /// <returns>构建器实例</returns>
        public EventBusBuilder WithHistory(bool enabled = true, int maxSize = 100)
        {
            _config.HistoryEnabled = enabled;
            _config.MaxHistorySize = Math.Max(0, maxSize);
            return this;
        }

        /// <summary>
        /// 启用性能监控
        /// </summary>
        /// <param name="enabled">是否启用</param>
        /// <returns>构建器实例</returns>
        public EventBusBuilder WithPerformanceMonitoring(bool enabled = true)
        {
            _config.PerformanceMonitoringEnabled = enabled;
            return this;
        }

        /// <summary>
        /// 启用调试模式
        /// </summary>
        /// <param name="enabled">是否启用</param>
        /// <returns>构建器实例</returns>
        public EventBusBuilder WithDebugMode(bool enabled = true)
        {
            _config.DebugMode = enabled;
            return this;
        }

        /// <summary>
        /// 应用高性能配置
        /// </summary>
        /// <returns>构建器实例</returns>
        public EventBusBuilder AsHighPerformance()
        {
            var config = EventBusConfig.HighPerformance;
            _config.IsThreadSafe = config.IsThreadSafe;
            _config.MaxConcurrentAsync = config.MaxConcurrentAsync;
            _config.MaxHistorySize = config.MaxHistorySize;
            _config.HistoryEnabled = config.HistoryEnabled;
            _config.PerformanceMonitoringEnabled = config.PerformanceMonitoringEnabled;
            _config.DebugMode = config.DebugMode;
            return this;
        }

        /// <summary>
        /// 应用调试配置
        /// </summary>
        /// <returns>构建器实例</returns>
        public EventBusBuilder AsDebug()
        {
            var config = EventBusConfig.Debug;
            _config.IsThreadSafe = config.IsThreadSafe;
            _config.MaxConcurrentAsync = config.MaxConcurrentAsync;
            _config.MaxHistorySize = config.MaxHistorySize;
            _config.HistoryEnabled = config.HistoryEnabled;
            _config.PerformanceMonitoringEnabled = config.PerformanceMonitoringEnabled;
            _config.DebugMode = config.DebugMode;
            return this;
        }

        /// <summary>
        /// 应用轻量级配置
        /// </summary>
        /// <returns>构建器实例</returns>
        public EventBusBuilder AsLightweight()
        {
            var config = EventBusConfig.Lightweight;
            _config.IsThreadSafe = config.IsThreadSafe;
            _config.MaxConcurrentAsync = config.MaxConcurrentAsync;
            _config.MaxHistorySize = config.MaxHistorySize;
            _config.HistoryEnabled = config.HistoryEnabled;
            _config.PerformanceMonitoringEnabled = config.PerformanceMonitoringEnabled;
            _config.DebugMode = config.DebugMode;
            return this;
        }

        /// <summary>
        /// 构建事件总线实例
        /// </summary>
        /// <returns>事件总线实例</returns>
        public IEventBus Build()
        {
            if (_config.IsThreadSafe)
                return new ThreadSafeEventBus(
                    _config.MaxConcurrentAsync,
                    _config.MaxHistorySize,
                    _config.HistoryEnabled
                );

            return new EventBus(
                _config.MaxHistorySize,
                _config.HistoryEnabled
            );
        }

        /// <summary>
        /// 获取当前配置
        /// </summary>
        /// <returns>配置实例</returns>
        public EventBusConfig GetConfig()
        {
            return new EventBusConfig
            {
                IsThreadSafe = _config.IsThreadSafe,
                MaxConcurrentAsync = _config.MaxConcurrentAsync,
                MaxHistorySize = _config.MaxHistorySize,
                HistoryEnabled = _config.HistoryEnabled,
                PerformanceMonitoringEnabled = _config.PerformanceMonitoringEnabled,
                DebugMode = _config.DebugMode
            };
        }
    }
}