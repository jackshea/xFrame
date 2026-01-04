using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using xFrame.Runtime.Logging;
using Object = UnityEngine.Object;

namespace xFrame.MVP
{
    /// <summary>
    /// MVP View工厂实现
    /// 负责View的创建和资源管理
    /// </summary>
    public class MVPViewFactory : IMVPViewFactory
    {
        private readonly IObjectResolver _container;
        private readonly IXLogger _logger;
        
        public MVPViewFactory(IObjectResolver container, IXLogger logger)
        {
            _container = container;
            _logger = logger;
        }
        
        public async UniTask<TView> CreateViewAsync<TView>(string assetKey) 
            where TView : class, IView
        {
            try
            {
                var prefab = await Resources.LoadAsync<GameObject>(assetKey);
                if (prefab == null)
                {
                    _logger.Error($"Failed to load view prefab: {assetKey}");
                    return null;
                }
                
                var gameObject = Object.Instantiate(prefab as GameObject);
                var view = gameObject.GetComponent<TView>();
                
                if (view == null)
                {
                    _logger.Error($"View component not found on prefab: {assetKey}");
                    Object.Destroy(gameObject);
                    return null;
                }
                
                _container.Inject(view);
                
                _logger.Info($"View created: {typeof(TView).Name}");
                
                return view;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to create view: {ex.Message}");
                throw;
            }
        }
        
        public async UniTask DestroyViewAsync<TView>(TView view) 
            where TView : class, IView
        {
            if (view == null)
            {
                _logger.Warning("Attempted to destroy null view");
                return;
            }
            
            try
            {
                view.Dispose();
                
                if (view is MonoBehaviour monoBehaviour)
                {
                    Object.Destroy(monoBehaviour.gameObject);
                }
                
                _logger.Info($"View destroyed: {typeof(TView).Name}");
                
                await UniTask.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to destroy view: {ex.Message}");
                throw;
            }
        }
    }
}
