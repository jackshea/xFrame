using Cysharp.Threading.Tasks;

namespace xFrame.MVP
{
    /// <summary>
    /// MVP管理器接口
    /// 负责MVP三元组的创建、管理和销毁
    /// </summary>
    public interface IMVPManager
    {
        /// <summary>
        /// 创建MVP三元组
        /// </summary>
        UniTask<TMVPTriple> CreateMVPAsync<TMVPTriple, TModel, TView, TPresenter>()
            where TMVPTriple : class, IMVPTriple
            where TModel : class, IModel
            where TView : class, IView
            where TPresenter : class, IPresenter;
        
        /// <summary>
        /// 销毁MVP三元组
        /// </summary>
        UniTask DestroyMVPAsync<TMVPTriple>(TMVPTriple mvpTriple)
            where TMVPTriple : class, IMVPTriple;
        
        /// <summary>
        /// 获取活跃的MVP三元组
        /// </summary>
        TMVPTriple GetActiveMVP<TMVPTriple>()
            where TMVPTriple : class, IMVPTriple;
    }
}
