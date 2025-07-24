using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace xFrame.Core
{
    /// <summary>
    /// 可更新模块接口
    /// 需要每帧更新的模块应实现此接口
    /// </summary>
    public interface IUpdatableModule : IModule
    {
        /// <summary>
        /// 模块更新
        /// 每帧调用一次，用于处理模块的持续性逻辑
        /// </summary>
        void OnUpdate();
    }

    /// <summary>
    /// 模块更新器
    /// 负责管理和调用所有实现了IUpdatableModule接口的模块的OnUpdate方法
    /// </summary>
    public class ModuleUpdater : MonoBehaviour
    {
        private List<IUpdatableModule> _updatableModules = new List<IUpdatableModule>();
        private IObjectResolver _container;
        private ModuleManager _moduleManager;
        private bool _initialized = false;

        /// <summary>
        /// 初始化模块更新器
        /// </summary>
        /// <param name="container">VContainer依赖注入容器</param>
        public void Initialize(IObjectResolver container)
        {
            if (_initialized)
            {
                Debug.LogWarning("模块更新器已经初始化");
                return;
            }

            _container = container;
            _moduleManager = _container.Resolve<ModuleManager>();
            _initialized = true;
            
            CollectUpdatableModules();
        }

        /// <summary>
        /// 收集所有可更新的模块
        /// </summary>
        private void CollectUpdatableModules()
        {
            _updatableModules.Clear();
            
            // 通过反射获取所有已注册的可更新模块
            var field = typeof(ModuleManager).GetField("_modules", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            
            if (field != null)
            {
                var modules = field.GetValue(_moduleManager) as List<IModule>;
                if (modules != null)
                {
                    foreach (var module in modules)
                    {
                        if (module is IUpdatableModule updatableModule)
                        {
                            _updatableModules.Add(updatableModule);
                            Debug.Log($"收集到可更新模块: {module.ModuleName}");
                        }
                    }
                }
            }
            
            Debug.Log($"共收集到 {_updatableModules.Count} 个可更新模块");
        }

        /// <summary>
        /// Unity Update生命周期方法
        /// 每帧调用一次，负责更新所有可更新模块
        /// </summary>
        private void Update()
        {
            if (!_initialized || _updatableModules.Count == 0)
                return;

            // 更新所有可更新模块
            for (int i = 0; i < _updatableModules.Count; i++)
            {
                try
                {
                    _updatableModules[i].OnUpdate();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"模块 {_updatableModules[i].ModuleName} 更新失败: {e}");
                }
            }
        }
    }
}
