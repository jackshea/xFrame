using System.Collections;
using Framework.Runtime.Infrastructure.IOC;
using UnityEngine;

namespace Framework.Runtime.Infrastructure
{
    public interface ISystemLoader
    {

        void Load();

        IEnumerator LoadAsync();

        IUFrameContainer Container { get; set; }

        IEventAggregator EventAggregator { get; set; }

    }

    public partial class SystemLoader : MonoBehaviour, ISystemLoader
    {
        public virtual void Load()
        {

        }

        public virtual IEnumerator LoadAsync()
        {
            yield break;
        }

        public IUFrameContainer Container { get; set; }

        public IEventAggregator EventAggregator { get; set; }
    }

}