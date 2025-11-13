using System;
using System.Collections.Generic;

namespace xFrame.Runtime.Logging
{
    /// <summary>
    /// 日志记录器实现
    /// 提供线程安全的日志记录功能，支持多个输出器
    /// </summary>
    public class XLogger : IXLogger
    {
        private readonly List<ILogAppender> _appenders;
        private readonly object _lock = new();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="moduleName">模块名称</param>
        public XLogger(string moduleName)
        {
            ModuleName = moduleName ?? "Unknown";
            _appenders = new List<ILogAppender>();
        }

        /// <summary>
        /// 模块名称
        /// </summary>
        public string ModuleName { get; }

        /// <summary>
        /// 是否启用日志记录
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 最小日志等级
        /// </summary>
        public LogLevel MinLevel { get; set; } = LogLevel.Debug;

        /// <summary>
        /// 记录详细日志
        /// </summary>
        /// <param name="message">日志消息</param>
        public void Verbose(string message)
        {
            Log(LogLevel.Verbose, message);
        }

        /// <summary>
        /// 记录调试日志
        /// </summary>
        /// <param name="message">日志消息</param>
        public void Debug(string message)
        {
            Log(LogLevel.Debug, message);
        }

        /// <summary>
        /// 记录信息日志
        /// </summary>
        /// <param name="message">日志消息</param>
        public void Info(string message)
        {
            Log(LogLevel.Info, message);
        }

        /// <summary>
        /// 记录警告日志
        /// </summary>
        /// <param name="message">日志消息</param>
        public void Warning(string message)
        {
            Log(LogLevel.Warning, message);
        }

        /// <summary>
        /// 记录错误日志
        /// </summary>
        /// <param name="message">日志消息</param>
        public void Error(string message)
        {
            Log(LogLevel.Error, message);
        }

        /// <summary>
        /// 记录错误日志（带异常）
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="exception">异常信息</param>
        public void Error(string message, Exception exception)
        {
            Log(LogLevel.Error, message, exception);
        }

        /// <summary>
        /// 记录致命日志
        /// </summary>
        /// <param name="message">日志消息</param>
        public void Fatal(string message)
        {
            Log(LogLevel.Fatal, message);
        }

        /// <summary>
        /// 记录致命日志（带异常）
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="exception">异常信息</param>
        public void Fatal(string message, Exception exception)
        {
            Log(LogLevel.Fatal, message, exception);
        }

        /// <summary>
        /// 记录指定等级的日志
        /// </summary>
        /// <param name="level">日志等级</param>
        /// <param name="message">日志消息</param>
        /// <param name="exception">异常信息（可选）</param>
        public void Log(LogLevel level, string message, Exception exception = null)
        {
            if (!IsEnabled || !IsLevelEnabled(level))
                return;

            var entry = new LogEntry(level, message, ModuleName, exception);

            lock (_lock)
            {
                foreach (var appender in _appenders)
                    try
                    {
                        appender.WriteLog(entry);
                    }
                    catch (Exception ex)
                    {
                        // 避免日志输出器异常影响主程序
                        UnityEngine.Debug.LogError($"日志输出器 {appender.Name} 写入失败: {ex.Message}");
                    }
            }
        }

        /// <summary>
        /// 判断指定等级的日志是否会被记录
        /// </summary>
        /// <param name="level">日志等级</param>
        /// <returns>是否会被记录</returns>
        public bool IsLevelEnabled(LogLevel level)
        {
            return level >= MinLevel;
        }

        /// <summary>
        /// 添加日志输出器
        /// </summary>
        /// <param name="appender">日志输出器</param>
        public void AddAppender(ILogAppender appender)
        {
            if (appender == null)
                throw new ArgumentNullException(nameof(appender));

            lock (_lock)
            {
                _appenders.Add(appender);
            }
        }

        /// <summary>
        /// 移除日志输出器
        /// </summary>
        /// <param name="appender">日志输出器</param>
        public void RemoveAppender(ILogAppender appender)
        {
            if (appender == null)
                return;

            lock (_lock)
            {
                _appenders.Remove(appender);
            }
        }

        /// <summary>
        /// 清空所有日志输出器
        /// </summary>
        public void ClearAppenders()
        {
            lock (_lock)
            {
                foreach (var appender in _appenders) appender.Dispose();
                _appenders.Clear();
            }
        }

        /// <summary>
        /// 刷新所有输出器的缓冲区
        /// </summary>
        public void Flush()
        {
            lock (_lock)
            {
                foreach (var appender in _appenders)
                    try
                    {
                        appender.Flush();
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError($"刷新日志输出器 {appender.Name} 缓冲区失败: {ex.Message}");
                    }
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            ClearAppenders();
        }
    }
}