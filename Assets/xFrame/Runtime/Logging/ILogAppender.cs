namespace xFrame.Runtime.Logging
{
    /// <summary>
    /// 日志输出器接口
    /// 定义了日志输出的标准接口，支持多种输出通道
    /// </summary>
    public interface ILogAppender
    {
        /// <summary>
        /// 输出器名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 是否启用该输出器
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// 最小日志等级过滤
        /// </summary>
        LogLevel MinLevel { get; set; }

        /// <summary>
        /// 写入日志条目
        /// </summary>
        /// <param name="entry">日志条目</param>
        void WriteLog(LogEntry entry);

        /// <summary>
        /// 刷新缓冲区（如果有）
        /// </summary>
        void Flush();

        /// <summary>
        /// 释放资源
        /// </summary>
        void Dispose();
    }
}