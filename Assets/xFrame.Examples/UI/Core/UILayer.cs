namespace xFrame.Runtime.UI
{
    /// <summary>
    /// UI层级枚举
    /// 定义UI的显示层级和优先级
    /// </summary>
    public enum UILayer
    {
        /// <summary>
        /// 背景层 (SortOrder: 0-999)
        /// 用于永久显示的背景UI
        /// </summary>
        Background = 0,

        /// <summary>
        /// 普通层 (SortOrder: 1000-1999)
        /// 用于常规游戏UI，如主界面、背包等
        /// </summary>
        Normal = 1,

        /// <summary>
        /// 弹窗层 (SortOrder: 2000-2999)
        /// 用于弹出式对话框、提示框等
        /// </summary>
        Popup = 2,

        /// <summary>
        /// 系统层 (SortOrder: 3000-3999)
        /// 用于系统级UI，如加载界面、网络错误提示等
        /// </summary>
        System = 3,

        /// <summary>
        /// 顶层 (SortOrder: 4000-4999)
        /// 用于最高优先级UI，如调试工具、GM面板等
        /// </summary>
        Top = 4
    }

    /// <summary>
    /// UI层级扩展方法
    /// </summary>
    public static class UILayerExtensions
    {
        /// <summary>
        /// 获取层级对应的基础SortOrder
        /// </summary>
        /// <param name="layer">UI层级</param>
        /// <returns>基础SortOrder值</returns>
        public static int GetBaseSortOrder(this UILayer layer)
        {
            return (int)layer * 1000;
        }

        /// <summary>
        /// 获取层级的Canvas名称
        /// </summary>
        /// <param name="layer">UI层级</param>
        /// <returns>Canvas名称</returns>
        public static string GetCanvasName(this UILayer layer)
        {
            return $"Canvas_{layer}";
        }
    }
}
