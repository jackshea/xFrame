using System.Threading.Tasks;
using UnityEngine;
using VContainer;
using xFrame.Runtime.EventBus;
using xFrame.Runtime.UI;
using xFrame.Runtime.UI.Events;

namespace xFrame.Examples.UI
{
    /// <summary>
    /// UI框架使用示例
    /// 展示如何在实际游戏中使用xFrame UI系统
    /// </summary>
    public class UIUsageExample : MonoBehaviour
    {
        #region Unity生命周期

        private async void Start()
        {
            Debug.Log("=== xFrame UI框架使用示例 ===");

            // 等待一帧，确保所有系统初始化完成
            await Task.Yield();

            // 示例1: 打开主菜单
            await Example1_OpenMainMenu();

            // 等待2秒
            await Task.Delay(2000);

            // 示例2: 打开确认对话框
            await Example2_OpenConfirmDialog();

            // 等待2秒
            await Task.Delay(2000);

            // 示例3: 打开玩家信息面板（MVVM）
            await Example3_OpenPlayerInfoPanel();

            // 等待2秒
            await Task.Delay(2000);

            // 示例4: UI导航栈
            await Example4_NavigationStack();

            // 示例5: 事件通信
            Example5_EventCommunication();

            // 示例6: UI查询
            Example6_QueryUI();
        }

        #endregion

        #region Unity生命周期

        private void OnDestroy()
        {
            // 取消事件订阅
            xFrameEventBus.UnsubscribeFrom<UIOpenedEvent>(OnUIOpened);
            xFrameEventBus.UnsubscribeFrom<UIClosedEvent>(OnUIClosed);
        }

        #endregion

        #region 依赖注入

        private IUIManager _uiManager;

        /// <summary>
        /// VContainer依赖注入
        /// </summary>
        [Inject]
        public void Construct(IUIManager uiManager)
        {
            _uiManager = uiManager;
        }

        #endregion

        #region 示例方法

        /// <summary>
        /// 示例1: 打开主菜单
        /// </summary>
        private async Task Example1_OpenMainMenu()
        {
            Debug.Log("\n--- 示例1: 打开主菜单 ---");

            // 创建数据
            var menuData = new MainMenuData
            {
                PlayerName = "玩家001",
                Level = 10
            };

            // 打开UI
            var mainMenu = await _uiManager.OpenAsync<MainMenuPanel>(menuData);

            Debug.Log($"主菜单已打开，IsOpen: {mainMenu.IsOpen}");
        }

        /// <summary>
        /// 示例2: 打开确认对话框
        /// </summary>
        private async Task Example2_OpenConfirmDialog()
        {
            Debug.Log("\n--- 示例2: 打开确认对话框 ---");

            // 方式1: 使用完整配置
            var dialogData = new ConfirmDialogData
            {
                Title = "删除确认",
                Message = "确定要删除这个存档吗？此操作不可撤销！",
                ConfirmText = "删除",
                CancelText = "取消",
                OnConfirm = () => Debug.Log("用户确认删除"),
                OnCancel = () => Debug.Log("用户取消删除")
            };

            var dialog = await _uiManager.OpenAsync<ConfirmDialog>(dialogData);
            Debug.Log("确认对话框已打开");

            // 方式2: 使用便捷方法创建提示对话框（只有确定按钮）
            // var alertData = ConfirmDialogData.CreateAlert(
            //     "保存成功！",
            //     "提示",
            //     () => Debug.Log("用户点击了确定")
            // );
            // await _uiManager.OpenAsync<ConfirmDialog>(alertData);
        }

        /// <summary>
        /// 示例3: 打开玩家信息面板（MVVM模式）
        /// </summary>
        private async Task Example3_OpenPlayerInfoPanel()
        {
            Debug.Log("\n--- 示例3: 打开玩家信息面板（MVVM） ---");

            // 创建ViewModel
            var viewModel = new PlayerInfoViewModel
            {
                PlayerName = "传奇勇士",
                Level = 42,
                CurrentHealth = 850,
                MaxHealth = 1000,
                CurrentMana = 320,
                MaxMana = 500,
                CurrentExp = 7500,
                ExpToNextLevel = 10000,
                Gold = 123456,
                Diamond = 999
            };

            // 打开面板
            var playerPanel = await _uiManager.OpenAsync<PlayerInfoPanel>(viewModel);
            Debug.Log("玩家信息面板已打开");

            // 模拟数据更新（2秒后）
            await Task.Delay(2000);

            // 更新ViewModel
            viewModel.CurrentHealth = 950;
            viewModel.Gold = 150000;

            // 通过事件通知UI更新
            xFrameEventBus.Raise(new PlayerDataUpdatedEvent(viewModel));
            Debug.Log("玩家数据已更新并通知UI");
        }

