using System;

namespace xFrame.Core.Logging
{
    /// <summary>
    /// 静态日志访问接口
    /// 提供全局的静态日志访问方法，便于在任何地方使用日志功能
    /// </summary>
    public static class Log
    {
        private static ILogManager _logManager;
        private static readonly object _lock = new object();

        /// <summary>
        /// 初始化静态日志系统
        /// </summary>
        /// <param name="logManager">日志管理器实例</param>
        public static void Initialize(ILogManager logManager)
        {
            lock (_lock)
            {
                _logManager = logManager ?? throw new ArgumentNullException(nameof(logManager));
            }
        }

        /// <summary>
        /// 获取指定模块的日志记录器
        /// </summary>
        /// <param name="moduleName">模块名称</param>
        /// <returns>日志记录器</returns>
        public static ILogger GetLogger(string moduleName)
        {
            EnsureInitialized();
            return _logManager.GetLogger(moduleName);
        }

        /// <summary>
        /// 获取指定类型的日志记录器
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns>日志记录器</returns>
        public static ILogger GetLogger(Type type)
        {
            EnsureInitialized();
            return _logManager.GetLogger(type);
        }

        /// <summary>
        /// 获取泛型类型的日志记录器
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <returns>日志记录器</returns>
        public static ILogger GetLogger<T>()
        {
            EnsureInitialized();
            return _logManager.GetLogger<T>();
        }

        /// <summary>
        /// 记录详细日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="moduleName">模块名称</param>
        public static void Verbose(string message, string moduleName = "Global")
        {
            GetLogger(moduleName).Verbose(message);
        }

        /// <summary>
        /// 记录调试日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="moduleName">模块名称</param>
        public static void Debug(string message, string moduleName = "Global")
        {
            GetLogger(moduleName).Debug(message);
        }

        /// <summary>
        /// 记录信息日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="moduleName">模块名称</param>
        public static void Info(string message, string moduleName = "Global")
        {
            GetLogger(moduleName).Info(message);
        }

        /// <summary>
        /// 记录警告日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="moduleName">模块名称</param>
        public static void Warning(string message, string moduleName = "Global")
        {
            GetLogger(moduleName).Warning(message);
        }

        /// <summary>
        /// 记录错误日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="moduleName">模块名称</param>
        public static void Error(string message, string moduleName = "Global")
        {
            GetLogger(moduleName).Error(message);
        }

        /// <summary>
        /// 记录错误日志（带异常）
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="exception">异常信息</param>
        /// <param name="moduleName">模块名称</param>
        public static void Error(string message, Exception exception, string moduleName = "Global")
        {
            GetLogger(moduleName).Error(message, exception);
        }

        /// <summary>
        /// 记录致命日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="moduleName">模块名称</param>
        public static void Fatal(string message, string moduleName = "Global")
        {
            GetLogger(moduleName).Fatal(message);
        }

        /// <summary>
        /// 记录致命日志（带异常）
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="exception">异常信息</param>
        /// <param name="moduleName">模块名称</param>
        public static void Fatal(string message, Exception exception, string moduleName = "Global")
        {
            GetLogger(moduleName).Fatal(message, exception);
        }

        /// <summary>
        /// 刷新所有日志记录器的缓冲区
        /// </summary>
        public static void FlushAll()
        {
            EnsureInitialized();
            _logManager.FlushAll();
        }

        /// <summary>
        /// 关闭日志系统
        /// </summary>
        public static void Shutdown()
        {
            lock (_lock)
            {
                _logManager?.Shutdown();
                _logManager = null;
            }
        }

        /// <summary>
        /// 确保日志系统已初始化
        /// </summary>
        private static void EnsureInitialized()
        {
            if (_logManager == null)
            {
                throw new InvalidOperationException("日志系统尚未初始化，请先调用 Log.Initialize() 方法");
            }
        }
    }
}
