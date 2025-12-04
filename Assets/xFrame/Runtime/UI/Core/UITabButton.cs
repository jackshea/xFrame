using UnityEngine;
using UnityEngine.UI;

namespace xFrame.Runtime.UI
{
    /// <summary>
    /// UI标签按钮组件
    /// 用于切换UITabContainer中的页面
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UITabButton : UIComponent
    {
        [Header("标签按钮配置")]
        [SerializeField]
        [Tooltip("按钮文本")]
        private Text buttonText;

        [SerializeField]
        [Tooltip("按钮图标")]
        private Image buttonIcon;

        [SerializeField]
        [Tooltip("选中状态的视觉表现")]
        private GameObject selectedVisual;

        [Header("颜色配置")]
        [SerializeField]
        [Tooltip("正常状态颜色")]
        private Color normalColor = Color.white;

        [SerializeField]
        [Tooltip("选中状态颜色")]
        private Color selectedColor = Color.yellow;

        /// <summary>
        /// 按钮组件
        /// </summary>
        private Button _button;

        /// <summary>
        /// 所属的容器
        /// </summary>
        private UITabContainer _container;

        /// <summary>
        /// 对应的页面索引
        /// </summary>
        public int PageIndex { get; private set; } = -1;

        /// <summary>
        /// 是否选中
        /// </summary>
        public bool IsSelected { get; private set; }

        /// <summary>
        /// 初始化
        /// </summary>
        protected override void OnInitialize()
        {
            base.OnInitialize();

            _button = GetComponent<Button>();

            // 绑定点击事件
            _button.onClick.AddListener(OnButtonClicked);

            // 初始状态
            SetSelected(false);
        }

        /// <summary>
        /// 设置按钮数据
        /// </summary>
        /// <param name="data">按钮数据</param>
        protected override void OnSetData(object data)
        {
            base.OnSetData(data);

            if (data is TabButtonData buttonData)
            {
                PageIndex = buttonData.PageIndex;
                _container = buttonData.Container;

                // 设置文本
                if (buttonText != null && !string.IsNullOrEmpty(buttonData.Text)) buttonText.text = buttonData.Text;

                // 设置图标
                if (buttonIcon != null && buttonData.Icon != null)
                {
                    buttonIcon.sprite = buttonData.Icon;
                    buttonIcon.gameObject.SetActive(true);
                }
                else if (buttonIcon != null)
                {
                    buttonIcon.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 按钮点击
        /// </summary>
        private void OnButtonClicked()
        {
            if (_container != null && PageIndex >= 0)
            {
                // 发送事件
                SendEvent(new TabButtonClickedEvent
                {
                    PageIndex = PageIndex
                });

                // 切换页面
                _container.SwitchPage(PageIndex);
            }
        }

        /// <summary>
        /// 设置选中状态
        /// </summary>
        /// <param name="selected">是否选中</param>
        public void SetSelected(bool selected)
        {
            IsSelected = selected;

            // 更新视觉表现
            if (selectedVisual != null) selectedVisual.SetActive(selected);

            // 更新颜色
            if (_button != null)
            {
                var colors = _button.colors;
                colors.normalColor = selected ? selectedColor : normalColor;
                _button.colors = colors;
            }

            if (buttonText != null) buttonText.color = selected ? selectedColor : normalColor;
        }

        /// <summary>
        /// 销毁组件
        /// </summary>
        protected override void OnDestroyComponent()
        {
            if (_button != null) _button.onClick.RemoveListener(OnButtonClicked);

            base.OnDestroyComponent();
        }
    }

    #region 数据和事件定义

    /// <summary>
    /// 标签按钮数据
    /// </summary>
    public class TabButtonData
    {
        /// <summary>
        /// 对应的页面索引
        /// </summary>
        public int PageIndex { get; set; }

        /// <summary>
        /// 按钮文本
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// 按钮图标
        /// </summary>
        public Sprite Icon { get; set; }

        /// <summary>
        /// 所属容器
        /// </summary>
        public UITabContainer Container { get; set; }
    }

    /// <summary>
    /// 标签按钮点击事件
    /// </summary>
    public struct TabButtonClickedEvent : IUIComponentEvent
    {
        /// <summary>
        /// 页面索引
        /// </summary>
        public int PageIndex { get; set; }
    }

    #endregion
}