using System;

namespace xFrame.Core.EventBus
{
    /// <summary>
    /// 事件处理器基接口
    /// </summary>
    public interface IEventHandler
    {
        /// <summary>
        /// 处理器优先级（数值越小优先级越高）
        /// </summary>
        int Priority { get; }
        
        /// <summary>
        /// 处理器是否激活
        /// </summary>
        bool IsActive { get; set; }
        
        /// <summary>
        /// 处理器唯一标识
        /// </summary>
        string HandlerId { get; }
    }
    
    /// <summary>
    /// 泛型事件处理器接口
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    public interface IEventHandler<in T> : IEventHandler where T : IEvent
    {
        /// <summary>
        /// 处理事件
        /// </summary>
        /// <param name="eventData">事件数据</param>
        void Handle(T eventData);
    }
    
    /// <summary>
    /// 异步事件处理器接口
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    public interface IAsyncEventHandler<in T> : IEventHandler where T : IEvent
    {
        /// <summary>
        /// 异步处理事件
        /// </summary>
        /// <param name="eventData">事件数据</param>
        /// <returns>处理任务</returns>
        System.Threading.Tasks.Task HandleAsync(T eventData);
    }
    
    /// <summary>
    /// 事件过滤器接口
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    public interface IEventFilter<in T> where T : IEvent
    {
        /// <summary>
        /// 判断事件是否应该被处理
        /// </summary>
        /// <param name="eventData">事件数据</param>
        /// <returns>true表示应该处理，false表示过滤掉</returns>
        bool ShouldHandle(T eventData);
    }
    
    /// <summary>
    /// 事件拦截器接口
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    public interface IEventInterceptor<T> where T : IEvent
    {
        /// <summary>
        /// 拦截器优先级（数值越小优先级越高）
        /// </summary>
        int Priority { get; }
        
        /// <summary>
        /// 事件处理前拦截
        /// </summary>
        /// <param name="eventData">事件数据</param>
        /// <returns>true表示继续处理，false表示阻止处理</returns>
        bool OnBeforeHandle(T eventData);
        
        /// <summary>
        /// 事件处理后拦截
        /// </summary>
        /// <param name="eventData">事件数据</param>
        void OnAfterHandle(T eventData);
        
        /// <summary>
        /// 事件处理异常时拦截
        /// </summary>
        /// <param name="eventData">事件数据</param>
        /// <param name="exception">异常信息</param>
        /// <returns>true表示异常已处理，false表示继续抛出异常</returns>
        bool OnException(T eventData, Exception exception);
    }
}
