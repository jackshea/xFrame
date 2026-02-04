using VContainer;

namespace xFrame.MVP
{
    /// <summary>
    /// MVP模块的VContainer注册扩展
    /// </summary>
    public static class MVPContainerExtensions
    {
        /// <summary>
        /// 注册MVP模块
        /// </summary>
        public static void RegisterMVPModule(this IContainerBuilder builder)
        {
            builder.Register<IMVPManager, MVPManager>(Lifetime.Singleton);
        }
        
        /// <summary>
        /// 注册MVP三元组
        /// </summary>
        public static void RegisterMVP<TModel, TView, TPresenter, TMVPTriple>(
            this IContainerBuilder builder)
            where TModel : class, IModel
            where TView : class, IView
            where TPresenter : class, IPresenter
            where TMVPTriple : class, IMVPTriple
        {
            builder.Register<TModel>(Lifetime.Transient);
            builder.Register<TView>(Lifetime.Transient);
            builder.Register<TPresenter>(Lifetime.Transient);
            builder.Register<TMVPTriple>(Lifetime.Transient);
        }
        
        /// <summary>
        /// 注册MVP三元组（使用默认MVPTriple实现）
        /// </summary>
        public static void RegisterMVP<TModel, TView, TPresenter>(
            this IContainerBuilder builder)
            where TModel : class, IModel
            where TView : class, IView
            where TPresenter : class, IPresenter
        {
            builder.Register<TModel>(Lifetime.Transient);
            builder.Register<TView>(Lifetime.Transient);
            builder.Register<TPresenter>(Lifetime.Transient);
        }
    }
}
