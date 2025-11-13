namespace xFrame.Core.Logging
{
    /// <summary>
    /// 日志等级枚举
    /// 定义了从详细到致命的六个日志等级
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// 详细日志 - 最详细的调试信息
        /// </summary>
        Verbose = 0,
        
        /// <summary>
        /// 调试日志 - 调试时使用的信息
        /// </summary>
        Debug = 1,
        
        /// <summary>
        /// 信息日志 - 一般的运行信息
        /// </summary>
        Info = 2,
        
        /// <summary>
        /// 警告日志 - 警告信息，不影响程序运行
        /// </summary>
        Warning = 3,
        
        /// <summary>
        /// 错误日志 - 错误信息，可能影响程序功能
        /// </summary>
        Error = 4,
        
        /// <summary>
        /// 致命日志 - 严重错误，可能导致程序崩溃
        /// </summary>
        Fatal = 5
    }
}
