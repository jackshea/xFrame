using System;
using System.Threading;

namespace xFrame.Runtime.Logging
{
    /// <summary>
    /// 日志条目数据结构
    /// 包含一条日志的所有信息
    /// </summary>
    public struct LogEntry
    {
        /// <summary>
        /// 日志等级
        /// </summary>
        public LogLevel Level { get; }

        /// <summary>
        /// 日志消息内容
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// 模块名称
        /// </summary>
        public string ModuleName { get; }

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// 线程ID
        /// </summary>
        public int ThreadId { get; }

        /// <summary>
        /// 异常信息（可选）
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="level">日志等级</param>
        /// <param name="message">日志消息</param>
        /// <param name="moduleName">模块名称</param>
        /// <param name="exception">异常信息</param>
        public LogEntry(LogLevel level, string message, string moduleName, Exception exception = null)
        {
            Level = level;
            Message = message ?? string.Empty;
            ModuleName = moduleName ?? "Unknown";
            Timestamp = DateTime.Now;
            ThreadId = Thread.CurrentThread.ManagedThreadId;
            Exception = exception;
        }
    }
}