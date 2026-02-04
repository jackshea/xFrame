using System;
using Cysharp.Threading.Tasks;

namespace xFrame.MVP
{
    /// <summary>
    /// MVP三元组接口
    /// 封装Model、View、Presenter的组合
    /// </summary>
    public interface IMVPTriple : IDisposable
    {
        /// <summary>
        /// Model实例
        /// </summary>
        IModel Model { get; }
        
        /// <summary>
        /// View实例
        /// </summary>
        IView View { get; }
        
        /// <summary>
        /// Presenter实例
        /// </summary>
        IPresenter Presenter { get; }
        
        /// <summary>
        /// 是否已激活
        /// </summary>
        bool IsActive { get; }
        
        /// <summary>
        /// 显示MVP
        /// </summary>
        UniTask ShowAsync();
        
        /// <summary>
        /// 隐藏MVP
        /// </summary>
        UniTask HideAsync();
    }
}
