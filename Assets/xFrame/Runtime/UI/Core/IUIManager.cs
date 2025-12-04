using System;
using System.Threading.Tasks;

namespace xFrame.Runtime.UI
{
    /// <summary>
    /// UI管理器接口
    /// 负责所有UI的加载、显示、隐藏、销毁等管理
    /// </summary>
    public interface IUIManager
    {
        #region 打开UI

        /// <summary>
        /// 异步打开UI
        /// </summary>
        /// <typeparam name="T">UI类型</typeparam>
        /// <param name="data">传递给UI的数据</param>
        /// <returns>打开的UI实例</returns>
        Task<T> OpenAsync<T>(object data = null) where T : UIView;

        /// <summary>
        /// 异步打开UI（通过类型）
        /// </summary>
        /// <param name="uiType">UI类型</param>
        /// <param name="data">传递给UI的数据</param>
        /// <returns>打开的UI实例</returns>
        Task<UIView> OpenAsync(Type uiType, object data = null);

        #endregion

        #region 关闭UI

        /// <summary>
        /// 关闭指定类型的UI
        /// </summary>
        /// <typeparam name="T">UI类型</typeparam>
        void Close<T>() where T : UIView;

        /// <summary>
        /// 关闭指定实例的UI
        /// </summary>
        /// <param name="view">UI实例</param>
        void Close(UIView view);

        /// <summary>
        /// 关闭指定类型的UI
        /// </summary>
        /// <param name="uiType">UI类型</param>
        void Close(Type uiType);

        /// <summary>
        /// 关闭所有UI
        /// </summary>
        /// <param name="layer">指定层级，null表示所有层级</param>
        void CloseAll(UILayer? layer = null);

        #endregion

        #region 查询UI

        /// <summary>
        /// 获取已打开的UI实例
        /// </summary>
        /// <typeparam name="T">UI类型</typeparam>
        /// <returns>UI实例，未打开则返回null</returns>
        T Get<T>() where T : UIView;

        /// <summary>
        /// 获取已打开的UI实例（通过类型）
        /// </summary>
        /// <param name="uiType">UI类型</param>
        /// <returns>UI实例，未打开则返回null</returns>
        UIView Get(Type uiType);

        /// <summary>
        /// 检查指定类型的UI是否已打开
        /// </summary>
        /// <typeparam name="T">UI类型</typeparam>
        /// <returns>是否已打开</returns>
        bool IsOpen<T>() where T : UIView;

        /// <summary>
        /// 检查指定类型的UI是否已打开
        /// </summary>
        /// <param name="uiType">UI类型</param>
        /// <returns>是否已打开</returns>
        bool IsOpen(Type uiType);

        #endregion

        #region 导航栈

        /// <summary>
        /// 返回上一个UI（栈管理）
        /// </summary>
        void Back();

        /// <summary>
        /// 检查是否可以返回
        /// </summary>
        /// <returns>栈中是否有可返回的UI</returns>
        bool CanGoBack();

        /// <summary>
        /// 清空导航栈
        /// </summary>
        void ClearNavigationStack();

        #endregion

        #region 预加载

        /// <summary>
        /// 预加载UI资源
        /// </summary>
        /// <typeparam name="T">UI类型</typeparam>
        Task PreloadAsync<T>() where T : UIView;

        /// <summary>
        /// 预加载UI资源（通过类型）
        /// </summary>
        /// <param name="uiType">UI类型</param>
        Task PreloadAsync(Type uiType);

        /// <summary>
        /// 预加载多个UI资源
        /// </summary>
        /// <param name="uiTypes">UI类型数组</param>
        Task PreloadBatchAsync(params Type[] uiTypes);

        #endregion

        #region 工具方法

        /// <summary>
        /// 设置指定层级的交互性
        /// </summary>
        /// <param name="layer">UI层级</param>
        /// <param name="interactable">是否可交互</param>
        void SetLayerInteractable(UILayer layer, bool interactable);

        /// <summary>
        /// 获取指定层级当前打开的UI数量
        /// </summary>
        /// <param name="layer">UI层级</param>
        /// <returns>UI数量</returns>
        int GetOpenUICount(UILayer layer);

        #endregion
    }
}