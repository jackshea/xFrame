using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using VContainer;
using xFrame.Runtime.Logging;

namespace xFrame.MVP
{
    /// <summary>
    /// MVP管理器实现
    /// 负责MVP三元组的创建、管理和销毁
    /// </summary>
    public class MVPManager : IMVPManager
    {
        private readonly IObjectResolver _container;
        private readonly IXLogger _logger;
        private readonly Dictionary<Type, IMVPTriple> _activeMVPs;
        
        public MVPManager(IObjectResolver container, IXLogger logger)
        {
            _container = container;
            _logger = logger;
            _activeMVPs = new Dictionary<Type, IMVPTriple>();
        }
        
        public async UniTask<TMVPTriple> CreateMVPAsync<TMVPTriple, TModel, TView, TPresenter>()
            where TMVPTriple : class, IMVPTriple
            where TModel : class, IModel
            where TView : class, IView
            where TPresenter : class, IPresenter
        {
            try
            {
                var model = _container.Resolve<TModel>();
                var view = _container.Resolve<TView>();
                var presenter = _container.Resolve<TPresenter>();
                
                await model.InitializeAsync();
                await presenter.InitializeAsync();
                
                await presenter.BindAsync(view, model);
                
                var mvpTriple = Activator.CreateInstance(typeof(TMVPTriple), model, view, presenter) as TMVPTriple;
                
                _activeMVPs[typeof(TMVPTriple)] = mvpTriple;
                
                _logger.Info($"MVP created: {typeof(TMVPTriple).Name}");
                
                return mvpTriple;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to create MVP: {ex.Message}");
                throw;
            }
        }
        
        public async UniTask DestroyMVPAsync<TMVPTriple>(TMVPTriple mvpTriple)
            where TMVPTriple : class, IMVPTriple
        {
            if (mvpTriple == null)
            {
                _logger.Warning("Attempted to destroy null MVP");
                return;
            }
            
            try
            {
                if (mvpTriple.IsActive)
                {
                    await mvpTriple.HideAsync();
                }
                
                mvpTriple.Dispose();
                
                _activeMVPs.Remove(typeof(TMVPTriple));
                
                _logger.Info($"MVP destroyed: {typeof(TMVPTriple).Name}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to destroy MVP: {ex.Message}");
                throw;
            }
        }
        
        public TMVPTriple GetActiveMVP<TMVPTriple>()
            where TMVPTriple : class, IMVPTriple
        {
            if (_activeMVPs.TryGetValue(typeof(TMVPTriple), out var mvp))
            {
                return mvp as TMVPTriple;
            }
            
            return null;
        }
    }
}
