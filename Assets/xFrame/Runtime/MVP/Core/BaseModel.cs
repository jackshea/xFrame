using System;
using Cysharp.Threading.Tasks;
using VContainer;
using xFrame.Runtime.Logging;

namespace xFrame.MVP
{
    /// <summary>
    /// Model基类
    /// 提供通用的数据管理功能
    /// </summary>
    public abstract class BaseModel : IModel
    {
        protected IXLogger Logger { get; private set; }
        
        public event Action<IModel> OnDataChanged;
        
        [Inject]
        public void Construct(IXLogger logger)
        {
            Logger = logger;
        }
        
        public virtual async UniTask InitializeAsync()
        {
            Logger.Info($"{GetType().Name} initialized");
            await UniTask.CompletedTask;
        }
        
        protected void NotifyDataChanged()
        {
            OnDataChanged?.Invoke(this);
        }
        
        public virtual void Dispose()
        {
            OnDataChanged = null;
            Logger.Info($"{GetType().Name} disposed");
        }
    }
}
