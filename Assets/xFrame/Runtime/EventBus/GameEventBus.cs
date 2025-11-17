using GenericEventBus;
using UnityEngine;
using TBaseEvent = xFrame.Runtime.EventBus.IEvent;
using TObject = UnityEngine.GameObject;

namespace xFrame.Runtime.EventBus
{
    public static class GameEventBus
    {
        private static GenericEventBus<TBaseEvent, TObject> _eventBus;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
#if UNITY_EDITOR
            // If domain reload is disabled, this can still be assigned from last play mode.
            _eventBus?.Dispose();
#endif

            _eventBus = new GenericEventBus<TBaseEvent, TObject>();
        }

        /// <summary>
        /// Has the current raised event been consumed?
        /// </summary>
        public static bool CurrentEventIsConsumed => _eventBus.CurrentEventIsConsumed;

        /// <summary>
        /// Is an event currently being raised?
        /// </summary>
        public static bool IsEventBeingRaised => _eventBus.IsEventBeingRaised;

        /// <summary>
        /// <para>Raises the given event immediately, regardless if another event is currently still being raised.</para>
        /// </summary>
        /// <param name="event">The event to raise.</param>
        /// <typeparam name="TEvent">The type of event to raise.</typeparam>
        /// <returns>Returns true if the event was consumed with <see cref="ConsumeCurrentEvent"/>.</returns>
        public static bool RaiseImmediately<TEvent>(TEvent @event) where TEvent : TBaseEvent
        {
            return RaiseImmediately(ref @event);
        }

        /// <summary>
        /// <para>Raises the given event immediately, regardless if another event is currently still being raised.</para>
        /// </summary>
        /// <param name="event">The event to raise.</param>
        /// <typeparam name="TEvent">The type of event to raise.</typeparam>
        /// <returns>Returns true if the event was consumed with <see cref="ConsumeCurrentEvent"/>.</returns>
        public static bool RaiseImmediately<TEvent>(ref TEvent @event) where TEvent : TBaseEvent
        {
            return _eventBus.RaiseImmediately(ref @event);
        }

        /// <summary>
        /// <para>Raises the given event. If there are other events currently being raised, this event will be raised after those events finish.</para>
        /// </summary>
        /// <param name="event">The event to raise.</param>
        /// <typeparam name="TEvent">The type of event to raise.</typeparam>
        /// <returns>If the event was raised immediately, returns true if the event was consumed with <see cref="ConsumeCurrentEvent"/>.</returns>
        public static bool Raise<TEvent>(in TEvent @event) where TEvent : TBaseEvent
        {
            return _eventBus.Raise(in @event);
        }

        /// <summary>
        /// Subscribe to a given event type.
        /// </summary>
        /// <param name="handler">The method that should be invoked when the event is raised.</param>
        /// <param name="priority">Higher priority means this listener will receive the event earlier than other listeners with lower priority.
        ///                        If multiple listeners have the same priority, they will be invoked in the order they subscribed.</param>
        /// <typeparam name="TEvent">The event type to subscribe to.</typeparam>
        public static void SubscribeTo<TEvent>(GenericEventBus<TBaseEvent>.EventHandler<TEvent> handler,
            float priority = 0)
            where TEvent : TBaseEvent
        {
            _eventBus.SubscribeTo(handler, priority);
        }

        /// <summary>
        /// Unsubscribe from a given event type.
        /// </summary>
        /// <param name="handler">The method that was previously given in SubscribeTo.</param>
        /// <typeparam name="TEvent">The event type to unsubscribe from.</typeparam>
        public static void UnsubscribeFrom<TEvent>(GenericEventBus<TBaseEvent>.EventHandler<TEvent> handler)
            where TEvent : TBaseEvent
        {
            _eventBus.UnsubscribeFrom(handler);
        }

        /// <summary>
        /// <para>Raises the given event. If there are other events currently being raised, this event will be raised after those events finish.</para>
        /// </summary>
        /// <param name="event">The event to raise.</param>
        /// <param name="target">The target object for this event.</param>
        /// <param name="source">The source object for this event.</param>
        /// <typeparam name="TEvent">The type of event to raise.</typeparam>
        /// <returns>If the event was raised immediately, returns true if the event was consumed with <see cref="ConsumeCurrentEvent"/>.</returns>
        public static bool Raise<TEvent>(TEvent @event, TObject target, TObject source) where TEvent : TBaseEvent
        {
            return _eventBus.Raise(@event, target, source);
        }

        /// <summary>
        /// <para>Raises the given event immediately, regardless if another event is currently still being raised.</para>
        /// </summary>
        /// <param name="event">The event to raise.</param>
        /// <param name="target">The target object for this event.</param>
        /// <param name="source">The source object for this event.</param>
        /// <typeparam name="TEvent">The type of event to raise.</typeparam>
        /// <returns>Returns true if the event was consumed with <see cref="ConsumeCurrentEvent"/>.</returns>
        public static bool RaiseImmediately<TEvent>(TEvent @event, TObject target, TObject source)
            where TEvent : TBaseEvent
        {
            return RaiseImmediately(ref @event, target, source);
        }

