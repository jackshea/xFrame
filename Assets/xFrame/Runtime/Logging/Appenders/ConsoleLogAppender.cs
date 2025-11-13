using System;

namespace xFrame.Core.Logging.Appenders
{
    /// <summary>
    /// 控制台日志输出器
    /// 将日志输出到控制台
    /// </summary>
    public class ConsoleLogAppender : ILogAppender
    {
        private readonly ILogFormatter _formatter;
        private readonly object _lock = new object();

        /// <summary>
        /// 输出器名称
        /// </summary>
        public string Name => "Console";

        /// <summary>
        /// 是否启用该输出器
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 最小日志等级过滤
        /// </summary>
        public LogLevel MinLevel { get; set; } = LogLevel.Debug;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="formatter">日志格式化器</param>
        public ConsoleLogAppender(ILogFormatter formatter = null)
        {
            _formatter = formatter ?? new DefaultLogFormatter();
        }

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
                
                // 根据日志等级设置控制台颜色
                var originalColor = Console.ForegroundColor;
                try
                {
                    Console.ForegroundColor = GetConsoleColor(entry.Level);
                    Console.WriteLine(formattedMessage);
                }
                finally
                {
                    Console.ForegroundColor = originalColor;
                }
            }
        }

        /// <summary>
        /// 刷新缓冲区
        /// </summary>
        public void Flush()
        {
            // 控制台输出无需刷新
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            // 控制台输出无需释放资源
        }

        /// <summary>
        /// 根据日志等级获取控制台颜色
        /// </summary>
        /// <param name="level">日志等级</param>
        /// <returns>控制台颜色</returns>
        private ConsoleColor GetConsoleColor(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Verbose: return ConsoleColor.Gray;
                case LogLevel.Debug: return ConsoleColor.White;
                case LogLevel.Info: return ConsoleColor.Green;
                case LogLevel.Warning: return ConsoleColor.Yellow;
                case LogLevel.Error: return ConsoleColor.Red;
                case LogLevel.Fatal: return ConsoleColor.Magenta;
                default: return ConsoleColor.White;
            }
        }
    }
}
