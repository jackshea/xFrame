using System;
using Cysharp.Threading.Tasks;

namespace xFrame.MVP
{
    /// <summary>
    /// MVP模式中的Presenter接口
    /// 负责协调View和Model的交互
    /// </summary>
    public interface IPresenter : IDisposable
    {
        /// <summary>
        /// Presenter是否已激活
        /// </summary>
        bool IsActive { get; }
        
        /// <summary>
        /// 初始化Presenter
        /// </summary>
        UniTask InitializeAsync();
        
        /// <summary>
        /// 绑定View和Model
        /// </summary>
        UniTask BindAsync(IView view, IModel model);
        
        /// <summary>
        /// 解绑View和Model
        /// </summary>
        UniTask UnbindAsync();
        
        /// <summary>
        /// 显示
        /// </summary>
        UniTask ShowAsync();
        
        /// <summary>
        /// 隐藏
        /// </summary>
        UniTask HideAsync();
    }
}
