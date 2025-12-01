using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VContainer;
using xFrame.Runtime.UI;
using xFrame.Runtime.EventBus;

namespace xFrame.Examples.UI
{
    /// <summary>
    /// 玩家信息面板示例
    /// 展示MVVM模式的使用
    /// </summary>
    public class PlayerInfoPanel : UIPanel
    {
        #region UI组件

        [Header("玩家信息")]
        [SerializeField] private Image avatarImage;
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI levelText;

        [Header("属性信息")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private Slider manaSlider;
        [SerializeField] private TextMeshProUGUI manaText;
        [SerializeField] private Slider expSlider;
        [SerializeField] private TextMeshProUGUI expText;

        [Header("货币信息")]
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI diamondText;

        [Header("按钮")]
        [SerializeField] private Button closeButton;

        #endregion

        #region 依赖注入

        private IUIManager _uiManager;

        [Inject]
        public void Construct(IUIManager uiManager)
        {
            _uiManager = uiManager;
        }

        #endregion

        #region UI配置

        public override UILayer Layer => UILayer.Normal;
        public override bool UseStack => true;
        public override bool Cacheable => true;

        #endregion

        #region 私有字段

        private PlayerInfoViewModel _viewModel;

        #endregion

        #region 生命周期回调

        protected override void OnCreate()
        {
            base.OnCreate();

            // 绑定按钮事件
            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseButtonClicked);

            // 订阅玩家数据更新事件
            xFrameEventBus.SubscribeTo<PlayerDataUpdatedEvent>(OnPlayerDataUpdated);

            Debug.Log("[PlayerInfoPanel] UI创建完成");
        }

        protected override void OnOpen(object data)
        {
            base.OnOpen(data);

            // 接收ViewModel数据
            if (data is PlayerInfoViewModel viewModel)
            {
                _viewModel = viewModel;
                UpdateView();
            }

            Debug.Log("[PlayerInfoPanel] 玩家信息面板已打开");
        }

        protected override void OnShow()
        {
            base.OnShow();
            Debug.Log("[PlayerInfoPanel] 玩家信息面板显示");

            // 显示时刷新数据（可能在被遮挡期间数据发生了变化）
            if (_viewModel != null)
            {
                UpdateView();
            }
        }

        protected override void OnHide()
        {
            base.OnHide();
            Debug.Log("[PlayerInfoPanel] 玩家信息面板隐藏");

            // 隐藏时可以停止数据更新，节省性能
        }

        protected override void OnClose()
        {
            base.OnClose();
            _viewModel = null;
            Debug.Log("[PlayerInfoPanel] 玩家信息面板已关闭");
        }

        protected override void OnDestroy()
        {
            // 取消事件订阅
            xFrameEventBus.UnsubscribeFrom<PlayerDataUpdatedEvent>(OnPlayerDataUpdated);

            if (closeButton != null)
                closeButton.onClick.RemoveListener(OnCloseButtonClicked);

            base.OnDestroy();
            Debug.Log("[PlayerInfoPanel] UI销毁完成");
        }

        #endregion

        #region 视图更新

        /// <summary>
        /// 更新视图显示
        /// </summary>
        private void UpdateView()
        {
            if (_viewModel == null) return;

            // 更新玩家信息
            if (playerNameText != null)
                playerNameText.text = _viewModel.PlayerName;

            if (levelText != null)
                levelText.text = $"Lv.{_viewModel.Level}";

            // 更新属性
            UpdateHealthBar();
            UpdateManaBar();
            UpdateExpBar();

            // 更新货币
            if (goldText != null)
                goldText.text = _viewModel.Gold.ToString("N0");

            if (diamondText != null)
                diamondText.text = _viewModel.Diamond.ToString();

            // 更新头像（如果有）
            if (avatarImage != null && _viewModel.AvatarSprite != null)
                avatarImage.sprite = _viewModel.AvatarSprite;
        }

        /// <summary>
        /// 更新生命值显示
        /// </summary>
        private void UpdateHealthBar()
        {
            if (healthSlider != null)
            {
                healthSlider.value = (float)_viewModel.CurrentHealth / _viewModel.MaxHealth;
            }

            if (healthText != null)
            {
                healthText.text = $"{_viewModel.CurrentHealth}/{_viewModel.MaxHealth}";
            }
        }

