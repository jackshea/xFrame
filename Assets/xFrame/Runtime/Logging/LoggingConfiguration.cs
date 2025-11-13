using System;
using UnityEngine;

namespace xFrame.Runtime.Logging
{
    /// <summary>
    /// 日志系统配置类
    /// 提供日志系统的各种配置选项
    /// </summary>
    [Serializable]
    public class LoggingConfiguration
    {
        [Header("全局设置")]
        [Tooltip("全局日志等级")]
        public LogLevel globalMinLevel = LogLevel.Debug;

        [Tooltip("是否启用全局日志")]
        public bool isGlobalEnabled = true;

        [Header("Unity Debug输出器")]
        [Tooltip("是否启用Unity Debug输出")]
        public bool enableUnityDebug = true;

        [Tooltip("Unity Debug输出最小等级")]
        public LogLevel unityDebugMinLevel = LogLevel.Debug;

        [Header("控制台输出器")]
        [Tooltip("是否启用控制台输出")]
        public bool enableConsole = true;

        [Tooltip("控制台输出最小等级")]
        public LogLevel consoleMinLevel = LogLevel.Info;

        [Header("文件输出器")]
        [Tooltip("是否启用文件输出")]
        public bool enableFile = true;

        [Tooltip("文件输出最小等级")]
        public LogLevel fileMinLevel = LogLevel.Info;

        [Tooltip("自定义日志文件路径（为空则使用默认路径）")]
        public string customLogFilePath = "";

        [Header("网络输出器")]
        [Tooltip("是否启用网络输出")]
        public bool enableNetwork;

        [Tooltip("网络输出最小等级")]
        public LogLevel networkMinLevel = LogLevel.Error;

        [Tooltip("网络日志服务器端点")]
        public string networkEndpoint = "";

        [Tooltip("网络日志刷新间隔（毫秒）")]
        public int networkFlushInterval = 5000;

        [Header("格式化设置")]
        [Tooltip("时间格式")]
        public string dateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";

        [Tooltip("是否包含线程ID")]
        public bool includeThreadId = true;

        [Tooltip("是否包含模块名")]
        public bool includeModuleName = true;

        /// <summary>
        /// 创建默认配置
        /// </summary>
        /// <returns>默认日志配置</returns>
        public static LoggingConfiguration CreateDefault()
        {
            return new LoggingConfiguration();
        }

        /// <summary>
        /// 创建开发环境配置
        /// </summary>
        /// <returns>开发环境日志配置</returns>
        public static LoggingConfiguration CreateDevelopment()
        {
            return new LoggingConfiguration
            {
                globalMinLevel = LogLevel.Verbose,
                enableUnityDebug = true,
                unityDebugMinLevel = LogLevel.Verbose,
                enableConsole = true,
                consoleMinLevel = LogLevel.Debug,
                enableFile = true,
                fileMinLevel = LogLevel.Debug,
                enableNetwork = false
            };
        }

        /// <summary>
        /// 创建生产环境配置
        /// </summary>
        /// <returns>生产环境日志配置</returns>
        public static LoggingConfiguration CreateProduction()
        {
            return new LoggingConfiguration
            {
                globalMinLevel = LogLevel.Info,
                enableUnityDebug = false,
                enableConsole = true,
                consoleMinLevel = LogLevel.Info,
                enableFile = true,
                fileMinLevel = LogLevel.Info,
                enableNetwork = true,
                networkMinLevel = LogLevel.Error
            };
        }
    }
}