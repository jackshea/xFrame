namespace xFrame.Runtime.UI
{
    /// <summary>
    /// UI标签页基类
    /// 既可以独立打开使用（作为UIPanel），也可以作为UITabContainer的子页面
    /// 子页面对是否在容器中是无感知的
    /// </summary>
    public abstract class UITabPage : UIPanel
    {
        /// <summary>
        /// 是否在容器中（内部标记，子类无需关心）
        /// </summary>
        internal bool IsInContainer { get; set; }

        /// <summary>
        /// 所属的容器（如果有）
        /// </summary>
        internal UITabContainer ParentContainer { get; set; }

        /// <summary>
        /// 页面索引（在容器中的索引）
        /// </summary>
        public int PageIndex { get; internal set; } = -1;

        /// <summary>
        /// 页面名称（用于标识）
        /// </summary>
        public virtual string PageName => GetType().Name;

        /// <summary>
        /// 标签页在容器中默认不使用栈管理
        /// 作为独立页面时可以使用栈
        /// </summary>
        public override bool UseStack => !IsInContainer;

        /// <summary>
        /// 页面进入时的回调
        /// 当页面被切换为活动页面时调用
        /// </summary>
        protected virtual void OnPageEnter()
        {
        }

        /// <summary>
        /// 页面退出时的回调
        /// 当页面从活动状态切换到其他页面时调用
        /// </summary>
        protected virtual void OnPageExit()
        {
        }

        /// <summary>
        /// 内部方法：页面进入
        /// 由容器调用
        /// </summary>
        internal void InternalPageEnter()
        {
            OnPageEnter();
        }

        /// <summary>
        /// 内部方法：页面退出
        /// 由容器调用
        /// </summary>
        internal void InternalPageExit()
        {
            OnPageExit();
        }
    }
}