using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace xFrame.Runtime.Logging
{
    /// <summary>
    /// 日志管理器实现
    /// 提供全局的日志管理功能，包括Logger创建、全局配置等
    /// </summary>
    public class XLogManager : IXLogManager
    {
        private readonly List<ILogAppender> _globalAppenders;
        private readonly object _lock = new();
        private readonly ConcurrentDictionary<string, IXLogger> _loggers;

        /// <summary>
        /// 构造函数
        /// </summary>
        public XLogManager()
        {
            _loggers = new ConcurrentDictionary<string, IXLogger>();
            _globalAppenders = new List<ILogAppender>();

            // 注册Unity异常处理
            RegisterUnityExceptionHandler();
        }

        /// <summary>
        /// 全局日志等级
        /// </summary>
        public LogLevel GlobalMinLevel { get; set; } = LogLevel.Debug;

        /// <summary>
        /// 是否启用全局日志
        /// </summary>
        public bool IsGlobalEnabled { get; set; } = true;

        /// <summary>
        /// 获取指定模块的日志记录器
        /// </summary>
        /// <param name="moduleName">模块名称</param>
        /// <returns>日志记录器</returns>
        public IXLogger GetLogger(string moduleName)
        {
            if (string.IsNullOrEmpty(moduleName))
                moduleName = "Default";

            return _loggers.GetOrAdd(moduleName, name =>
            {
                var logger = new XLogger(name)
                {
                    IsEnabled = IsGlobalEnabled,
                    MinLevel = GlobalMinLevel
                };

                // 添加全局输出器
                lock (_lock)
                {
                    foreach (var appender in _globalAppenders) logger.AddAppender(appender);
                }

                return logger;
            });
        }

        /// <summary>
        /// 获取指定类型的日志记录器
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns>日志记录器</returns>
        public IXLogger GetLogger(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            return GetLogger(type.Name);
        }

        /// <summary>
        /// 获取泛型类型的日志记录器
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <returns>日志记录器</returns>
        public IXLogger GetLogger<T>()
        {
            return GetLogger(typeof(T));
        }

        /// <summary>
        /// 添加全局日志输出器
        /// </summary>
        /// <param name="appender">日志输出器</param>
        public void AddGlobalAppender(ILogAppender appender)
        {
            if (appender == null)
                throw new ArgumentNullException(nameof(appender));

            lock (_lock)
            {
                _globalAppenders.Add(appender);

                // 为所有现有的Logger添加此输出器
                foreach (var logger in _loggers.Values)
                    if (logger is XLogger concreteLogger)
                        concreteLogger.AddAppender(appender);
            }
        }

        /// <summary>
        /// 移除全局日志输出器
        /// </summary>
        /// <param name="appender">日志输出器</param>
        public void RemoveGlobalAppender(ILogAppender appender)
        {
            if (appender == null)
                return;

            lock (_lock)
            {
                _globalAppenders.Remove(appender);

                // 从所有现有的Logger中移除此输出器
                foreach (var logger in _loggers.Values)
                    if (logger is XLogger concreteLogger)
                        concreteLogger.RemoveAppender(appender);
            }
        }

        /// <summary>
        /// 刷新所有日志记录器的缓冲区
        /// </summary>
        public void FlushAll()
        {
            foreach (var logger in _loggers.Values)
                if (logger is XLogger concreteLogger)
                    concreteLogger.Flush();
        }

        /// <summary>
        /// 关闭日志系统并释放资源
        /// </summary>
        public void Shutdown()
        {
            FlushAll();

            lock (_lock)
            {
                foreach (var appender in _globalAppenders) appender.Dispose();
                _globalAppenders.Clear();
            }

            foreach (var logger in _loggers.Values)
                if (logger is XLogger concreteLogger)
                    concreteLogger.Dispose();

            _loggers.Clear();
        }

        /// <summary>
        /// 注册Unity异常处理器
        /// </summary>
        private void RegisterUnityExceptionHandler()
        {
            // Application.logMessageReceived += OnUnityLogMessageReceived;
        }

        /// <summary>
        /// Unity日志消息接收处理
        /// </summary>
        /// <param name="logString">日志字符串</param>
        /// <param name="stackTrace">堆栈跟踪</param>
        /// <param name="type">日志类型</param>
        private void OnUnityLogMessageReceived(string logString, string stackTrace, LogType type)
        {
            // 避免循环日志
            if (logString.Contains("[xFrame.Logging]"))
                return;

            var logger = GetLogger("Unity");
            var message = $"[Unity] {logString}";

            if (!string.IsNullOrEmpty(stackTrace)) message += $"\n堆栈跟踪:\n{stackTrace}";

            switch (type)
            {
                case LogType.Error:
                case LogType.Exception:
                    logger.Error(message);
                    break;
                case LogType.Assert:
                    logger.Fatal(message);
                    break;
                case LogType.Warning:
                    logger.Warning(message);
                    break;
                case LogType.Log:
                    logger.Info(message);
                    break;
            }
        }
    }
}