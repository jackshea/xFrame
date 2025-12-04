using UnityEngine;
using UnityEngine.UI;
using VContainer;
using xFrame.Runtime.UI;
using xFrame.Runtime.EventBus;

namespace xFrame.Examples.UI
{
    /// <summary>
    /// 主菜单面板示例
    /// 展示如何创建一个基本的UI面板
    /// </summary>
    public class MainMenuPanel : UIPanel
    {
        #region UI组件

        [Header("UI组件")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button achievementsButton;
        [SerializeField] private Button exitButton;

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

        #region UI配置

        /// <summary>
        /// 设置UI层级为普通层
        /// </summary>
        public override UILayer Layer => UILayer.Normal;

        /// <summary>
        /// 打开时关闭其他同层UI
        /// </summary>
        public override bool CloseOthers => true;

        /// <summary>
        /// 主菜单不使用导航栈（作为根UI）
        /// </summary>
        public override bool UseStack => false;

        /// <summary>
        /// 允许缓存到对象池
        /// </summary>
        public override bool Cacheable => true;

        #endregion

        #region 生命周期回调

        /// <summary>
        /// UI创建时调用（仅一次）
        /// </summary>
        protected override void OnCreate()
        {
            base.OnCreate();

            // 绑定按钮事件
            if (startButton != null)
                startButton.onClick.AddListener(OnStartButtonClicked);

            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsButtonClicked);

            if (achievementsButton != null)
                achievementsButton.onClick.AddListener(OnAchievementsButtonClicked);

            if (exitButton != null)
                exitButton.onClick.AddListener(OnExitButtonClicked);

            Debug.Log("[MainMenuPanel] UI创建完成");
        }

        /// <summary>
        /// UI打开时调用
        /// </summary>
        protected override void OnOpen(object data)
        {
            base.OnOpen(data);

            // 处理传入的数据
            if (data is MainMenuData menuData)
            {
                Debug.Log($"[MainMenuPanel] 打开主菜单，玩家名称: {menuData.PlayerName}");
            }

            Debug.Log("[MainMenuPanel] 主菜单已打开");
        }

        /// <summary>
        /// UI显示时调用
        /// 场景1: 打开后自动调用
        /// 场景2: 从导航栈恢复时调用（被其他UI遮挡后返回）
        /// </summary>
        protected override void OnShow()
        {
            base.OnShow();
            Debug.Log("[MainMenuPanel] 主菜单显示");

            // 在这里可以：
            // - 刷新UI显示
            // - 播放显示动画
            // - 恢复背景音乐
            // - 重新开始更新逻辑
        }

        /// <summary>
        /// UI隐藏时调用
        /// 场景1: 被其他UI遮挡压入栈时调用
        /// 场景2: 关闭前调用
        /// </summary>
        protected override void OnHide()
        {
            base.OnHide();
            Debug.Log("[MainMenuPanel] 主菜单隐藏");

            // 在这里可以：
            // - 暂停动画
            // - 降低背景音乐音量
            // - 停止更新逻辑
            // - 保存临时状态
        }

        /// <summary>
        /// UI关闭时调用
        /// </summary>
        protected override void OnClose()
        {
            base.OnClose();
            Debug.Log("[MainMenuPanel] 主菜单已关闭");
        }

        /// <summary>
        /// UI销毁时调用（仅一次）
        /// </summary>
        protected override void OnUIDestroy()
        {
            // 取消按钮事件绑定
            if (startButton != null)
                startButton.onClick.RemoveListener(OnStartButtonClicked);

            if (settingsButton != null)
                settingsButton.onClick.RemoveListener(OnSettingsButtonClicked);

            if (achievementsButton != null)
                achievementsButton.onClick.RemoveListener(OnAchievementsButtonClicked);

            if (exitButton != null)
                exitButton.onClick.RemoveListener(OnExitButtonClicked);

            base.OnUIDestroy();
            Debug.Log("[MainMenuPanel] UI销毁完成");
        }

        #endregion

        #region 按钮事件处理

        /// <summary>
        /// 开始游戏按钮点击
        /// </summary>
        private void OnStartButtonClicked()
        {
            Debug.Log("[MainMenuPanel] 开始游戏");

            // 通过事件总线发送游戏开始事件
            xFrameEventBus.Raise(new GameStartEvent());

            // 关闭主菜单，打开游戏界面
            // _uiManager.OpenAsync<GamePlayPanel>();
        }

        /// <summary>
        /// 设置按钮点击
        /// </summary>
        private async void OnSettingsButtonClicked()
        {
            Debug.Log("[MainMenuPanel] 打开设置");

            // 打开设置窗口
            // await _uiManager.OpenAsync<SettingsWindow>();
        }

        /// <summary>
        /// 成就按钮点击
        /// </summary>
        private async void OnAchievementsButtonClicked()
        {
            Debug.Log("[MainMenuPanel] 打开成就");

            // 打开成就面板
            // await _uiManager.OpenAsync<AchievementsPanel>();
        }

        /// <summary>
        /// 退出按钮点击
        /// </summary>
        private async void OnExitButtonClicked()
        {
            Debug.Log("[MainMenuPanel] 退出游戏");

            // 打开确认对话框
            var dialogData = new ConfirmDialogData
            {
                Title = "退出游戏",
                Message = "确定要退出游戏吗？",
                OnConfirm = () =>
                {
                    Debug.Log("确认退出游戏");
                    Application.Quit();
                },
                OnCancel = () =>
                {
                    Debug.Log("取消退出");
                }
            };

            // await _uiManager.OpenAsync<ConfirmDialog>(dialogData);
        }

        #endregion
    }

    #region 数据类

    /// <summary>
    /// 主菜单数据
    /// </summary>
    public class MainMenuData
    {
        public string PlayerName { get; set; }
        public int Level { get; set; }
    }

    /// <summary>
    /// 游戏开始事件
    /// </summary>
    public struct GameStartEvent : IEvent
    {
    }

    #endregion
}
