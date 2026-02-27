using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.IO;

namespace xFrame.Runtime.Core.Events
{
    /// <summary>
    /// 核心事件接口 - 与Unity解耦
    /// </summary>
    public interface ICoreEvent
    {
    }

    /// <summary>
    /// 核心事件总线 - 完全与Unity解耦的事件系统
    /// 支持目标源过滤，与GenericEventBus API兼容
    /// </summary>
    public static class CoreEventBus
    {
        private static CoreGenericEventBus _eventBus;

        /// <summary>
        /// 事件总线实例
        /// </summary>
        private static CoreGenericEventBus EventBus
        {
            get
            {
                if (_eventBus == null)
                {
                    _eventBus = new CoreGenericEventBus();
                }
                return _eventBus;
            }
        }

        /// <summary>
        /// 初始化/重置事件总线
        /// </summary>
        public static void Initialize()
        {
            _eventBus?.Dispose();
            _eventBus = new CoreGenericEventBus();
        }

        /// <summary>
        /// 当前触发的事件是否已经被消费
        /// </summary>
        public static bool CurrentEventIsConsumed => EventBus.CurrentEventIsConsumed;

        /// <summary>
        /// 当前是否正在触发某个事件
        /// </summary>
        public static bool IsEventBeingRaised => EventBus.IsEventBeingRaised;

        /// <summary>
        /// 立即触发事件
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RaiseImmediately<TCoreEvent>(TCoreEvent @event) where TCoreEvent : ICoreEvent
        {
            return RaiseImmediately(ref @event);
        }

        /// <summary>
        /// 立即触发事件
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RaiseImmediately<TCoreEvent>(ref TCoreEvent @event) where TCoreEvent : ICoreEvent
        {
            return EventBus.RaiseImmediately(ref @event);
        }

        /// <summary>
        /// 触发事件
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Raise<TCoreEvent>(in TCoreEvent @event) where TCoreEvent : ICoreEvent
        {
            return EventBus.Raise(in @event);
        }

        /// <summary>
        /// 触发事件（带目标和源）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Raise<TCoreEvent>(TCoreEvent @event, object target, object source) 
            where TCoreEvent : ICoreEvent
        {
            return EventBus.Raise(@event, target, source);
        }

        /// <summary>
        /// 立即触发事件（带目标和源）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RaiseImmediately<TCoreEvent>(TCoreEvent @event, object target, object source) 
            where TCoreEvent : ICoreEvent
        {
            return RaiseImmediately(ref @event, target, source);
        }

        /// <summary>
        /// 立即触发事件（带目标和源）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RaiseImmediately<TCoreEvent>(ref TCoreEvent @event, object target, object source) 
            where TCoreEvent : ICoreEvent
        {
            return EventBus.RaiseImmediately(ref @event, target, source);
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        public static void Subscribe<TCoreEvent>(EventHandler<TCoreEvent> handler, float priority = 0) 
            where TCoreEvent : ICoreEvent
        {
            EventBus.Subscribe(handler, priority);
        }

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        public static void Unsubscribe<TCoreEvent>(EventHandler<TCoreEvent> handler) 
            where TCoreEvent : ICoreEvent
        {
            EventBus.Unsubscribe(handler);
        }

        /// <summary>
        /// 订阅针对特定目标的事件
        /// </summary>
        public static void SubscribeToTarget<TCoreEvent>(object target, TargetedEventHandler<TCoreEvent> handler, float priority = 0) 
            where TCoreEvent : ICoreEvent
        {
            EventBus.SubscribeToTarget(target, handler, priority);
        }

        /// <summary>
        /// 取消订阅针对特定目标的事件
        /// </summary>
        public static void UnsubscribeFromTarget<TCoreEvent>(object target, TargetedEventHandler<TCoreEvent> handler) 
            where TCoreEvent : ICoreEvent
        {
            EventBus.UnsubscribeFromTarget(target, handler);
        }

        /// <summary>
        /// 订阅来自特定源的事件
        /// </summary>
        public static void SubscribeToSource<TCoreEvent>(object source, TargetedEventHandler<TCoreEvent> handler, float priority = 0) 
            where TCoreEvent : ICoreEvent
        {
            EventBus.SubscribeToSource(source, handler, priority);
        }

        /// <summary>
        /// 取消订阅来自特定源的事件
        /// </summary>
        public static void UnsubscribeFromSource<TCoreEvent>(object source, TargetedEventHandler<TCoreEvent> handler) 
            where TCoreEvent : ICoreEvent
        {
            EventBus.UnsubscribeFromSource(source, handler);
        }

        /// <summary>
        /// 消费当前正在触发的事件
        /// </summary>
        public static void ConsumeCurrentEvent()
        {
            EventBus.ConsumeCurrentEvent();
        }

        /// <summary>
        /// 清除指定事件类型的所有监听器
        /// </summary>
        public static void ClearListeners<TCoreEvent>() where TCoreEvent : ICoreEvent
        {
            EventBus.ClearListeners<TCoreEvent>();
        }

        /// <summary>
        /// 清除所有事件监听器
        /// </summary>
        public static void ClearAll()
        {
            EventBus.ClearAll();
        }
    }

