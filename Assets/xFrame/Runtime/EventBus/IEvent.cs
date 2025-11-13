namespace xFrame.Runtime.EventBus
{
    /// <summary>
    /// 事件基接口
    /// 所有事件都必须实现此接口
    /// </summary>
    public interface IEvent
    {
        /// <summary>
        /// 事件唯一标识符
        /// </summary>
        string EventId { get; }

        /// <summary>
        /// 事件时间戳
        /// </summary>
        long Timestamp { get; }

        /// <summary>
        /// 事件是否已被处理
        /// </summary>
        bool IsHandled { get; set; }

        /// <summary>
        /// 事件是否被取消
        /// </summary>
        bool IsCancelled { get; set; }

        /// <summary>
        /// 事件优先级（数值越小优先级越高）
        /// </summary>
        int Priority { get; }
    }

    /// <summary>
    /// 泛型事件接口
    /// 提供强类型的事件数据
    /// </summary>
    /// <typeparam name="T">事件数据类型</typeparam>
    public interface IEvent<T> : IEvent
    {
        /// <summary>
        /// 事件数据
        /// </summary>
        T Data { get; set; }
    }
}