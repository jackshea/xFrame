using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace xFrame.Core.EventBus
{
    /// <summary>
    /// 事件总线接口
    /// 提供事件发布、订阅、取消订阅等核心功能
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理器</param>
        void Subscribe<T>(IEventHandler<T> handler) where T : IEvent;
        
        /// <summary>
        /// 订阅事件（使用委托）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理委托</param>
        /// <param name="priority">处理优先级</param>
        /// <returns>订阅标识，用于取消订阅</returns>
        string Subscribe<T>(Action<T> handler, int priority = 0) where T : IEvent;
        
        /// <summary>
        /// 订阅异步事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">异步事件处理器</param>
        void SubscribeAsync<T>(IAsyncEventHandler<T> handler) where T : IEvent;
        
        /// <summary>
        /// 订阅异步事件（使用委托）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">异步事件处理委托</param>
        /// <param name="priority">处理优先级</param>
        /// <returns>订阅标识，用于取消订阅</returns>
        string SubscribeAsync<T>(Func<T, Task> handler, int priority = 0) where T : IEvent;
        
        /// <summary>
        /// 取消订阅事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理器</param>
        void Unsubscribe<T>(IEventHandler<T> handler) where T : IEvent;
        
        /// <summary>
        /// 取消订阅事件（使用订阅标识）
        /// </summary>
        /// <param name="subscriptionId">订阅标识</param>
        void Unsubscribe(string subscriptionId);
        
        /// <summary>
        /// 取消所有订阅
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        void UnsubscribeAll<T>() where T : IEvent;
        
        /// <summary>
        /// 发布事件（同步）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="eventData">事件数据</param>
        void Publish<T>(T eventData) where T : IEvent;
        
        /// <summary>
        /// 发布事件（异步）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="eventData">事件数据</param>
        /// <returns>发布任务</returns>
        Task PublishAsync<T>(T eventData) where T : IEvent;
        
        /// <summary>
        /// 批量发布事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="events">事件列表</param>
        void PublishBatch<T>(IEnumerable<T> events) where T : IEvent;
        
        /// <summary>
        /// 延迟发布事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="eventData">事件数据</param>
        /// <param name="delay">延迟时间（毫秒）</param>
        void PublishDelayed<T>(T eventData, int delay) where T : IEvent;
        
        /// <summary>
        /// 添加事件过滤器
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="filter">事件过滤器</param>
        void AddFilter<T>(IEventFilter<T> filter) where T : IEvent;
        
        /// <summary>
        /// 移除事件过滤器
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="filter">事件过滤器</param>
        void RemoveFilter<T>(IEventFilter<T> filter) where T : IEvent;
        
        /// <summary>
        /// 添加事件拦截器
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="interceptor">事件拦截器</param>
        void AddInterceptor<T>(IEventInterceptor<T> interceptor) where T : IEvent;
        
        /// <summary>
        /// 移除事件拦截器
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="interceptor">事件拦截器</param>
        void RemoveInterceptor<T>(IEventInterceptor<T> interceptor) where T : IEvent;
        
        /// <summary>
        /// 获取订阅者数量
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <returns>订阅者数量</returns>
        int GetSubscriberCount<T>() where T : IEvent;
        
        /// <summary>
        /// 检查是否有订阅者
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <returns>true表示有订阅者，false表示没有</returns>
        bool HasSubscribers<T>() where T : IEvent;
        
        /// <summary>
        /// 清空所有订阅
        /// </summary>
        void Clear();
        
        /// <summary>
        /// 获取事件历史记录
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="count">获取数量</param>
        /// <returns>事件历史列表</returns>
        IEnumerable<T> GetEventHistory<T>(int count = 10) where T : IEvent;
        
        /// <summary>
        /// 启用或禁用事件历史记录
        /// </summary>
        /// <param name="enabled">是否启用</param>
        void SetHistoryEnabled(bool enabled);
    }
}
