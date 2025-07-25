using UnityEngine;
using VContainer;
using VContainer.Unity;
using xFrame.Core.DI;
using xFrame.Core.Logging;

namespace xFrame.Core
{
    /// <summary>
    /// xFrame框架启动器
    /// 作为游戏的入口点，负责初始化框架的核心组件
    /// </summary>
    public class xFrameBootstrapper : MonoBehaviour
    {
        /// <summary>
        /// 生命周期容器预制体
        /// 如果不指定，将自动创建默认的xFrameLifetimeScope
        /// </summary>
        [SerializeField]
        private LifetimeScope lifetimeScopePrefab;

        /// <summary>
        /// 是否在Awake时自动初始化
        /// </summary>
        [SerializeField]
        private bool autoInitialize = true;

        /// <summary>
        /// 是否使用DontDestroyOnLoad保持框架对象
        /// </summary>
        [SerializeField]
        private bool dontDestroyOnLoad = true;

        /// <summary>
        /// 框架生命周期容器实例
        /// </summary>
        private LifetimeScope _lifetimeScope;

        /// <summary>
        /// 模块管理器实例
        /// </summary>
        private ModuleManager _moduleManager;

        /// <summary>
        /// 日志管理器实例
        /// </summary>
        private IXLogManager _logManager;

        /// <summary>
        /// 框架是否已初始化
        /// </summary>
        private bool _initialized = false;

        /// <summary>
        /// 单例实例
        /// </summary>
        private static xFrameBootstrapper _instance;

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static xFrameBootstrapper Instance => _instance;

        /// <summary>
        /// 获取生命周期容器
        /// </summary>
        public LifetimeScope LifetimeScope => _lifetimeScope;

        /// <summary>
        /// 获取模块管理器
        /// </summary>
        public ModuleManager ModuleManager => _moduleManager;

        /// <summary>
        /// 获取日志管理器
        /// </summary>
        public IXLogManager LogManager => _logManager;

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

            // 设置DontDestroyOnLoad
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            // 自动初始化
            if (autoInitialize)
            {
                Initialize();
            }
        }

        /// <summary>
        /// 初始化框架
        /// </summary>
        public void Initialize()
        {
            if (_initialized)
            {
                Debug.LogWarning("xFrame框架已经初始化");
                return;
            }

            Debug.Log("开始初始化xFrame框架...");

            // 创建生命周期容器
            CreateLifetimeScope();

            // 获取核心服务
            _moduleManager = _lifetimeScope.Container.Resolve<ModuleManager>();
            _logManager = _lifetimeScope.Container.Resolve<IXLogManager>();

            // 初始化模块
            _moduleManager.InitializeModules();
            _moduleManager.StartModules();

            _initialized = true;
            Debug.Log("xFrame框架初始化完成");
        }

        /// <summary>
        /// 创建生命周期容器
        /// </summary>
        private void CreateLifetimeScope()
        {
            if (lifetimeScopePrefab != null)
            {
                // 实例化预制体
                _lifetimeScope = Instantiate(lifetimeScopePrefab);
                _lifetimeScope.name = lifetimeScopePrefab.name;
            }
            else
            {
                // 创建默认的xFrameLifetimeScope
                var scopeObj = new GameObject("xFrameLifetimeScope");
                _lifetimeScope = scopeObj.AddComponent<xFrameLifetimeScope>();
                
                // 创建模块更新器
                var updaterObj = new GameObject("ModuleUpdater");
                updaterObj.transform.SetParent(scopeObj.transform);
                var updater = updaterObj.AddComponent<ModuleUpdater>();
                
                // 设置引用
                var field = _lifetimeScope.GetType().GetField("moduleUpdater", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                
                if (field != null)
                {
                    field.SetValue(_lifetimeScope, updater);
                }
                else
                {
                    Debug.LogError("无法找到moduleUpdater字段，请检查xFrameLifetimeScope类中的字段名称");
                }
            }

            // 如果使用DontDestroyOnLoad，则对生命周期容器也应用
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(_lifetimeScope.gameObject);
            }
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

            if (_initialized && _moduleManager != null)
            {
                _moduleManager.Dispose();
                _initialized = false;
                Debug.Log("xFrame框架已销毁");
            }
        }
    }
}
