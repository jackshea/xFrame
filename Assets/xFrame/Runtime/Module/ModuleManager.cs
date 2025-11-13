using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace xFrame.Runtime
{
    /// <summary>
    /// 模块管理器
    /// 负责管理所有模块的生命周期、初始化顺序和依赖关系
    /// </summary>
    public class ModuleManager : IStartable, IDisposable
    {
        private readonly IObjectResolver _container;
        private readonly List<IModule> _modules = new();
        private bool _initialized;
        private bool _started;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="builder">VContainer 依赖注入构建器</param>
        /// <param name="container">VContainer依赖注入容器</param>
        public ModuleManager(IObjectResolver container)
        {
            _container = container;
        }

        /// <summary>
        /// VContainer生命周期销毁方法
        /// </summary>
        public void Dispose()
        {
            // 按优先级反序销毁所有模块
            var modulesToDispose = new List<IModule>(_modules);
            modulesToDispose.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            foreach (var module in modulesToDispose)
                try
                {
                    module.OnDestroy();
                    Debug.Log($"模块 {module.ModuleName} 销毁成功");
                }
                catch (Exception e)
                {
                    Debug.LogError($"模块 {module.ModuleName} 销毁失败: {e}");
                }

            _modules.Clear();
            _initialized = false;
            _started = false;
            Debug.Log("所有模块已销毁");
        }

        /// <summary>
        /// VContainer生命周期启动方法
        /// </summary>
        public void Start()
        {
            if (!_initialized) InitializeModules();

            StartModules();
        }

        /// <summary>
        /// 注册模块到管理器
        /// </summary>
        /// <typeparam name="T">模块类型，必须实现IModule接口</typeparam>
        public void RegisterModule<T>() where T : IModule
        {
            if (_initialized)
            {
                Debug.LogError("无法注册模块，模块管理器已经初始化");
                return;
            }

            var module = _container.Resolve<T>();
            RegisterModule(module);
        }

        /// <summary>
        /// 注册模块实例到管理器
        /// </summary>
        /// <param name="module">模块实例</param>
        public void RegisterModule(IModule module)
        {
            if (_initialized)
            {
                Debug.LogError("无法注册模块，模块管理器已经初始化");
                return;
            }

            if (_modules.Any(m => m.GetType() == module.GetType()))
            {
                Debug.LogWarning($"模块 {module.ModuleName} 已经注册，将被忽略");
                return;
            }

            if (module is BaseModule baseModule) baseModule.SetContainer(_container);

            _modules.Add(module);
            Debug.Log($"模块 {module.ModuleName} 已注册");
        }

        /// <summary>
        /// 初始化所有模块
        /// 按照优先级顺序进行初始化
        /// </summary>
        public void InitializeModules()
        {
            if (_initialized)
            {
                Debug.LogWarning("模块管理器已经初始化");
                return;
            }

            Debug.Log("开始初始化所有模块...");

            // 按优先级排序模块
            _modules.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            // 初始化所有模块
            foreach (var module in _modules)
                try
                {
                    module.OnInit();
                    Debug.Log($"模块 {module.ModuleName} 初始化成功");
                }
                catch (Exception e)
                {
                    Debug.LogError($"模块 {module.ModuleName} 初始化失败: {e}");
                }

            _initialized = true;
            Debug.Log("所有模块初始化完成");
        }

        /// <summary>
        /// 启动所有模块
        /// 仅在所有模块初始化完成后调用
        /// </summary>
        public void StartModules()
        {
            if (!_initialized)
            {
                Debug.LogError("无法启动模块，模块管理器尚未初始化");
                return;
            }

            if (_started)
            {
                Debug.LogWarning("模块管理器已经启动");
                return;
            }

            Debug.Log("开始启动所有模块...");

            // 启动所有模块
            foreach (var module in _modules)
                try
                {
                    module.OnStart();
                    Debug.Log($"模块 {module.ModuleName} 启动成功");
                }
                catch (Exception e)
                {
                    Debug.LogError($"模块 {module.ModuleName} 启动失败: {e}");
                }

            _started = true;
            Debug.Log("所有模块启动完成");
        }

        /// <summary>
        /// 获取指定类型的模块
        /// </summary>
        /// <typeparam name="T">模块类型</typeparam>
        /// <returns>指定类型的模块实例，如果不存在则返回默认值</returns>
        public T GetModule<T>() where T : class, IModule
        {
            return _modules.OfType<T>().FirstOrDefault();
        }

        /// <summary>
        /// 获取模块是否存在
        /// </summary>
        /// <typeparam name="T">模块类型</typeparam>
        /// <returns>模块是否存在</returns>
        public bool HasModule<T>() where T : IModule
        {
            return _modules.Any(m => m is T);
        }
    }
}