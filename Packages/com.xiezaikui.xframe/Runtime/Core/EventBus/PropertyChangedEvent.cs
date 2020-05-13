namespace xFrame.Core
{
    public class PropertyChangedEvent<T> : EventBase
    {
        public PropertyChangedEvent(T oldValue, T newValue, string propertyName) : this(null, oldValue, newValue,
            propertyName)
        {
        }

        public PropertyChangedEvent(object sender, T oldValue, T newValue, string propertyName) : base(sender)
        {
            PropertyName = propertyName;
            OldValue = oldValue;
            NewValue = newValue;
        }

        public string PropertyName { get; }

        public T OldValue { get; }

        public T NewValue { get; }
    }
}