        /// <summary>
        /// <para>Raises the given event immediately, regardless if another event is currently still being raised.</para>
        /// </summary>
        /// <param name="event">The event to raise.</param>
        /// <param name="target">The target object for this event.</param>
        /// <param name="source">The source object for this event.</param>
        /// <typeparam name="TEvent">The type of event to raise.</typeparam>
        /// <returns>Returns true if the event was consumed with <see cref="ConsumeCurrentEvent"/>.</returns>
        public static bool RaiseImmediately<TEvent>(ref TEvent @event, TObject target, TObject source)
            where TEvent : TBaseEvent
        {
            return _eventBus.RaiseImmediately(ref @event, target, source);
        }

        /// <summary>
        /// Subscribe to a given event type.
        /// </summary>
        /// <param name="handler">The method that should be invoked when the event is raised.</param>
        /// <param name="priority">Higher priority means this listener will receive the event earlier than other listeners with lower priority.
        ///                        If multiple listeners have the same priority, they will be invoked in the order they subscribed.</param>
        /// <typeparam name="TEvent">The event type to subscribe to.</typeparam>
        public static void SubscribeTo<TEvent>(
            GenericEventBus<TBaseEvent, TObject>.TargetedEventHandler<TEvent> handler,
            float priority = 0)
            where TEvent : TBaseEvent
        {
            _eventBus.SubscribeTo(handler, priority);
        }

        /// <summary>
        /// Unsubscribe from a given event type.
        /// </summary>
        /// <param name="handler">The method that was previously given in SubscribeTo.</param>
        /// <typeparam name="TEvent">The event type to unsubscribe from.</typeparam>
        public static void UnsubscribeFrom<TEvent>(
            GenericEventBus<TBaseEvent, TObject>.TargetedEventHandler<TEvent> handler) where TEvent : TBaseEvent
        {
            _eventBus.UnsubscribeFrom(handler);
        }

        /// <summary>
        /// Subscribe to a given event type, but only if it targets the given object.
        /// </summary>
        /// <param name="target">The target object.</param>
        /// <param name="handler">The method that should be invoked when the event is raised.</param>
        /// <param name="priority">Higher priority means this listener will receive the event earlier than other listeners with lower priority.
        ///                        If multiple listeners have the same priority, they will be invoked in the order they subscribed.</param>
        /// <typeparam name="TEvent">The event type to subscribe to.</typeparam>
        public static void SubscribeToTarget<TEvent>(TObject target,
            GenericEventBus<TBaseEvent, TObject>.TargetedEventHandler<TEvent> handler, float priority = 0)
            where TEvent : TBaseEvent
        {
            _eventBus.SubscribeToTarget(target, handler, priority);
        }

        /// <summary>
        /// Unsubscribe from a given event type, but only if it targets the given object.
        /// </summary>
        /// <param name="target">The target object.</param>
        /// <param name="handler">The method that was previously given in SubscribeToTarget.</param>
        /// <typeparam name="TEvent">The event type to unsubscribe from.</typeparam>
        public static void UnsubscribeFromTarget<TEvent>(TObject target,
            GenericEventBus<TBaseEvent, TObject>.TargetedEventHandler<TEvent> handler)
            where TEvent : TBaseEvent
        {
            _eventBus.UnsubscribeFromTarget(target, handler);
        }

        /// <summary>
        /// Subscribe to a given event type, but only if it comes from the given object.
        /// </summary>
        /// <param name="source">The source object.</param>
        /// <param name="handler">The method that should be invoked when the event is raised.</param>
        /// <param name="priority">Higher priority means this listener will receive the event earlier than other listeners with lower priority.
        ///                        If multiple listeners have the same priority, they will be invoked in the order they subscribed.</param>
        /// <typeparam name="TEvent">The event type to subscribe to.</typeparam>
        public static void SubscribeToSource<TEvent>(TObject source,
            GenericEventBus<TBaseEvent, TObject>.TargetedEventHandler<TEvent> handler, float priority = 0)
            where TEvent : TBaseEvent
        {
            _eventBus.SubscribeToSource(source, handler, priority);
        }

        /// <summary>
        /// Unsubscribe from a given event type, but only if it comes from the given object.
        /// </summary>
        /// <param name="source">The source object.</param>
        /// <param name="handler">The method that was previously given in SubscribeToSource.</param>
        /// <typeparam name="TEvent">The event type to unsubscribe from.</typeparam>
        public static void UnsubscribeFromSource<TEvent>(TObject source,
            GenericEventBus<TBaseEvent, TObject>.TargetedEventHandler<TEvent> handler)
            where TEvent : TBaseEvent
        {
            _eventBus.UnsubscribeFromSource(source, handler);
        }

        /// <summary>
        /// Consumes the current event being raised, which stops the propagation to other listeners.
        /// </summary>
        public static void ConsumeCurrentEvent()
        {
            _eventBus.ConsumeCurrentEvent();
        }

        /// <summary>
        /// Removes all the listeners of the given event type.
        /// </summary>
        /// <typeparam name="TEvent">The event type.</typeparam>
        /// <exception cref="System.InvalidOperationException">Thrown if an event is currently being raised.</exception>
        public static void ClearListeners<TEvent>() where TEvent : TBaseEvent
        {
            _eventBus.ClearListeners<TEvent>();
        }
    }
}