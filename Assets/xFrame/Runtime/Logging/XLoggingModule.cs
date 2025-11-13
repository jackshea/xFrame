using System;
using System.IO;
using UnityEngine;
using xFrame.Runtime.Logging.Appenders;

namespace xFrame.Runtime.Logging
{
    /// <summary>
    /// 日志模块
    /// 负责初始化和配置整个日志系统，集成到VContainer依赖注入框架
    /// </summary>
    public class XLoggingModule : BaseModule
    {
        private readonly IXLogManager _logManager;
        private readonly IXLogger _moduleLogger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logManager">日志管理器</param>
        public XLoggingModule(IXLogManager logManager)
        {
            _logManager = logManager ?? throw new ArgumentNullException(nameof(logManager));
            _moduleLogger = _logManager.GetLogger<XLoggingModule>();
        }

        public override string ModuleName { get; } = nameof(XLoggingModule);
        public override int Priority { get; } = 1;

        /// <summary>
        /// 初始化日志模块
        /// </summary>
        public override void OnInit()
        {
            _moduleLogger.Info("日志模块初始化开始...");

            try
            {
                // 配置默认的日志输出器
                ConfigureDefaultAppenders();

                _moduleLogger.Info("日志模块初始化完成");
            }
            catch (Exception ex)
            {
                _moduleLogger.Error("日志模块初始化失败", ex);
                throw;
            }
        }

        /// <summary>
        /// 配置默认的日志输出器
        /// </summary>
        private void ConfigureDefaultAppenders()
        {
            // Unity Debug输出器 - 用于Unity编辑器调试
            var unityDebugAppender = new UnityDebugLogAppender
            {
                MinLevel = LogLevel.Debug,
                IsEnabled = true
            };
            _logManager.AddGlobalAppender(unityDebugAppender);

            // 控制台输出器 - 用于独立构建版本
            if (!Application.isEditor)
            {
                var consoleAppender = new ConsoleLogAppender
                {
                    MinLevel = LogLevel.Info,
                    IsEnabled = true
                };
                _logManager.AddGlobalAppender(consoleAppender);
            }

            // 文件输出器 - 持久化日志
            try
            {
                var logFilePath = GetLogFilePath();
                var fileAppender = new FileLogAppender(logFilePath)
                {
                    MinLevel = LogLevel.Info,
                    IsEnabled = true
                };
                _logManager.AddGlobalAppender(fileAppender);
                _moduleLogger.Debug($"已添加文件日志输出器，路径: {logFilePath}");
            }
            catch (Exception ex)
            {
                _moduleLogger.Warning($"无法创建文件日志输出器: {ex.Message}");
            }

            // 网络输出器 - 可选，用于远程日志收集
            var networkEndpoint = GetNetworkLogEndpoint();
            if (!string.IsNullOrEmpty(networkEndpoint))
                try
                {
                    var networkAppender = new NetworkLogAppender(networkEndpoint)
                    {
                        MinLevel = LogLevel.Error,
                        IsEnabled = true
                    };
                    _logManager.AddGlobalAppender(networkAppender);
                    _moduleLogger.Debug($"已添加网络日志输出器，端点: {networkEndpoint}");
                }
                catch (Exception ex)
                {
                    _moduleLogger.Warning($"无法创建网络日志输出器: {ex.Message}");
                }
        }

        /// <summary>
        /// 获取日志文件路径
        /// </summary>
        /// <returns>日志文件路径</returns>
        private string GetLogFilePath()
        {
            var logDir = Application.persistentDataPath + "/Logs";
            var fileName = $"xFrame_{DateTime.Now:yyyyMMdd}.log";
            return Path.Combine(logDir, fileName);
        }

        /// <summary>
        /// 获取网络日志端点
        /// </summary>
        /// <returns>网络日志端点URL，如果未配置则返回null</returns>
        private string GetNetworkLogEndpoint()
        {
            // 可以从配置文件、环境变量或PlayerPrefs中读取
            // 这里提供一个示例实现
            return PlayerPrefs.GetString("xFrame.Logging.NetworkEndpoint", null);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public override void OnDestroy()
        {
            _moduleLogger?.Info("日志模块正在关闭...");
            _logManager?.Shutdown();
        }
    }
}