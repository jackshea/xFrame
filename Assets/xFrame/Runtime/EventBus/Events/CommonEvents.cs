using System;
using System.Collections.Generic;

namespace xFrame.Runtime.EventBus.Events
{
    /// <summary>
    /// 系统启动事件
    /// </summary>
    public class SystemStartupEvent : BaseEvent
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public SystemStartupEvent()
        {
            StartupTime = DateTime.Now;
            StartupArgs = new string[0];
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="args">启动参数</param>
        public SystemStartupEvent(string[] args) : this()
        {
            StartupArgs = args ?? new string[0];
        }

        /// <summary>
        /// 启动时间
        /// </summary>
        public DateTime StartupTime { get; set; }

        /// <summary>
        /// 启动参数
        /// </summary>
        public string[] StartupArgs { get; set; }
    }

    /// <summary>
    /// 系统关闭事件
    /// </summary>
    public class SystemShutdownEvent : BaseEvent
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public SystemShutdownEvent()
        {
            Reason = "Normal shutdown";
            IsForced = false;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="reason">关闭原因</param>
        /// <param name="isForced">是否强制关闭</param>
        public SystemShutdownEvent(string reason, bool isForced = false) : this()
        {
            Reason = reason;
            IsForced = isForced;
        }

        /// <summary>
        /// 关闭原因
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// 是否强制关闭
        /// </summary>
        public bool IsForced { get; set; }
    }

    /// <summary>
    /// 系统暂停事件
    /// </summary>
    public class SystemPauseEvent : BaseEvent
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public SystemPauseEvent()
        {
            Reason = "System paused";
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="reason">暂停原因</param>
        public SystemPauseEvent(string reason) : this()
        {
            Reason = reason;
        }

        /// <summary>
        /// 暂停原因
        /// </summary>
        public string Reason { get; set; }
    }

    /// <summary>
    /// 系统恢复事件
    /// </summary>
    public class SystemResumeEvent : BaseEvent
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public SystemResumeEvent()
        {
            Reason = "System resumed";
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="reason">恢复原因</param>
        public SystemResumeEvent(string reason) : this()
        {
            Reason = reason;
        }

        /// <summary>
        /// 恢复原因
        /// </summary>
        public string Reason { get; set; }
    }

    /// <summary>
    /// 错误事件
    /// </summary>
    public class ErrorEvent : BaseEvent
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public ErrorEvent()
        {
            Level = ErrorLevel.Error;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="level">错误级别</param>
        /// <param name="source">错误来源</param>
        public ErrorEvent(string message, ErrorLevel level = ErrorLevel.Error, string source = null) : this()
        {
            Message = message;
            Level = level;
            Source = source;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="exception">异常信息</param>
        /// <param name="level">错误级别</param>
        /// <param name="source">错误来源</param>
        public ErrorEvent(Exception exception, ErrorLevel level = ErrorLevel.Error, string source = null) : this()
        {
            Exception = exception;
            Message = exception?.Message;
            Level = level;
            Source = source;
        }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 异常信息
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// 错误级别
        /// </summary>
        public ErrorLevel Level { get; set; }

        /// <summary>
        /// 错误来源
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// 事件优先级（错误事件优先级较高）
        /// </summary>
        public override int Priority => -10;
    }

    /// <summary>
    /// 错误级别枚举
    /// </summary>
    public enum ErrorLevel
    {
        /// <summary>
        /// 信息
        /// </summary>
        Info = 0,

        /// <summary>
        /// 警告
        /// </summary>
        Warning = 1,

        /// <summary>
        /// 错误
        /// </summary>
        Error = 2,

        /// <summary>
        /// 致命错误
        /// </summary>
        Fatal = 3
    }

    /// <summary>
    /// 日志事件
    /// </summary>
    public class LogEvent : BaseEvent
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public LogEvent()
        {
            Level = LogLevel.Info;
            Tags = new string[0];
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="level">日志级别</param>
        /// <param name="source">日志来源</param>
        /// <param name="tags">日志标签</param>
        public LogEvent(string message, LogLevel level = LogLevel.Info, string source = null,
            params string[] tags) : this()
        {
            Message = message;
            Level = level;
            Source = source;
            Tags = tags ?? new string[0];
        }

        /// <summary>
        /// 日志消息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 日志级别
        /// </summary>
        public LogLevel Level { get; set; }

        /// <summary>
        /// 日志来源
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// 日志标签
        /// </summary>
        public string[] Tags { get; set; }
    }

    /// <summary>
    /// 日志级别枚举
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// 调试
        /// </summary>
        Debug = 0,

        /// <summary>
        /// 信息
        /// </summary>
        Info = 1,

        /// <summary>
        /// 警告
        /// </summary>
        Warning = 2,

        /// <summary>
        /// 错误
        /// </summary>
        Error = 3
    }

