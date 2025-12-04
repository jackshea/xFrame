namespace xFrame.Runtime.UI
{
    /// <summary>
    /// UI面板基类
    /// 代表一个完整的UI界面，如主菜单、背包、设置等
    /// </summary>
    public abstract class UIPanel : UIView
    {
        /// <summary>
        /// 是否支持栈式管理（Back键返回）
        /// 当为true时，打开此面板会将其压入导航栈
        /// </summary>
        public virtual bool UseStack => true;

        /// <summary>
        /// 打开时是否关闭其他同层UI
        /// 当为true时，打开此面板会关闭同层级的其他所有UI
        /// </summary>
        public virtual bool CloseOthers => false;

        /// <summary>
        /// 是否为全屏UI
        /// 全屏UI会阻挡下层的交互
        /// </summary>
        public virtual bool IsFullScreen => false;

        /// <summary>
        /// 面板动画持续时间（秒）
        /// </summary>
        protected virtual float AnimationDuration => 0.3f;

        /// <summary>
        /// 面板打开动画
        /// 默认无动画，子类可重写实现自定义动画
        /// </summary>
        protected virtual void PlayOpenAnimation()
        {
            // 子类可重写实现动画
        }

        /// <summary>
        /// 面板关闭动画
        /// 默认无动画，子类可重写实现自定义动画
        /// </summary>
        protected virtual void PlayCloseAnimation()
        {
            // 子类可重写实现动画
        }

        /// <summary>
        /// 打开时调用动画
        /// </summary>
        protected override void OnOpen(object data)
        {
            base.OnOpen(data);
            PlayOpenAnimation();
        }

        /// <summary>
        /// 关闭时调用动画
        /// </summary>
        protected override void OnClose()
        {
            PlayCloseAnimation();
            base.OnClose();
        }
    }
}