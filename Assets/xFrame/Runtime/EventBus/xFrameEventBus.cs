using GenericEventBus;
using UnityEngine;
using TBaseEvent = xFrame.Runtime.EventBus.IEvent;
using TObject = UnityEngine.GameObject;

namespace xFrame.Runtime.EventBus
{
    /// <summary>
    /// xFrame事件总线
    /// </summary>
    public static class xFrameEventBus
    {
        private static GenericEventBus<TBaseEvent, TObject> _eventBus;

        /// <summary>
        /// 确保事件总线实例已经创建。
        /// </summary>
        private static GenericEventBus<TBaseEvent, TObject> EventBus
        {
            get
            {
                if (_eventBus == null)
                {
                    _eventBus = new GenericEventBus<TBaseEvent, TObject>();
                }

                return _eventBus;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
#if UNITY_EDITOR
            // 如果禁用了域重新加载，则此字段可能仍保持上一次播放模式的赋值。
            _eventBus?.Dispose();
#endif

            _eventBus = new GenericEventBus<TBaseEvent, TObject>();
        }

        /// <summary>
        /// 当前触发的事件是否已经被消费？
        /// </summary>
        public static bool CurrentEventIsConsumed => EventBus.CurrentEventIsConsumed;

        /// <summary>
        /// 当前是否正在触发某个事件？
        /// </summary>
        public static bool IsEventBeingRaised => EventBus.IsEventBeingRaised;

        /// <summary>
        /// 立即触发给定事件，即使目前仍有其他事件正在触发。
        /// </summary>
        /// <param name="event">要触发的事件。</param>
        /// <typeparam name="TEvent">要触发的事件类型。</typeparam>
        /// <returns>如果事件已通过 <see cref="ConsumeCurrentEvent"/> 被消费，则返回 true。</returns>
        public static bool RaiseImmediately<TEvent>(TEvent @event) where TEvent : TBaseEvent
        {
            return RaiseImmediately(ref @event);
        }

        /// <summary>
        /// 立即触发给定事件，即使目前仍有其他事件正在触发。
        /// </summary>
        /// <param name="event">要触发的事件。</param>
        /// <typeparam name="TEvent">要触发的事件类型。</typeparam>
        /// <returns>如果事件已通过 <see cref="ConsumeCurrentEvent"/> 被消费，则返回 true。</returns>
        public static bool RaiseImmediately<TEvent>(ref TEvent @event) where TEvent : TBaseEvent
        {
            return EventBus.RaiseImmediately(ref @event);
        }

        /// <summary>
        /// 触发给定事件。如果当前有其他事件正在触发，该事件将会在它们完成之后触发。
        /// </summary>
        /// <param name="event">要触发的事件。</param>
        /// <typeparam name="TEvent">要触发的事件类型。</typeparam>
        /// <returns>如果事件被立即触发，当事件通过 <see cref="ConsumeCurrentEvent"/> 被消费时返回 true。</returns>
        public static bool Raise<TEvent>(in TEvent @event) where TEvent : TBaseEvent
        {
            return EventBus.Raise(in @event);
        }

        /// <summary>
        /// 订阅指定的事件类型。
        /// </summary>
        /// <param name="handler">当事件被触发时应被调用的方法。</param>
        /// <param name="priority">优先级越高，该监听器就越早接收事件；若多个监听器具有相同优先级，则按照订阅顺序调用。</param>
        /// <typeparam name="TEvent">要订阅的事件类型。</typeparam>
        public static void SubscribeTo<TEvent>(GenericEventBus<TBaseEvent>.EventHandler<TEvent> handler,
            float priority = 0)
            where TEvent : TBaseEvent
        {
            EventBus.SubscribeTo(handler, priority);
        }

        /// <summary>
        /// 取消订阅指定的事件类型。
        /// </summary>
        /// <param name="handler">先前在 SubscribeTo 中提供的方法。</param>
        /// <typeparam name="TEvent">要取消订阅的事件类型。</typeparam>
        public static void UnsubscribeFrom<TEvent>(GenericEventBus<TBaseEvent>.EventHandler<TEvent> handler)
            where TEvent : TBaseEvent
        {
            EventBus.UnsubscribeFrom(handler);
        }

        /// <summary>
        /// 触发给定事件。如果当前有其他事件正在触发，该事件将会在它们完成之后触发。
        /// </summary>
        /// <param name="event">要触发的事件。</param>
        /// <param name="target">事件的目标对象。</param>
        /// <param name="source">事件的来源对象。</param>
        /// <typeparam name="TEvent">要触发的事件类型。</typeparam>
        /// <returns>如果事件被立即触发，当事件通过 <see cref="ConsumeCurrentEvent"/> 被消费时返回 true。</returns>
        public static bool Raise<TEvent>(TEvent @event, TObject target, TObject source) where TEvent : TBaseEvent
        {
            return EventBus.Raise(@event, target, source);
        }

        /// <summary>
        /// 立即触发给定事件，即使目前仍有其他事件正在触发。
        /// </summary>
        /// <param name="event">要触发的事件。</param>
        /// <param name="target">事件的目标对象。</param>
        /// <param name="source">事件的来源对象。</param>
        /// <typeparam name="TEvent">要触发的事件类型。</typeparam>
        /// <returns>如果事件已通过 <see cref="ConsumeCurrentEvent"/> 被消费，则返回 true。</returns>
        public static bool RaiseImmediately<TEvent>(TEvent @event, TObject target, TObject source)
            where TEvent : TBaseEvent
        {
            return RaiseImmediately(ref @event, target, source);
        }

        /// <summary>
        /// 立即触发给定事件，即使目前仍有其他事件正在触发。
        /// </summary>
        /// <param name="event">要触发的事件。</param>
        /// <param name="target">事件的目标对象。</param>
        /// <param name="source">事件的来源对象。</param>
        /// <typeparam name="TEvent">要触发的事件类型。</typeparam>
        /// <returns>如果事件已通过 <see cref="ConsumeCurrentEvent"/> 被消费，则返回 true。</returns>
        public static bool RaiseImmediately<TEvent>(ref TEvent @event, TObject target, TObject source)
            where TEvent : TBaseEvent
        {
            return EventBus.RaiseImmediately(ref @event, target, source);
        }

        /// <summary>
        /// 订阅指定的事件类型。
        /// </summary>
        /// <param name="handler">当事件被触发时应被调用的方法。</param>
        /// <param name="priority">优先级越高，该监听器就越早接收事件；若多个监听器具有相同优先级，则按照订阅顺序调用。</param>
        /// <typeparam name="TEvent">要订阅的事件类型。</typeparam>
        public static void SubscribeTo<TEvent>(
            GenericEventBus<TBaseEvent, TObject>.TargetedEventHandler<TEvent> handler,
            float priority = 0)
            where TEvent : TBaseEvent
        {
            EventBus.SubscribeTo(handler, priority);
        }

        /// <summary>
        /// 取消订阅指定的事件类型。
        /// </summary>
        /// <param name="handler">先前在 SubscribeTo 中提供的方法。</param>
        /// <typeparam name="TEvent">要取消订阅的事件类型。</typeparam>
        public static void UnsubscribeFrom<TEvent>(
            GenericEventBus<TBaseEvent, TObject>.TargetedEventHandler<TEvent> handler) where TEvent : TBaseEvent
        {
            EventBus.UnsubscribeFrom(handler);
        }

        /// <summary>
        /// 订阅指定的事件类型，但仅在事件针对给定对象时触发。
        /// </summary>
        /// <param name="target">目标对象。</param>
        /// <param name="handler">当事件被触发时应被调用的方法。</param>
        /// <param name="priority">优先级越高，该监听器就越早接收事件；若多个监听器具有相同优先级，则按照订阅顺序调用。</param>
        /// <typeparam name="TEvent">要订阅的事件类型。</typeparam>
        public static void SubscribeToTarget<TEvent>(TObject target,
            GenericEventBus<TBaseEvent, TObject>.TargetedEventHandler<TEvent> handler, float priority = 0)
            where TEvent : TBaseEvent
        {
            EventBus.SubscribeToTarget(target, handler, priority);
        }

        /// <summary>
        /// 取消订阅指定的事件类型，但仅在事件的目标是给定对象时取消。
        /// </summary>
        /// <param name="target">目标对象。</param>
        /// <param name="handler">先前在 SubscribeToTarget 中提供的方法。</param>
        /// <typeparam name="TEvent">要取消订阅的事件类型。</typeparam>
        public static void UnsubscribeFromTarget<TEvent>(TObject target,
            GenericEventBus<TBaseEvent, TObject>.TargetedEventHandler<TEvent> handler)
            where TEvent : TBaseEvent
        {
            EventBus.UnsubscribeFromTarget(target, handler);
        }

        /// <summary>
        /// 订阅指定的事件类型，但仅在事件来源于给定对象时触发。
        /// </summary>
        /// <param name="source">来源对象。</param>
        /// <param name="handler">当事件被触发时应被调用的方法。</param>
        /// <param name="priority">优先级越高，该监听器就越早接收事件；若多个监听器具有相同优先级，则按照订阅顺序调用。</param>
        /// <typeparam name="TEvent">要订阅的事件类型。</typeparam>
        public static void SubscribeToSource<TEvent>(TObject source,
            GenericEventBus<TBaseEvent, TObject>.TargetedEventHandler<TEvent> handler, float priority = 0)
            where TEvent : TBaseEvent
        {
            EventBus.SubscribeToSource(source, handler, priority);
        }

        /// <summary>
        /// 取消订阅指定的事件类型，但仅在事件来源于给定对象时取消。
        /// </summary>
        /// <param name="source">来源对象。</param>
        /// <param name="handler">先前在 SubscribeToSource 中提供的方法。</param>
        /// <typeparam name="TEvent">要取消订阅的事件类型。</typeparam>
        public static void UnsubscribeFromSource<TEvent>(TObject source,
            GenericEventBus<TBaseEvent, TObject>.TargetedEventHandler<TEvent> handler)
            where TEvent : TBaseEvent
        {
            EventBus.UnsubscribeFromSource(source, handler);
        }

        /// <summary>
        /// 消费当前正在触发的事件，从而阻止事件继续传播给其他监听器。
        /// </summary>
        public static void ConsumeCurrentEvent()
        {
            EventBus.ConsumeCurrentEvent();
        }

        /// <summary>
        /// 移除指定事件类型的所有监听器。
        /// </summary>
        /// <typeparam name="TEvent">事件类型。</typeparam>
        /// <exception cref="System.InvalidOperationException">当当前正在触发事件时抛出。</exception>
        public static void ClearListeners<TEvent>() where TEvent : TBaseEvent
        {
            EventBus.ClearListeners<TEvent>();
        }
    }
}