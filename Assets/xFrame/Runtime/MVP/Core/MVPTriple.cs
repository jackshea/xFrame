using Cysharp.Threading.Tasks;

namespace xFrame.MVP
{
    /// <summary>
    /// MVP三元组泛型实现
    /// 封装Model、View、Presenter的组合
    /// </summary>
    public class MVPTriple<TModel, TView, TPresenter> : IMVPTriple
        where TModel : class, IModel
        where TView : class, IView
        where TPresenter : class, IPresenter
    {
        public IModel Model => TypedModel;
        public IView View => TypedView;
        public IPresenter Presenter => TypedPresenter;
        
        public TModel TypedModel { get; private set; }
        public TView TypedView { get; private set; }
        public TPresenter TypedPresenter { get; private set; }
        
        public bool IsActive => Presenter?.IsActive ?? false;
        
        public MVPTriple(TModel model, TView view, TPresenter presenter)
        {
            TypedModel = model;
            TypedView = view;
            TypedPresenter = presenter;
        }
        
        public async UniTask ShowAsync()
        {
            if (Presenter != null)
            {
                await Presenter.ShowAsync();
            }
        }
        
        public async UniTask HideAsync()
        {
            if (Presenter != null)
            {
                await Presenter.HideAsync();
            }
        }
        
        public void Dispose()
        {
            Presenter?.Dispose();
            View?.Dispose();
            Model?.Dispose();
            
            TypedPresenter = null;
            TypedView = null;
            TypedModel = null;
        }
    }
}
