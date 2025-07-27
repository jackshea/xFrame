using System;

namespace xFrame.Core.EventBus
{
    /// <summary>
    /// 事件基类
    /// 提供事件的基础实现
    /// </summary>
    public abstract class BaseEvent : IEvent
    {
        /// <summary>
        /// 事件唯一标识符
        /// </summary>
        public string EventId { get; private set; }
        
        /// <summary>
        /// 事件时间戳
        /// </summary>
        public long Timestamp { get; private set; }
        
        /// <summary>
        /// 事件是否已被处理
        /// </summary>
        public bool IsHandled { get; set; }
        
        /// <summary>
        /// 事件是否被取消
        /// </summary>
        public bool IsCancelled { get; set; }
        
        /// <summary>
        /// 事件优先级（数值越小优先级越高）
        /// </summary>
        public virtual int Priority => 0;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        protected BaseEvent()
        {
            EventId = Guid.NewGuid().ToString();
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            IsHandled = false;
            IsCancelled = false;
        }
        
        /// <summary>
        /// 构造函数（指定事件ID）
        /// </summary>
        /// <param name="eventId">事件ID</param>
        protected BaseEvent(string eventId)
        {
            EventId = eventId ?? Guid.NewGuid().ToString();
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            IsHandled = false;
            IsCancelled = false;
        }
        
        /// <summary>
        /// 重置事件状态（用于对象池复用）
        /// </summary>
        public virtual void Reset()
        {
            EventId = Guid.NewGuid().ToString();
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            IsHandled = false;
            IsCancelled = false;
        }
        
        /// <summary>
        /// 获取事件类型名称
        /// </summary>
        /// <returns>事件类型名称</returns>
        public string GetEventTypeName()
        {
            return GetType().Name;
        }
        
        /// <summary>
        /// 转换为字符串表示
        /// </summary>
        /// <returns>字符串表示</returns>
        public override string ToString()
        {
            return $"{GetEventTypeName()}[{EventId}] - Timestamp: {Timestamp}, Handled: {IsHandled}, Cancelled: {IsCancelled}";
        }
    }
    
    /// <summary>
    /// 泛型事件基类
    /// </summary>
    /// <typeparam name="T">事件数据类型</typeparam>
    public class BaseEvent<T> : BaseEvent, IEvent<T>
    {
        /// <summary>
        /// 事件数据
        /// </summary>
        public T Data { get; set; }
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public BaseEvent() : base()
        {
        }
        
        /// <summary>
        /// 构造函数（指定数据）
        /// </summary>
        /// <param name="data">事件数据</param>
        public BaseEvent(T data) : base()
        {
            Data = data;
        }
        
        /// <summary>
        /// 构造函数（指定事件ID和数据）
        /// </summary>
        /// <param name="eventId">事件ID</param>
        /// <param name="data">事件数据</param>
        public BaseEvent(string eventId, T data) : base(eventId)
        {
            Data = data;
        }
        
        /// <summary>
        /// 重置事件状态
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            Data = default(T);
        }
        
        /// <summary>
        /// 转换为字符串表示
        /// </summary>
        /// <returns>字符串表示</returns>
        public override string ToString()
        {
            return $"{base.ToString()}, Data: {Data}";
        }
    }
}
