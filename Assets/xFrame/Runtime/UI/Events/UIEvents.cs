using System;
using xFrame.Runtime.EventBus;

namespace xFrame.Runtime.UI.Events
{
    /// <summary>
    /// UI打开事件
    /// 当UI成功打开时触发
    /// </summary>
    public struct UIOpenedEvent : IEvent
    {
        /// <summary>
        /// 打开的UI类型
        /// </summary>
        public Type UIType { get; set; }

        /// <summary>
        /// 打开的UI实例
        /// </summary>
        public UIView View { get; set; }

        /// <summary>
        /// UI所在层级
        /// </summary>
        public UILayer Layer { get; set; }

        public UIOpenedEvent(Type uiType, UIView view, UILayer layer)
        {
            UIType = uiType;
            View = view;
            Layer = layer;
        }
    }

    /// <summary>
    /// UI关闭事件
    /// 当UI被关闭时触发
    /// </summary>
    public struct UIClosedEvent : IEvent
    {
        /// <summary>
        /// 关闭的UI类型
        /// </summary>
        public Type UIType { get; set; }

        /// <summary>
        /// UI所在层级
        /// </summary>
        public UILayer Layer { get; set; }

        public UIClosedEvent(Type uiType, UILayer layer)
        {
            UIType = uiType;
            Layer = layer;
        }
    }

    /// <summary>
    /// UI层级改变事件
    /// 当某个层级的UI数量发生变化时触发
    /// </summary>
    public struct UILayerChangedEvent : IEvent
    {
        /// <summary>
        /// 发生变化的层级
        /// </summary>
        public UILayer Layer { get; set; }

        /// <summary>
        /// 当前该层级激活的UI数量
        /// </summary>
        public int ActiveCount { get; set; }

        public UILayerChangedEvent(UILayer layer, int activeCount)
        {
            Layer = layer;
            ActiveCount = activeCount;
        }
    }

    /// <summary>
    /// UI加载开始事件
    /// 当开始异步加载UI时触发
    /// </summary>
    public struct UILoadStartEvent : IEvent
    {
        /// <summary>
        /// 正在加载的UI类型
        /// </summary>
        public Type UIType { get; set; }

        public UILoadStartEvent(Type uiType)
        {
            UIType = uiType;
        }
    }

    /// <summary>
    /// UI加载完成事件
    /// 当UI异步加载完成时触发
    /// </summary>
    public struct UILoadCompleteEvent : IEvent
    {
        /// <summary>
        /// 加载完成的UI类型
        /// </summary>
        public Type UIType { get; set; }

        /// <summary>
        /// 是否加载成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 加载耗时（秒）
        /// </summary>
        public float Duration { get; set; }

        public UILoadCompleteEvent(Type uiType, bool success, float duration)
        {
            UIType = uiType;
            Success = success;
            Duration = duration;
        }
    }

    /// <summary>
    /// UI导航事件
    /// 当通过导航栈切换UI时触发
    /// </summary>
    public struct UINavigationEvent : IEvent
    {
        /// <summary>
        /// 前一个UI类型（可能为null）
        /// </summary>
        public Type FromUIType { get; set; }

        /// <summary>
        /// 目标UI类型
        /// </summary>
        public Type ToUIType { get; set; }

        /// <summary>
        /// 导航类型（前进/后退）
        /// </summary>
        public NavigationType Type { get; set; }

        public UINavigationEvent(Type fromUIType, Type toUIType, NavigationType type)
        {
            FromUIType = fromUIType;
            ToUIType = toUIType;
            Type = type;
        }
    }

    /// <summary>
    /// 导航类型
    /// </summary>
    public enum NavigationType
    {
        /// <summary>
        /// 前进到新UI
        /// </summary>
        Forward,

        /// <summary>
        /// 返回到上一个UI
        /// </summary>
        Back
    }
}