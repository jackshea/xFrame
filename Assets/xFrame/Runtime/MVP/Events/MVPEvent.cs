using xFrame.Runtime.EventBus;

namespace xFrame.MVP.Events
{
    /// <summary>
    /// MVP相关事件的基类
    /// </summary>
    public abstract class MVPEvent : IEvent
    {
        public string MVPId { get; }
        
        protected MVPEvent(string mvpId)
        {
            MVPId = mvpId;
        }
    }
}