        /// <summary>
        /// 示例4: UI导航栈
        /// </summary>
        private async Task Example4_NavigationStack()
        {
            Debug.Log("\n--- 示例4: UI导航栈 ---");

            // 关闭所有UI
            _uiManager.CloseAll();
            await Task.Delay(500);

            // 打开一系列UI（使用导航栈）
            Debug.Log("打开UI A");
            // await _uiManager.OpenAsync<UIPanelA>();

            await Task.Delay(500);

            Debug.Log("打开UI B");
            // await _uiManager.OpenAsync<UIPanelB>();

            await Task.Delay(500);

            Debug.Log("打开UI C");
            // await _uiManager.OpenAsync<UIPanelC>();

            // 检查是否可以返回
            if (_uiManager.CanGoBack())
            {
                Debug.Log("可以返回，按Back键返回上一个UI");
                _uiManager.Back(); // 返回到UI B
            }

            await Task.Delay(500);

            if (_uiManager.CanGoBack()) _uiManager.Back(); // 返回到UI A
        }

        /// <summary>
        /// 示例5: 事件通信
        /// </summary>
        private void Example5_EventCommunication()
        {
            Debug.Log("\n--- 示例5: 事件通信 ---");

            // 订阅UI打开事件
            xFrameEventBus.SubscribeTo<UIOpenedEvent>(OnUIOpened);

            // 订阅UI关闭事件
            xFrameEventBus.SubscribeTo<UIClosedEvent>(OnUIClosed);

            Debug.Log("已订阅UI事件");
        }

        /// <summary>
        /// UI打开事件处理
        /// </summary>
        private void OnUIOpened(ref UIOpenedEvent evt)
        {
            Debug.Log($"[事件] UI已打开: {evt.UIType.Name}, 层级: {evt.Layer}");
        }

        /// <summary>
        /// UI关闭事件处理
        /// </summary>
        private void OnUIClosed(ref UIClosedEvent evt)
        {
            Debug.Log($"[事件] UI已关闭: {evt.UIType.Name}, 层级: {evt.Layer}");
        }

        /// <summary>
        /// 示例6: UI查询
        /// </summary>
        private void Example6_QueryUI()
        {
            Debug.Log("\n--- 示例6: UI查询 ---");

            // 检查UI是否打开
            var isMainMenuOpen = _uiManager.IsOpen<MainMenuPanel>();
            Debug.Log($"主菜单是否打开: {isMainMenuOpen}");

            // 获取已打开的UI实例
            var mainMenu = _uiManager.Get<MainMenuPanel>();
            if (mainMenu != null) Debug.Log($"获取到主菜单实例: {mainMenu.name}");

            // 获取指定层级的UI数量
            var normalLayerCount = _uiManager.GetOpenUICount(UILayer.Normal);
            var popupLayerCount = _uiManager.GetOpenUICount(UILayer.Popup);

            Debug.Log($"Normal层UI数量: {normalLayerCount}");
            Debug.Log($"Popup层UI数量: {popupLayerCount}");
        }

        #endregion

        #region 高级示例

        /// <summary>
        /// 高级示例: UI预加载
        /// </summary>
        private async Task AdvancedExample_Preload()
        {
            Debug.Log("\n--- 高级示例: UI预加载 ---");

            // 预加载常用UI
            await _uiManager.PreloadAsync<MainMenuPanel>();
            await _uiManager.PreloadAsync<ConfirmDialog>();

            // 批量预加载
            await _uiManager.PreloadBatchAsync(
                typeof(MainMenuPanel),
                typeof(ConfirmDialog),
                typeof(PlayerInfoPanel)
            );

            Debug.Log("UI预加载完成");
        }

        /// <summary>
        /// 高级示例: 设置层级交互性
        /// </summary>
        private void AdvancedExample_LayerInteraction()
        {
            Debug.Log("\n--- 高级示例: 设置层级交互性 ---");

            // 禁用Normal层的交互（比如打开加载界面时）
            _uiManager.SetLayerInteractable(UILayer.Normal, false);

            // 恢复交互
            _uiManager.SetLayerInteractable(UILayer.Normal, true);
        }

        /// <summary>
        /// 高级示例: 关闭指定层级的所有UI
        /// </summary>
        private void AdvancedExample_CloseLayer()
        {
            Debug.Log("\n--- 高级示例: 关闭指定层级的所有UI ---");

            // 关闭所有弹窗
            _uiManager.CloseAll(UILayer.Popup);

            // 关闭所有UI
            _uiManager.CloseAll();
        }

        #endregion
    }

    #region 测试用的简单UI面板

    // 这些是用于演示导航栈的简单UI面板
    // 实际使用时应该创建完整的UI预制体

    /*
    public class UIPanelA : UIPanel
    {
        public override UILayer Layer => UILayer.Normal;
        public override bool UseStack => true;
    }

    public class UIPanelB : UIPanel
    {
        public override UILayer Layer => UILayer.Normal;
        public override bool UseStack => true;
    }

    public class UIPanelC : UIPanel
    {
        public override UILayer Layer => UILayer.Normal;
        public override bool UseStack => true;
    }
    */

    #endregion
}