    /// <summary>
    /// 配置变更事件
    /// </summary>
    public class ConfigChangedEvent : BaseEvent<ConfigChangeData>
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public ConfigChangedEvent()
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="configKey">配置键</param>
        /// <param name="oldValue">旧值</param>
        /// <param name="newValue">新值</param>
        public ConfigChangedEvent(string configKey, object oldValue, object newValue)
        {
            Data = new ConfigChangeData
            {
                ConfigKey = configKey,
                OldValue = oldValue,
                NewValue = newValue,
                ChangeTime = DateTime.Now
            };
        }
    }

    /// <summary>
    /// 配置变更数据
    /// </summary>
    public class ConfigChangeData
    {
        /// <summary>
        /// 配置键
        /// </summary>
        public string ConfigKey { get; set; }

        /// <summary>
        /// 旧值
        /// </summary>
        public object OldValue { get; set; }

        /// <summary>
        /// 新值
        /// </summary>
        public object NewValue { get; set; }

        /// <summary>
        /// 变更时间
        /// </summary>
        public DateTime ChangeTime { get; set; }

        /// <summary>
        /// 变更来源
        /// </summary>
        public string Source { get; set; }
    }

    /// <summary>
    /// 用户操作事件
    /// </summary>
    public class UserActionEvent : BaseEvent<UserActionData>
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public UserActionEvent()
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="actionType">操作类型</param>
        /// <param name="target">操作目标</param>
        /// <param name="parameters">操作参数</param>
        public UserActionEvent(string actionType, string target, object parameters = null)
        {
            Data = new UserActionData
            {
                ActionType = actionType,
                Target = target,
                Parameters = parameters,
                ActionTime = DateTime.Now
            };
        }
    }

    /// <summary>
    /// 用户操作数据
    /// </summary>
    public class UserActionData
    {
        /// <summary>
        /// 操作类型
        /// </summary>
        public string ActionType { get; set; }

        /// <summary>
        /// 操作目标
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// 操作参数
        /// </summary>
        public object Parameters { get; set; }

        /// <summary>
        /// 操作时间
        /// </summary>
        public DateTime ActionTime { get; set; }

        /// <summary>
        /// 用户ID
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// 会话ID
        /// </summary>
        public string SessionId { get; set; }
    }

    /// <summary>
    /// 性能监控事件
    /// </summary>
    public class PerformanceEvent : BaseEvent<PerformanceData>
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public PerformanceEvent()
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="metricName">指标名称</param>
        /// <param name="value">指标值</param>
        /// <param name="unit">单位</param>
        public PerformanceEvent(string metricName, double value, string unit = null)
        {
            Data = new PerformanceData
            {
                MetricName = metricName,
                Value = value,
                Unit = unit,
                MeasureTime = DateTime.Now
            };
        }
    }

    /// <summary>
    /// 性能数据
    /// </summary>
    public class PerformanceData
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public PerformanceData()
        {
            Properties = new Dictionary<string, object>();
        }

        /// <summary>
        /// 指标名称
        /// </summary>
        public string MetricName { get; set; }

        /// <summary>
        /// 指标值
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// 单位
        /// </summary>
        public string Unit { get; set; }

        /// <summary>
        /// 测量时间
        /// </summary>
        public DateTime MeasureTime { get; set; }

        /// <summary>
        /// 来源组件
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// 附加属性
        /// </summary>
        public Dictionary<string, object> Properties { get; set; }
    }
}