using UnityEngine;

namespace xFrame.Runtime.Logging.Appenders
{
    /// <summary>
    /// Unity调试日志输出器
    /// 将日志输出到Unity的Debug.Log系统
    /// </summary>
    public class UnityDebugLogAppender : ILogAppender
    {
        private readonly ILogFormatter _formatter;
        private readonly object _lock = new();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="formatter">日志格式化器</param>
        public UnityDebugLogAppender(ILogFormatter formatter = null)
        {
            _formatter = formatter ?? new SimpleLogFormatter();
        }

        /// <summary>
        /// 输出器名称
        /// </summary>
        public string Name => "UnityDebug";

        /// <summary>
        /// 是否启用该输出器
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 最小日志等级过滤
        /// </summary>
        public LogLevel MinLevel { get; set; } = LogLevel.Debug;

        /// <summary>
        /// 写入日志条目
        /// </summary>
        /// <param name="entry">日志条目</param>
        public void WriteLog(LogEntry entry)
        {
            if (!IsEnabled || entry.Level < MinLevel)
                return;

            lock (_lock)
            {
                var formattedMessage = _formatter.Format(entry);

                // 根据日志等级选择Unity的不同日志方法
                switch (entry.Level)
                {
                    case LogLevel.Verbose:
                    case LogLevel.Debug:
                    case LogLevel.Info:
                        Debug.Log(formattedMessage);
                        break;

                    case LogLevel.Warning:
                        Debug.LogWarning(formattedMessage);
                        break;

                    case LogLevel.Error:
                    case LogLevel.Fatal:
                        if (entry.Exception != null)
                        {
                            Debug.LogError(formattedMessage);
                            Debug.LogException(entry.Exception);
                        }
                        else
                        {
                            Debug.LogError(formattedMessage);
                        }

                        break;
                }
            }
        }

        /// <summary>
        /// 刷新缓冲区
        /// </summary>
        public void Flush()
        {
            // Unity Debug输出无需刷新
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            // Unity Debug输出无需释放资源
        }
    }
}