        /// <summary>
        /// 更新魔法值显示
        /// </summary>
        private void UpdateManaBar()
        {
            if (manaSlider != null)
            {
                manaSlider.value = (float)_viewModel.CurrentMana / _viewModel.MaxMana;
            }

            if (manaText != null)
            {
                manaText.text = $"{_viewModel.CurrentMana}/{_viewModel.MaxMana}";
            }
        }

        /// <summary>
        /// 更新经验值显示
        /// </summary>
        private void UpdateExpBar()
        {
            if (expSlider != null)
            {
                expSlider.value = (float)_viewModel.CurrentExp / _viewModel.ExpToNextLevel;
            }

            if (expText != null)
            {
                expText.text = $"{_viewModel.CurrentExp}/{_viewModel.ExpToNextLevel}";
            }
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 关闭按钮点击
        /// </summary>
        private void OnCloseButtonClicked()
        {
            _uiManager.Close(this);
        }

        /// <summary>
        /// 玩家数据更新事件处理
        /// </summary>
        private void OnPlayerDataUpdated(ref PlayerDataUpdatedEvent evt)
        {
            // 如果更新的是当前显示的玩家，刷新视图
            if (_viewModel != null && evt.ViewModel != null)
            {
                _viewModel = evt.ViewModel;
                UpdateView();
                Debug.Log("[PlayerInfoPanel] 玩家数据已更新");
            }
        }

        #endregion

        #region 动画（可选）

        protected override void PlayOpenAnimation()
        {
            // 可以添加滑入动画
            if (RectTransform != null)
            {
                // 从右侧滑入
                Vector2 startPos = RectTransform.anchoredPosition + new Vector2(Screen.width, 0);
                RectTransform.anchoredPosition = startPos;

                // 使用协程实现简单动画
                StartCoroutine(SlideAnimation(startPos, Vector2.zero, AnimationDuration));
            }
        }

        protected override void PlayCloseAnimation()
        {
            // 滑出动画
            if (RectTransform != null)
            {
                Vector2 endPos = RectTransform.anchoredPosition + new Vector2(Screen.width, 0);
                StartCoroutine(SlideAnimation(RectTransform.anchoredPosition, endPos, AnimationDuration));
            }
        }

        private System.Collections.IEnumerator SlideAnimation(Vector2 from, Vector2 to, float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // 使用EaseOutCubic曲线
                t = 1f - Mathf.Pow(1f - t, 3f);

                RectTransform.anchoredPosition = Vector2.Lerp(from, to, t);
                yield return null;
            }

            RectTransform.anchoredPosition = to;
        }

        #endregion
    }

    #region ViewModel

    /// <summary>
    /// 玩家信息ViewModel
    /// MVVM模式中的数据模型
    /// </summary>
    public class PlayerInfoViewModel
    {
        // 基本信息
        public string PlayerName { get; set; }
        public int Level { get; set; }
        public Sprite AvatarSprite { get; set; }

        // 属性
        public int CurrentHealth { get; set; }
        public int MaxHealth { get; set; }
        public int CurrentMana { get; set; }
        public int MaxMana { get; set; }
        public int CurrentExp { get; set; }
        public int ExpToNextLevel { get; set; }

        // 货币
        public int Gold { get; set; }
        public int Diamond { get; set; }

        /// <summary>
        /// 生命值百分比
        /// </summary>
        public float HealthPercentage => MaxHealth > 0 ? (float)CurrentHealth / MaxHealth : 0f;

        /// <summary>
        /// 魔法值百分比
        /// </summary>
        public float ManaPercentage => MaxMana > 0 ? (float)CurrentMana / MaxMana : 0f;

        /// <summary>
        /// 经验值百分比
        /// </summary>
        public float ExpPercentage => ExpToNextLevel > 0 ? (float)CurrentExp / ExpToNextLevel : 0f;

        /// <summary>
        /// 创建示例数据
        /// </summary>
        public static PlayerInfoViewModel CreateSample()
        {
            return new PlayerInfoViewModel
            {
                PlayerName = "测试玩家",
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
        }
    }

    #endregion

    #region Events

    /// <summary>
    /// 玩家数据更新事件
    /// </summary>
    public struct PlayerDataUpdatedEvent : IEvent
    {
        public PlayerInfoViewModel ViewModel { get; set; }

        public PlayerDataUpdatedEvent(PlayerInfoViewModel viewModel)
        {
            ViewModel = viewModel;
        }
    }

    #endregion
}
