using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using xFrame.Core.Logging;

namespace xFrame.Core
{
    /// <summary>
    /// xFrame应用程序
    /// 作为游戏的主应用程序入口，管理游戏的生命周期和核心系统
    /// </summary>
    public class xFrameApplication : MonoBehaviour
    {
        /// <summary>
        /// 框架启动器预制体
        /// 如果不指定，将自动创建默认的xFrameBootstrapper
        /// </summary>
        [SerializeField]
        private xFrameBootstrapper bootstrapperPrefab;

        /// <summary>
        /// 应用程序配置
        /// </summary>
        [SerializeField]
        private xFrameApplicationConfig applicationConfig;

        /// <summary>
        /// 框架启动器实例
        /// </summary>
        private xFrameBootstrapper _bootstrapper;

        /// <summary>
        /// 应用程序是否已初始化
        /// </summary>
        private bool _initialized = false;

        /// <summary>
        /// 单例实例
        /// </summary>
        private static xFrameApplication _instance;

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static xFrameApplication Instance => _instance;

        /// <summary>
        /// 获取框架启动器
        /// </summary>
        public xFrameBootstrapper Bootstrapper => _bootstrapper;

        /// <summary>
        /// 获取应用程序配置
        /// </summary>
        public xFrameApplicationConfig Config => applicationConfig;

        /// <summary>
        /// 应用程序初始化完成事件
        /// </summary>
        public event Action OnApplicationInitialized;

        /// <summary>
        /// Unity Awake生命周期
        /// </summary>
        private void Awake()
        {
            // 确保单例
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // 创建默认配置（如果未指定）
            if (applicationConfig == null)
            {
                applicationConfig = ScriptableObject.CreateInstance<xFrameApplicationConfig>();
                applicationConfig.name = "DefaultApplicationConfig";
            }

            // 初始化应用程序
            Initialize();
        }

        /// <summary>
        /// 初始化应用程序
        /// </summary>
        private void Initialize()
        {
            if (_initialized)
            {
                Debug.LogWarning("xFrame应用程序已经初始化");
                return;
            }

            Debug.Log("开始初始化xFrame应用程序...");

            // 创建框架启动器
            CreateBootstrapper();

            // 应用程序配置
            ApplyApplicationConfig();

            _initialized = true;
            Debug.Log("xFrame应用程序初始化完成");

            // 触发初始化完成事件
            OnApplicationInitialized?.Invoke();
        }

        /// <summary>
        /// 创建框架启动器
        /// </summary>
        private void CreateBootstrapper()
        {
            // 检查是否已存在启动器
            _bootstrapper = FindObjectOfType<xFrameBootstrapper>();
            
            if (_bootstrapper == null)
            {
                if (bootstrapperPrefab != null)
                {
                    // 实例化预制体
                    _bootstrapper = Instantiate(bootstrapperPrefab);
                    _bootstrapper.name = bootstrapperPrefab.name;
                }
                else
                {
                    // 创建默认的xFrameBootstrapper
                    var bootstrapperObj = new GameObject("xFrameBootstrapper");
                    _bootstrapper = bootstrapperObj.AddComponent<xFrameBootstrapper>();
                }

                // 确保启动器不会被自动销毁
                DontDestroyOnLoad(_bootstrapper.gameObject);
            }
        }

        /// <summary>
        /// 应用应用程序配置
        /// </summary>
        private void ApplyApplicationConfig()
        {
            if (applicationConfig != null)
            {
                // 应用日志配置
                var logManager = _bootstrapper.LogManager;
                if (logManager != null && applicationConfig.LogConfig != null)
                {
                    // 使用GlobalMinLevel属性替代SetGlobalLogLevel方法
                    logManager.GlobalMinLevel = applicationConfig.LogConfig.GlobalLogLevel;
                    
                    // 应用其他日志配置...
                    Debug.Log($"已应用日志配置，全局日志级别：{applicationConfig.LogConfig.GlobalLogLevel}");
                }
                
                // 应用其他配置...
            }
        }

        /// <summary>
        /// Unity Start生命周期
        /// </summary>
        private void Start()
        {
            // 在这里可以添加应用程序启动后的逻辑
        }

        /// <summary>
        /// Unity OnDestroy生命周期
        /// </summary>
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }

            Debug.Log("xFrame应用程序已销毁");
        }

        /// <summary>
        /// Unity OnApplicationQuit生命周期
        /// </summary>
        private void OnApplicationQuit()
        {
            // 在应用程序退出时执行清理操作
            Debug.Log("xFrame应用程序退出");
        }
    }
}
