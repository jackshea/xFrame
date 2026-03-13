using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using xFrame.Runtime.DI;
using xFrame.Runtime.Startup;
using xFrame.Runtime.UI;

namespace xFrame.Runtime.Unity.Startup
{
    /// <summary>
    ///     Unity 宿主下的启动装配根。
    ///     仅负责桥接 Unity 对象与启动运行时所需的最小依赖。
    /// </summary>
    public sealed class UnityStartupCompositionRoot : IStartupCompositionRoot
    {
        private readonly Dictionary<Type, object> _localServices = new();
        private readonly bool _dontDestroyOnLoad;
        private readonly LifetimeScope _lifetimeScopePrefab;
        private readonly Transform _rootTransform;

        public UnityStartupCompositionRoot(
            Transform rootTransform,
            LifetimeScope lifetimeScopePrefab,
            bool dontDestroyOnLoad)
        {
            _rootTransform = rootTransform;
            _lifetimeScopePrefab = lifetimeScopePrefab;
            _dontDestroyOnLoad = dontDestroyOnLoad;
        }

        /// <summary>
        ///     当前绑定的 DI 容器。
        /// </summary>
        public LifetimeScope LifetimeScope { get; private set; }

        /// <summary>
        ///     注册仅在当前启动运行时可见的本地服务。
        /// </summary>
        public void RegisterLocalService<T>(T service) where T : class
        {
            if (service == null) throw new ArgumentNullException(nameof(service));

            _localServices[typeof(T)] = service;
        }

        public void EnsureInitialized()
        {
            if (LifetimeScope != null) return;

            EnsureLifetimeScope();
            UIEventSystemUtility.EnsureEventSystem(_rootTransform);
        }

        private void EnsureLifetimeScope()
        {
            var loadedSceneScopes = Resources.FindObjectsOfTypeAll<LifetimeScope>()
                .Where(scope => scope != null && scope.gameObject.scene.IsValid())
                .ToArray();

            if (loadedSceneScopes.Length > 1)
            {
                var scopeNames = string.Join(", ", loadedSceneScopes.Select(scope => scope.gameObject.name));
                throw new InvalidOperationException($"启动场景中检测到多个 LifetimeScope: {scopeNames}");
            }

            LifetimeScope = loadedSceneScopes.Length == 1 ? loadedSceneScopes[0] : null;
            if (LifetimeScope == null)
            {
                if (_lifetimeScopePrefab != null)
                {
                    LifetimeScope = UnityEngine.Object.Instantiate(_lifetimeScopePrefab);
                    LifetimeScope.name = _lifetimeScopePrefab.name;
                }
                else
                {
                    var scopeObject = new GameObject(nameof(xFrameLifetimeScope));
                    LifetimeScope = scopeObject.AddComponent<xFrameLifetimeScope>();
                }
            }

            if (LifetimeScope != null && LifetimeScope.Container == null)
                LifetimeScope.Build();

            if (_dontDestroyOnLoad && LifetimeScope != null)
                UnityEngine.Object.DontDestroyOnLoad(LifetimeScope.gameObject);
        }

        public T Resolve<T>() where T : class
        {
            EnsureInitialized();
            if (_localServices.TryGetValue(typeof(T), out var localService))
                return localService as T;

            if (LifetimeScope?.Container == null)
                throw new InvalidOperationException("启动容器尚未构建完成。");

            return LifetimeScope.Container.Resolve<T>();
        }

        public bool TryResolve<T>(out T service) where T : class
        {
            EnsureInitialized();
            if (_localServices.TryGetValue(typeof(T), out var localService) && localService is T typedService)
            {
                service = typedService;
                return true;
            }

            if (LifetimeScope?.Container == null)
            {
                service = default;
                return false;
            }

            return LifetimeScope.Container.TryResolve(out service);
        }
    }
}
