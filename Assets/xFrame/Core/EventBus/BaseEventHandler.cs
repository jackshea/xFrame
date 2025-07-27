using System;
using System.Threading.Tasks;

namespace xFrame.Core.EventBus
{
    /// <summary>
    /// 事件处理器基类
    /// 提供事件处理器的基础实现
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    public abstract class BaseEventHandler<T> : IEventHandler<T> where T : IEvent
    {
        /// <summary>
        /// 处理器优先级（数值越小优先级越高）
        /// </summary>
        public virtual int Priority => 0;
        
        /// <summary>
        /// 处理器是否激活
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// 处理器唯一标识
        /// </summary>
        public string HandlerId { get; private set; }
        
        /// <summary>
        /// 构造函数
        /// </summary>
        protected BaseEventHandler()
        {
            HandlerId = Guid.NewGuid().ToString();
        }
        
        /// <summary>
        /// 构造函数（指定处理器ID）
        /// </summary>
        /// <param name="handlerId">处理器ID</param>
        protected BaseEventHandler(string handlerId)
        {
            HandlerId = handlerId ?? Guid.NewGuid().ToString();
        }
        
        /// <summary>
        /// 处理事件
        /// </summary>
        /// <param name="eventData">事件数据</param>
        public abstract void Handle(T eventData);
        
        /// <summary>
        /// 判断是否应该处理该事件（可重写以添加自定义过滤逻辑）
        /// </summary>
        /// <param name="eventData">事件数据</param>
        /// <returns>true表示应该处理，false表示跳过</returns>
        public virtual bool ShouldHandle(T eventData)
        {
            return IsActive && !eventData.IsCancelled;
        }
        
        /// <summary>
        /// 事件处理前的回调（可重写）
        /// </summary>
        /// <param name="eventData">事件数据</param>
        protected virtual void OnBeforeHandle(T eventData)
        {
        }
        
        /// <summary>
        /// 事件处理后的回调（可重写）
        /// </summary>
        /// <param name="eventData">事件数据</param>
        protected virtual void OnAfterHandle(T eventData)
        {
        }
        
        /// <summary>
        /// 事件处理异常时的回调（可重写）
        /// </summary>
        /// <param name="eventData">事件数据</param>
        /// <param name="exception">异常信息</param>
        protected virtual void OnException(T eventData, Exception exception)
        {
            // 默认重新抛出异常
            throw exception;
        }
        
        /// <summary>
        /// 安全处理事件（包含异常处理）
        /// </summary>
        /// <param name="eventData">事件数据</param>
        internal void SafeHandle(T eventData)
        {
            if (!ShouldHandle(eventData))
                return;
                
            try
            {
                OnBeforeHandle(eventData);
                Handle(eventData);
                OnAfterHandle(eventData);
            }
            catch (Exception ex)
            {
                OnException(eventData, ex);
            }
        }
        
        /// <summary>
        /// 转换为字符串表示
        /// </summary>
        /// <returns>字符串表示</returns>
        public override string ToString()
        {
            return $"{GetType().Name}[{HandlerId}] - Priority: {Priority}, Active: {IsActive}";
        }
    }
    
