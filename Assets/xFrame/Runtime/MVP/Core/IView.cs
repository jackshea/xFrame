using System;
using Cysharp.Threading.Tasks;

namespace xFrame.MVP
{
    /// <summary>
    /// MVP模式中的View接口
    /// 负责UI显示和用户交互
    /// </summary>
    public interface IView : IDisposable
    {
        /// <summary>
        /// View是否已激活
        /// </summary>
        bool IsActive { get; }
        
        /// <summary>
        /// 显示View
        /// </summary>
        UniTask ShowAsync();
        
        /// <summary>
        /// 隐藏View
        /// </summary>
        UniTask HideAsync();
        
        /// <summary>
        /// 绑定Presenter
        /// </summary>
        void BindPresenter(IPresenter presenter);
        
        /// <summary>
        /// 解绑Presenter
        /// </summary>
        void UnbindPresenter();
    }
}
