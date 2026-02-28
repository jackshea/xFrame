using VContainer;

namespace xFrame.Runtime.MVVM
{
    /// <summary>
    /// MVVM 模块容器扩展。
    /// </summary>
    public static class MVVMContainerExtensions
    {
        /// <summary>
        /// 注册 MVVM 模块基础服务。
        /// 预留统一入口，后续可在此扩展绑定服务或工厂。
        /// </summary>
        public static void RegisterMVVMModule(this IContainerBuilder builder)
        {
        }

        /// <summary>
        /// 注册 MVVM 组合类型。
        /// </summary>
        public static void RegisterMVVM<TModel, TViewModel>(this IContainerBuilder builder, Lifetime lifetime = Lifetime.Transient)
            where TModel : class
            where TViewModel : class
        {
            builder.Register<TModel>(lifetime);
            builder.Register<TViewModel>(lifetime);
        }
    }
}
