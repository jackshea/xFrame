using System;
using System.Collections;
using Framework.Runtime.Infrastructure.IOC;

namespace Framework.Runtime.Infrastructure
{
    /// <summary>
    /// This class is a generic base class for a systemservice, your probably looking for SystemServiceMonoBehaviour.
    /// </summary>
    public abstract class SystemService : ISystemService
    {
        [Inject]
        public IEventAggregator EventAggregator { get; set; }

        public virtual void Setup()
        {

        }

        public virtual IEnumerator SetupAsync()
        {
            yield break;
        }

        public virtual void Loaded()
        {
            
        }

        public virtual void Dispose()
        {

        }

        public IObservable<TEvent> OnEvent<TEvent>()
        {
            return EventAggregator.GetEvent<TEvent>();
        }

        public void Publish<TEvent>(TEvent eventMessage)
        {
            EventAggregator.Publish(eventMessage);
        }
    }
}