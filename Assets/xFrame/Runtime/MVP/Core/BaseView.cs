using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using xFrame.Runtime.Logging;

namespace xFrame.MVP
{
    /// <summary>
    /// View基类
    /// 提供通用的UI管理功能
    /// </summary>
    public abstract class BaseView : MonoBehaviour, IView
    {
        protected IPresenter Presenter { get; private set; }
        protected IXLogger Logger { get; private set; }
        
        public bool IsActive => gameObject.activeInHierarchy;
        
        [Inject]
        public void Construct(IXLogger logger)
        {
            Logger = logger;
        }
        
        public virtual async UniTask ShowAsync()
        {
            gameObject.SetActive(true);
            await OnShowAsync();
            Logger.Info($"{GetType().Name} shown");
        }
        
        public virtual async UniTask HideAsync()
        {
            await OnHideAsync();
            gameObject.SetActive(false);
            Logger.Info($"{GetType().Name} hidden");
        }
        
        public void BindPresenter(IPresenter presenter)
        {
            Presenter = presenter;
            OnPresenterBound();
        }
        
        public void UnbindPresenter()
        {
            OnPresenterUnbound();
            Presenter = null;
        }
        
        protected abstract UniTask OnShowAsync();
        protected abstract UniTask OnHideAsync();
        protected abstract void OnPresenterBound();
        protected abstract void OnPresenterUnbound();
        
        public virtual void Dispose()
        {
            UnbindPresenter();
            Logger.Info($"{GetType().Name} disposed");
        }
    }
}