    /// <summary>
    /// 异步事件处理器基类
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    public abstract class BaseAsyncEventHandler<T> : IAsyncEventHandler<T> where T : IEvent
    {
        /// <summary>
        /// 处理器优先级（数值越小优先级越高）
        /// </summary>
        public virtual int Priority => 0;
        
        /// <summary>
        /// 处理器是否激活
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// 处理器唯一标识
        /// </summary>
        public string HandlerId { get; private set; }
        
        /// <summary>
        /// 构造函数
        /// </summary>
        protected BaseAsyncEventHandler()
        {
            HandlerId = Guid.NewGuid().ToString();
        }
        
        /// <summary>
        /// 构造函数（指定处理器ID）
        /// </summary>
        /// <param name="handlerId">处理器ID</param>
        protected BaseAsyncEventHandler(string handlerId)
        {
            HandlerId = handlerId ?? Guid.NewGuid().ToString();
        }
        
        /// <summary>
        /// 异步处理事件
        /// </summary>
        /// <param name="eventData">事件数据</param>
        /// <returns>处理任务</returns>
        public abstract Task HandleAsync(T eventData);
        
        /// <summary>
        /// 判断是否应该处理该事件（可重写以添加自定义过滤逻辑）
        /// </summary>
        /// <param name="eventData">事件数据</param>
        /// <returns>true表示应该处理，false表示跳过</returns>
        public virtual bool ShouldHandle(T eventData)
        {
            return IsActive && !eventData.IsCancelled;
        }
        
        /// <summary>
        /// 事件处理前的回调（可重写）
        /// </summary>
        /// <param name="eventData">事件数据</param>
        protected virtual Task OnBeforeHandleAsync(T eventData)
        {
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// 事件处理后的回调（可重写）
        /// </summary>
        /// <param name="eventData">事件数据</param>
        protected virtual Task OnAfterHandleAsync(T eventData)
        {
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// 事件处理异常时的回调（可重写）
        /// </summary>
        /// <param name="eventData">事件数据</param>
        /// <param name="exception">异常信息</param>
        protected virtual Task OnExceptionAsync(T eventData, Exception exception)
        {
            // 默认重新抛出异常
            throw exception;
        }
        
        /// <summary>
        /// 安全异步处理事件（包含异常处理）
        /// </summary>
        /// <param name="eventData">事件数据</param>
        internal async Task SafeHandleAsync(T eventData)
        {
            if (!ShouldHandle(eventData))
                return;
                
            try
            {
                await OnBeforeHandleAsync(eventData);
                await HandleAsync(eventData);
                await OnAfterHandleAsync(eventData);
            }
            catch (Exception ex)
            {
                await OnExceptionAsync(eventData, ex);
            }
        }
        
        /// <summary>
        /// 转换为字符串表示
        /// </summary>
        /// <returns>字符串表示</returns>
        public override string ToString()
        {
            return $"{GetType().Name}[{HandlerId}] - Priority: {Priority}, Active: {IsActive}";
        }
    }
    
    /// <summary>
    /// 委托事件处理器
    /// 用于包装Action委托为事件处理器
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    internal class DelegateEventHandler<T> : BaseEventHandler<T> where T : IEvent
    {
        private readonly Action<T> _handler;
        private readonly int _priority;
        
        /// <summary>
        /// 处理器优先级
        /// </summary>
        public override int Priority => _priority;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="handler">处理委托</param>
        /// <param name="priority">优先级</param>
        public DelegateEventHandler(Action<T> handler, int priority = 0)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            _priority = priority;
        }
        
        /// <summary>
        /// 处理事件
        /// </summary>
        /// <param name="eventData">事件数据</param>
        public override void Handle(T eventData)
        {
            _handler(eventData);
        }
    }
    
    /// <summary>
    /// 委托异步事件处理器
    /// 用于包装Func委托为异步事件处理器
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    internal class DelegateAsyncEventHandler<T> : BaseAsyncEventHandler<T> where T : IEvent
    {
        private readonly Func<T, Task> _handler;
        private readonly int _priority;
        
        /// <summary>
        /// 处理器优先级
        /// </summary>
        public override int Priority => _priority;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="handler">异步处理委托</param>
        /// <param name="priority">优先级</param>
        public DelegateAsyncEventHandler(Func<T, Task> handler, int priority = 0)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            _priority = priority;
        }
        
        /// <summary>
        /// 异步处理事件
        /// </summary>
        /// <param name="eventData">事件数据</param>
        /// <returns>处理任务</returns>
        public override Task HandleAsync(T eventData)
        {
            return _handler(eventData);
        }
    }
}
