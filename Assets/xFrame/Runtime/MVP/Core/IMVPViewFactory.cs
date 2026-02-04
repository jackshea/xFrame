using Cysharp.Threading.Tasks;

namespace xFrame.MVP
{
    /// <summary>
    /// MVP View工厂接口
    /// 负责View的创建和资源管理
    /// </summary>
    public interface IMVPViewFactory
    {
        /// <summary>
        /// 创建View实例
        /// </summary>
        UniTask<TView> CreateViewAsync<TView>(string assetKey) 
            where TView : class, IView;
        
        /// <summary>
        /// 销毁View实例
        /// </summary>
        UniTask DestroyViewAsync<TView>(TView view) 
            where TView : class, IView;
    }
}