    /// <summary>
    /// 事件处理器委托
    /// </summary>
    public delegate void EventHandler<in TCoreEvent>(TCoreEvent @event) where TCoreEvent : ICoreEvent;

    /// <summary>
    /// 目标事件处理器委托
    /// </summary>
    public delegate void TargetedEventHandler<in TCoreEvent>(TCoreEvent @event, object target, object source) where TCoreEvent : ICoreEvent;

    /// <summary>
    /// 通用事件总线实现 - 核心层版本
    /// </summary>
    public class CoreGenericEventBus : IDisposable
    {
        private readonly Dictionary<Type, List<Listener>> _listeners = new();
        private readonly Dictionary<Type, List<TargetedListener>> _targetedListeners = new();
        private readonly Dictionary<object, List<TargetedListener>> _targetListenersMap = new();
        private readonly Dictionary<object, List<TargetedListener>> _sourceListenersMap = new();
        
        private bool _isEventBeingRaised;
        private bool _eventConsumed;
        private int _subscriptionId;
        
        // 日志输出器
        private static TextWriter _errorWriter = Console.Error;

        public bool CurrentEventIsConsumed => _eventConsumed;
        public bool IsEventBeingRaised => _isEventBeingRaised;

        /// <summary>
        /// 监听器结构
        /// </summary>
        private struct Listener
        {
            public int Id;
            public float Priority;
            public Delegate Handler;
        }

        /// <summary>
        /// 目标监听器结构
        /// </summary>
        private struct TargetedListener
        {
            public int Id;
            public float Priority;
            public object Target;
            public object Source;
            public Delegate Handler;
        }

        /// <summary>
        /// 触发事件
        /// </summary>
        public bool Raise<TCoreEvent>(in TCoreEvent @event) where TCoreEvent : ICoreEvent
        {
            var previousIsRaising = _isEventBeingRaised;
            _isEventBeingRaising = true;
            _eventConsumed = false;

            try
            {
                // 先触发普通监听器
                if (_listeners.TryGetValue(typeof(TCoreEvent), out var listeners))
                {
                    // 按优先级排序（从高到低）
                    var sorted = SortByPriority(listeners);
                    foreach (var listener in sorted)
                    {
                        if (_eventConsumed) break;
                        try
                        {
                            ((EventHandler<TCoreEvent>)listener.Handler)(@event);
                        }
                        catch (Exception ex)
                        {
                            _errorWriter.WriteLine($"事件处理异常: {ex}");
                        }
                    }
                }

                // 再触发目标监听器
                if (_targetedListeners.TryGetValue(typeof(TCoreEvent), out var targetedListeners))
                {
                    var sorted = SortByPriority(targetedListeners);
                    foreach (var listener in sorted)
                    {
                        if (_eventConsumed) break;
                        try
                        {
                            ((TargetedEventHandler<TCoreEvent>)listener.Handler)(@event, listener.Target, listener.Source);
                        }
                        catch (Exception ex)
                        {
                            _errorWriter.WriteLine($"事件处理异常: {ex}");
                        }
                    }
                }

                return true;
            }
            finally
            {
                _isEventBeingRaised = previousIsRaising;
                if (!previousIsRaising)
                {
                    _eventConsumed = false;
                }
            }
        }

        /// <summary>
        /// 立即触发事件
        /// </summary>
        public bool RaiseImmediately<TCoreEvent>(ref TCoreEvent @event) where TCoreEvent : ICoreEvent
        {
            var previousIsRaising = _isEventBeingRaised;
            _isEventBeingRaised = false;
            _eventConsumed = false;

            try
            {
                return Raise(in @event);
            }
            finally
            {
                _isEventBeingRaised = previousIsRaising;
            }
        }

