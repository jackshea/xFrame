using UnityEngine;
using xFrame.Core;
using VContainer;
using VContainer.Unity;

namespace xFrame.Examples
{
    /// <summary>
    /// 示例场景
    /// 演示如何在场景中使用模块系统
    /// </summary>
    public class ExampleScene : MonoBehaviour
    {
        /// <summary>
        /// 模块系统引导器引用
        /// </summary>
        private ModuleBootstrap _bootstrap;
        
        /// <summary>
        /// 生命周期容器引用
        /// </summary>
        private ExampleLifetimeScope _lifetimeScope;
        
        /// <summary>
        /// 模块管理器引用
        /// </summary>
        private ModuleManager _moduleManager;

        /// <summary>
        /// Unity Awake生命周期
        /// </summary>
        private void Awake()
        {
            // 创建模块系统引导器
            if (FindObjectOfType<ModuleBootstrap>() == null)
            {
                var bootstrapObj = new GameObject("ModuleBootstrap");
                _bootstrap = bootstrapObj.AddComponent<ModuleBootstrap>();
                DontDestroyOnLoad(bootstrapObj);
            }
            
            // 创建生命周期容器
            if (FindObjectOfType<ExampleLifetimeScope>() == null)
            {
                var scopeObj = new GameObject("ExampleLifetimeScope");
                _lifetimeScope = scopeObj.AddComponent<ExampleLifetimeScope>();
                
                // 添加模块更新器
                var updaterObj = new GameObject("ModuleUpdater");
                updaterObj.transform.SetParent(scopeObj.transform);
                var updater = updaterObj.AddComponent<ModuleUpdater>();
                
                // 设置引用 - 修正字段名称为moduleUpdater
                var field = _lifetimeScope.GetType().GetField("moduleUpdater", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                
                if (field != null)
                {
                    field.SetValue(_lifetimeScope, updater);
                }
                else
                {
                    Debug.LogError("无法找到moduleUpdater字段，请检查ExampleLifetimeScope类中的字段名称");
                }
            }
            
            Debug.Log("示例场景初始化完成");
        }

        /// <summary>
        /// Unity Start生命周期
        /// </summary>
        private void Start()
        {
            // 获取模块管理器
            _moduleManager = _lifetimeScope?.Container?.Resolve<ModuleManager>();
            
            if (_moduleManager != null)
            {
                Debug.Log("成功获取模块管理器");
                
                // 显示所有注册的模块
                DisplayRegisteredModules();
            }
            else
            {
                Debug.LogError("无法获取模块管理器");
            }
        }

        /// <summary>
        /// 显示所有注册的模块
        /// </summary>
        private void DisplayRegisteredModules()
        {
            // 使用反射获取所有已注册的模块
            var field = typeof(ModuleManager).GetField("_modules", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            
            if (field != null)
            {
                var modules = field.GetValue(_moduleManager) as System.Collections.Generic.List<IModule>;
                if (modules != null && modules.Count > 0)
                {
                    Debug.Log($"已注册 {modules.Count} 个模块:");
                    foreach (var module in modules)
                    {
                        Debug.Log($"- [{module.Priority}] {module.ModuleName} ({module.GetType().Name})");
                    }
                }
                else
                {
                    Debug.Log("没有注册的模块");
                }
            }
        }
    }
}
