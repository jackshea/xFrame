using System;

namespace xFrame.Core.Logging
{
    /// <summary>
    /// 日志记录器接口
    /// 提供统一的日志记录功能
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// 模块名称
        /// </summary>
        string ModuleName { get; }
        
        /// <summary>
        /// 是否启用日志记录
        /// </summary>
        bool IsEnabled { get; set; }
        
        /// <summary>
        /// 最小日志等级
        /// </summary>
        LogLevel MinLevel { get; set; }
        
        /// <summary>
        /// 记录详细日志
        /// </summary>
        /// <param name="message">日志消息</param>
        void Verbose(string message);
        
        /// <summary>
        /// 记录调试日志
        /// </summary>
        /// <param name="message">日志消息</param>
        void Debug(string message);
        
        /// <summary>
        /// 记录信息日志
        /// </summary>
        /// <param name="message">日志消息</param>
        void Info(string message);
        
        /// <summary>
        /// 记录警告日志
        /// </summary>
        /// <param name="message">日志消息</param>
        void Warning(string message);
        
        /// <summary>
        /// 记录错误日志
        /// </summary>
        /// <param name="message">日志消息</param>
        void Error(string message);
        
        /// <summary>
        /// 记录错误日志（带异常）
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="exception">异常信息</param>
        void Error(string message, Exception exception);
        
        /// <summary>
        /// 记录致命日志
        /// </summary>
        /// <param name="message">日志消息</param>
        void Fatal(string message);
        
        /// <summary>
        /// 记录致命日志（带异常）
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="exception">异常信息</param>
        void Fatal(string message, Exception exception);
        
        /// <summary>
        /// 记录指定等级的日志
        /// </summary>
        /// <param name="level">日志等级</param>
        /// <param name="message">日志消息</param>
        /// <param name="exception">异常信息（可选）</param>
        void Log(LogLevel level, string message, Exception exception = null);
        
        /// <summary>
        /// 判断指定等级的日志是否会被记录
        /// </summary>
        /// <param name="level">日志等级</param>
        /// <returns>是否会被记录</returns>
        bool IsLevelEnabled(LogLevel level);
    }
}