        /// <summary>
        /// 触发事件（带目标和源）
        /// </summary>
        public bool Raise<TCoreEvent>(TCoreEvent @event, object target, object source) where TCoreEvent : ICoreEvent
        {
            // 触发目标为null或匹配的事件
            if (_targetListenersMap.TryGetValue(target, out var targetListeners))
            {
                foreach (var listener in SortByPriority(targetListeners))
                {
                    if (_eventConsumed) break;
                    try
                    {
                        ((TargetedEventHandler<TCoreEvent>)listener.Handler)(@event, target, source);
                    }
                    catch (Exception ex)
                    {
                        _errorWriter.WriteLine($"事件处理异常: {ex}");
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 立即触发事件（带目标和源）
        /// </summary>
        public bool RaiseImmediately<TCoreEvent>(ref TCoreEvent @event, object target, object source) where TCoreEvent : ICoreEvent
        {
            var previousIsRaising = _isEventBeingRaised;
            _isEventBeingRaised = false;
            _eventConsumed = false;

            try
            {
                return Raise(@event, target, source);
            }
            finally
            {
                _isEventBeingRaised = previousIsRaising;
            }
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        public void Subscribe<TCoreEvent>(EventHandler<TCoreEvent> handler, float priority = 0) where TCoreEvent : ICoreEvent
        {
            var eventType = typeof(TCoreEvent);
            if (!_listeners.ContainsKey(eventType))
            {
                _listeners[eventType] = new List<Listener>();
            }

            _listeners[eventType].Add(new Listener
            {
                Id = ++_subscriptionId,
                Priority = priority,
                Handler = handler
            });
        }

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        public void Unsubscribe<TCoreEvent>(EventHandler<TCoreEvent> handler) where TCoreEvent : ICoreEvent
        {
            var eventType = typeof(TCoreEvent);
            if (_listeners.TryGetValue(eventType, out var listeners))
            {
                listeners.RemoveAll(l => l.Handler == handler);
            }
        }

        /// <summary>
        /// 订阅针对特定目标的事件
        /// </summary>
        public void SubscribeToTarget<TCoreEvent>(object target, TargetedEventHandler<TCoreEvent> handler, float priority = 0) 
            where TCoreEvent : ICoreEvent
        {
            var eventType = typeof(TCoreEvent);
            if (!_targetedListeners.ContainsKey(eventType))
            {
                _targetedListeners[eventType] = new List<TargetedListener>();
            }

            var listener = new TargetedListener
            {
                Id = ++_subscriptionId,
                Priority = priority,
                Target = target,
                Handler = handler
            };

            _targetedListeners[eventType].Add(listener);

            if (target != null)
            {
                if (!_targetListenersMap.ContainsKey(target))
                {
                    _targetListenersMap[target] = new List<TargetedListener>();
                }
                _targetListenersMap[target].Add(listener);
            }
        }

        /// <summary>
        /// 取消订阅针对特定目标的事件
        /// </summary>
        public void UnsubscribeFromTarget<TCoreEvent>(object target, TargetedEventHandler<TCoreEvent> handler) 
            where TCoreEvent : ICoreEvent
        {
            var eventType = typeof(TCoreEvent);
            if (_targetedListeners.TryGetValue(eventType, out var listeners))
            {
                listeners.RemoveAll(l => l.Target == target && l.Handler == handler);
            }

            if (_targetListenersMap.TryGetValue(target, out var targetListeners))
            {
                targetListeners.RemoveAll(l => l.Handler == handler);
            }
        }

        /// <summary>
        /// 订阅来自特定源的事件
        /// </summary>
        public void SubscribeToSource<TCoreEvent>(object source, TargetedEventHandler<TCoreEvent> handler, float priority = 0) 
            where TCoreEvent : ICoreEvent
        {
            var eventType = typeof(TCoreEvent);
            if (!_targetedListeners.ContainsKey(eventType))
            {
                _targetedListeners[eventType] = new List<TargetedListener>();
            }

            var listener = new TargetedListener
            {
                Id = ++_subscriptionId,
                Priority = priority,
                Source = source,
                Handler = handler
            };

            _targetedListeners[eventType].Add(listener);

            if (source != null)
            {
                if (!_sourceListenersMap.ContainsKey(source))
                {
                    _sourceListenersMap[source] = new List<TargetedListener>();
                }
                _sourceListenersMap[source].Add(listener);
            }
        }

        /// <summary>
        /// 取消订阅来自特定源的事件
        /// </summary>
        public void UnsubscribeFromSource<TCoreEvent>(object source, TargetedEventHandler<TCoreEvent> handler) 
            where TCoreEvent : ICoreEvent
        {
            var eventType = typeof(TCoreEvent);
            if (_targetedListeners.TryGetValue(eventType, out var listeners))
            {
                listeners.RemoveAll(l => l.Source == source && l.Handler == handler);
            }

            if (_sourceListenersMap.TryGetValue(source, out var sourceListeners))
            {
                sourceListeners.RemoveAll(l => l.Handler == handler);
            }
        }

        /// <summary>
        /// 消费当前事件
        /// </summary>
        public void ConsumeCurrentEvent()
        {
            _eventConsumed = true;
        }

        /// <summary>
        /// 清除指定事件的所有监听器
        /// </summary>
        public void ClearListeners<TCoreEvent>() where TCoreEvent : ICoreEvent
        {
            _listeners.Remove(typeof(TCoreEvent));
            _targetedListeners.Remove(typeof(TCoreEvent));
        }

        /// <summary>
        /// 清除所有监听器
        /// </summary>
        public void ClearAll()
        {
            _listeners.Clear();
            _targetedListeners.Clear();
            _targetListenersMap.Clear();
            _sourceListenersMap.Clear();
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            ClearAll();
        }

        private List<Listener> SortByPriority(List<Listener> listeners)
        {
            var sorted = new List<Listener>(listeners);
            sorted.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            return sorted;
        }

        private List<TargetedListener> SortByPriority(List<TargetedListener> listeners)
        {
            var sorted = new List<TargetedListener>(listeners);
            sorted.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            return sorted;
        }
    }
}
