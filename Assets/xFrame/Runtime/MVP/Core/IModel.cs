using System;
using Cysharp.Threading.Tasks;

namespace xFrame.MVP
{
    /// <summary>
    /// MVP模式中的Model接口
    /// 负责数据管理和业务逻辑
    /// </summary>
    public interface IModel : IDisposable
    {
        /// <summary>
        /// 初始化Model
        /// </summary>
        UniTask InitializeAsync();
        
        /// <summary>
        /// 数据变更事件
        /// </summary>
        event Action<IModel> OnDataChanged;
    }
}
