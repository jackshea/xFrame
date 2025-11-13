using System.Collections;
using System.Reflection;
using UnityEngine;
using xFrame.Runtime;
using xFrame.Runtime.Logging;

namespace xFrame.Examples.Core
{
    /// <summary>
    /// xFrame启动器示例
    /// 展示如何使用xFrameBootstrapper和xFrameApplication
    /// </summary>
    public class BootstrapperExample : MonoBehaviour
    {
        /// <summary>
        /// 是否自动创建应用程序
        /// </summary>
        [SerializeField]
        private bool autoCreateApplication = true;

        /// <summary>
        /// 日志
        /// </summary>
        private IXLogger _logger;

        /// <summary>
        /// Unity Awake生命周期
        /// </summary>
        private void Awake()
        {
            // 如果需要自动创建应用程序
            if (autoCreateApplication && xFrameApplication.Instance == null)
            {
                Debug.Log("自动创建xFrameApplication...");
                var appObject = new GameObject("xFrameApplication");
                appObject.AddComponent<xFrameApplication>();
            }
        }

        /// <summary>
        /// Unity Start生命周期
        /// </summary>
        private void Start()
        {
            // 确保应用程序已初始化
            if (xFrameApplication.Instance == null)
            {
                Debug.LogError("xFrameApplication未初始化，请确保场景中存在xFrameApplication组件");
                return;
            }

            // 获取日志记录器
            _logger =
                (xFrameApplication.Instance.Bootstrapper.LifetimeScope.Container.Resolve(typeof(IXLogManager)) as
                    IXLogManager)?.GetLogger(GetType());

            // 输出日志
            _logger.Info("BootstrapperExample已启动");
            _logger.Info($"应用程序名称: {xFrameApplication.Instance.Config.ApplicationName}");
            _logger.Info($"应用程序版本: {xFrameApplication.Instance.Config.ApplicationVersion}");

            // 获取模块管理器
            var moduleManager = xFrameApplication.Instance.Bootstrapper.ModuleManager;
            if (moduleManager != null)
            {
                _logger.Info($"已注册的模块数量: {GetModuleCount(moduleManager)}");

                // 显示所有已注册模块
                DisplayRegisteredModules(moduleManager);
            }
            else
            {
                _logger.Error("无法获取ModuleManager");
            }
        }

        /// <summary>
        /// 获取已注册的模块数量
        /// </summary>
        /// <param name="moduleManager">模块管理器</param>
        /// <returns>模块数量</returns>
        private int GetModuleCount(ModuleManager moduleManager)
        {
            // 使用反射获取私有字段_modules
            var modulesField = typeof(ModuleManager).GetField("_modules",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (modulesField != null)
            {
                var modules = modulesField.GetValue(moduleManager) as ICollection;
                return modules?.Count ?? 0;
            }

            return 0;
        }

        /// <summary>
        /// 显示所有已注册的模块
        /// </summary>
        /// <param name="moduleManager">模块管理器</param>
        private void DisplayRegisteredModules(ModuleManager moduleManager)
        {
            // 使用反射获取私有字段_modules
            var modulesField = typeof(ModuleManager).GetField("_modules",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (modulesField != null)
            {
                var modules = modulesField.GetValue(moduleManager) as IList;
                if (modules != null && modules.Count > 0)
                {
                    _logger.Info("已注册的模块列表:");
                    for (var i = 0; i < modules.Count; i++)
                    {
                        var module = modules[i];
                        _logger.Info($"  [{i}] {module.GetType().Name} (优先级: {GetModulePriority(module)})");
                    }
                }
                else
                {
                    _logger.Info("没有注册任何模块");
                }
            }
        }

        /// <summary>
        /// 获取模块优先级
        /// </summary>
        /// <param name="module">模块实例</param>
        /// <returns>优先级</returns>
        private int GetModulePriority(object module)
        {
            // 获取IModule接口的Priority属性
            var priorityProperty = module.GetType().GetProperty("Priority");
            if (priorityProperty != null) return (int)priorityProperty.GetValue(module);

            return 0;
        }
    }
}