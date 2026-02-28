using System;
using UnityEngine;
using xFrame.Runtime.Logging;

namespace xFrame.Runtime
{
    /// <summary>
    /// xFrame应用程序配置
    /// 用于配置框架的各种参数
    /// </summary>
    [CreateAssetMenu(fileName = "xFrameApplicationConfig", menuName = "xFrame/应用程序配置", order = 1)]
    public class xFrameApplicationConfig : ScriptableObject
    {
        /// <summary>
        /// 应用程序名称
        /// </summary>
        [Header("应用程序基本信息")]
        [Tooltip("应用程序名称")]
        public string ApplicationName = "xFrame应用";

        /// <summary>
        /// 应用程序版本
        /// </summary>
        [Tooltip("应用程序版本")]
        public string ApplicationVersion = "1.0.0";

        /// <summary>
        /// 日志配置
        /// </summary>
        [Header("日志配置")]
        [Tooltip("日志系统配置")]
        public LogConfig LogConfig;

        /// <summary>
        /// 模块配置
        /// </summary>
        [Header("模块配置")]
        [Tooltip("模块系统配置")]
        public ModuleConfig ModuleConfig;

        /// <summary>
        /// Unity OnEnable生命周期
        /// </summary>
        private void OnEnable()
        {
            // 确保配置对象有效
            if (LogConfig == null) LogConfig = new LogConfig();

            if (ModuleConfig == null) ModuleConfig = new ModuleConfig();
        }

        /// <summary>
        /// 重置为默认值
        /// </summary>
        public void ResetToDefaults()
        {
            ApplicationName = "xFrame应用";
            ApplicationVersion = "1.0.0";

            // 重置日志配置
            if (LogConfig == null)
            {
                LogConfig = new LogConfig();
            }
            else
            {
                LogConfig.GlobalLogLevel = LogLevel.Info;
                LogConfig.EnableFileLogging = false;
                LogConfig.LogFilePath = "Logs/xFrame.log";
                LogConfig.ShowLogsInConsole = true;
                LogConfig.ShowTimestamp = true;
                LogConfig.ShowLogLevel = true;
                LogConfig.ShowModuleName = true;
            }

            // 重置模块配置
            if (ModuleConfig == null)
            {
                ModuleConfig = new ModuleConfig();
            }
            else
            {
                ModuleConfig.AutoInitializeModules = true;
                ModuleConfig.AutoStartModules = true;
                ModuleConfig.ModuleInitTimeout = 10f;
                ModuleConfig.ModuleStartTimeout = 10f;
            }
        }
    }

    /// <summary>
    /// 日志配置类
    /// </summary>
    [Serializable]
    public class LogConfig
    {
        /// <summary>
        /// 全局日志级别
        /// </summary>
        [Tooltip("全局日志级别")]
        public LogLevel GlobalLogLevel = LogLevel.Info;

        /// <summary>
        /// 是否启用文件日志
        /// </summary>
        [Tooltip("是否启用文件日志")]
        public bool EnableFileLogging;

        /// <summary>
        /// 日志文件路径
        /// </summary>
        [Tooltip("日志文件路径")]
        public string LogFilePath = "Logs/xFrame.log";

        /// <summary>
        /// 是否在控制台显示日志
        /// </summary>
        [Tooltip("是否在控制台显示日志")]
        public bool ShowLogsInConsole = true;

        /// <summary>
        /// 是否显示时间戳
        /// </summary>
        [Tooltip("是否显示时间戳")]
        public bool ShowTimestamp = true;

        /// <summary>
        /// 是否显示日志级别
        /// </summary>
        [Tooltip("是否显示日志级别")]
        public bool ShowLogLevel = true;

        /// <summary>
        /// 是否显示模块名称
        /// </summary>
        [Tooltip("是否显示模块名称")]
        public bool ShowModuleName = true;
    }

    /// <summary>
    /// 模块配置类
    /// </summary>
    [Serializable]
    public class ModuleConfig
    {
        /// <summary>
        /// 是否自动初始化模块
        /// </summary>
        [Tooltip("是否自动初始化模块")]
        public bool AutoInitializeModules = true;

        /// <summary>
        /// 是否自动启动模块
        /// </summary>
        [Tooltip("是否自动启动模块")]
        public bool AutoStartModules = true;

        /// <summary>
        /// 模块初始化超时时间（秒）
        /// </summary>
        [Tooltip("模块初始化超时时间（秒）")]
        public float ModuleInitTimeout = 10f;

        /// <summary>
        /// 模块启动超时时间（秒）
        /// </summary>
        [Tooltip("模块启动超时时间（秒）")]
        public float ModuleStartTimeout = 10f;
    }
}