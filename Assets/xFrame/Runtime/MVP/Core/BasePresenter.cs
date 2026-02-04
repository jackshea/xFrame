using Cysharp.Threading.Tasks;
using VContainer;
using xFrame.Runtime.Logging;

namespace xFrame.MVP
{
    /// <summary>
    /// Presenter基类
    /// 提供通用的逻辑协调功能
    /// </summary>
    public abstract class BasePresenter : IPresenter
    {
        protected IView View { get; private set; }
        protected IModel Model { get; private set; }
        protected IXLogger Logger { get; private set; }
        
        public bool IsActive { get; private set; }
        
        [Inject]
        public void Construct(IXLogger logger)
        {
            Logger = logger;
        }
        
        public virtual async UniTask InitializeAsync()
        {
            Logger.Info($"{GetType().Name} initialized");
            await UniTask.CompletedTask;
        }
        
        public virtual async UniTask BindAsync(IView view, IModel model)
        {
            View = view;
            Model = model;
            
            View.BindPresenter(this);
            Model.OnDataChanged += OnModelDataChanged;
            
            await OnBindAsync();
            IsActive = true;
            
            Logger.Info($"{GetType().Name} bound to View and Model");
        }
        
        public virtual async UniTask UnbindAsync()
        {
            IsActive = false;
            
            await OnUnbindAsync();
            
            if (Model != null)
            {
                Model.OnDataChanged -= OnModelDataChanged;
            }
            
            View?.UnbindPresenter();
            
            View = null;
            Model = null;
            
            Logger.Info($"{GetType().Name} unbound from View and Model");
        }
        
        public virtual async UniTask ShowAsync()
        {
            if (View != null)
            {
                await View.ShowAsync();
                await OnShowAsync();
            }
        }
        
        public virtual async UniTask HideAsync()
        {
            if (View != null)
            {
                await OnHideAsync();
                await View.HideAsync();
            }
        }
        
        protected abstract UniTask OnBindAsync();
        protected abstract UniTask OnUnbindAsync();
        protected abstract UniTask OnShowAsync();
        protected abstract UniTask OnHideAsync();
        protected abstract void OnModelDataChanged(IModel model);
        
        public virtual void Dispose()
        {
            if (IsActive)
            {
                UnbindAsync().Forget();
            }
            Logger.Info($"{GetType().Name} disposed");
        }
    }
}
