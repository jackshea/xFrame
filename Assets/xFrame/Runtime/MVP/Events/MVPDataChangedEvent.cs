namespace xFrame.MVP.Events
{
    /// <summary>
    /// MVP数据变更事件
    /// </summary>
    public class MVPDataChangedEvent : MVPEvent
    {
        public object Data { get; }
        
        public MVPDataChangedEvent(string mvpId, object data) : base(mvpId)
        {
            Data = data;
        }
    }
}
