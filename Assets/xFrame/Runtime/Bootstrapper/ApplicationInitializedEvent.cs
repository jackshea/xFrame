using xFrame.Runtime.EventBus;

namespace xFrame.Runtime
{
    /// <summary>
    /// 应用程序初始化完成事件
    /// </summary>
    public struct ApplicationInitializedEvent : IEvent
    {
        /// <summary>
        /// 应用程序实例
        /// </summary>
        public xFrameApplication Application { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="application">应用程序实例</param>
        public ApplicationInitializedEvent(xFrameApplication application)
        {
            Application = application;
        }
    }
}
