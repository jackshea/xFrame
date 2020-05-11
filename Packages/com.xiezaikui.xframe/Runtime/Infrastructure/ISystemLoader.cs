using System.Collections;
using UnityEngine;
using xFrame.Infrastructure;

namespace xFrame.Infrastructure
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