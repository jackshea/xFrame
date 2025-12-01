using UnityEngine;
using UnityEngine.UI;
using TMPro;
using xFrame.Runtime.UI;

namespace xFrame.Examples.UI.Components
{
    /// <summary>
    /// 物品槽组件示例
    /// 展示可复用的UI组件
    /// </summary>
    public class ItemSlotComponent : UIComponent
    {
        #region UI组件

        [Header("UI引用")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private Button slotButton;
        [SerializeField] private GameObject selectedFrame;

        #endregion

        #region 私有字段

        private ItemSlotData _currentData;
        private bool _isSelected;

        #endregion

        #region 生命周期

        protected override void OnInitialize()
        {
            base.OnInitialize();

            // 绑定按钮事件
            if (slotButton != null)
            {
                slotButton.onClick.AddListener(OnSlotClicked);
            }

            // 初始化状态
            SetSelected(false);

            Debug.Log($"[ItemSlotComponent] 初始化完成, ID: {ComponentId}");
        }

        protected override void OnSetData(object data)
        {
            base.OnSetData(data);

            if (data is ItemSlotData itemData)
            {
                _currentData = itemData;
                UpdateDisplay();
            }
        }

        protected override void OnShow()
        {
            base.OnShow();
            Debug.Log($"[ItemSlotComponent] 显示, ID: {ComponentId}");
        }

        protected override void OnHide()
        {
            base.OnHide();
            Debug.Log($"[ItemSlotComponent] 隐藏, ID: {ComponentId}");
        }

        protected override void OnRefresh()
        {
            base.OnRefresh();
            UpdateDisplay();
        }

        protected override void OnReset()
        {
            base.OnReset();
            _currentData = null;
            _isSelected = false;
            SetSelected(false);
        }

        protected override void OnDestroyComponent()
        {
            base.OnDestroyComponent();

            // 取消按钮绑定
            if (slotButton != null)
            {
                slotButton.onClick.RemoveListener(OnSlotClicked);
            }

            Debug.Log($"[ItemSlotComponent] 销毁, ID: {ComponentId}");
        }

        #endregion

        #region 显示更新

        /// <summary>
        /// 更新显示
        /// </summary>
        private void UpdateDisplay()
        {
            if (_currentData == null)
            {
                // 显示空槽
                if (iconImage != null)
                    iconImage.enabled = false;

                if (countText != null)
                    countText.text = "";

                return;
            }

            // 显示物品
            if (iconImage != null)
            {
                iconImage.enabled = true;
                iconImage.sprite = _currentData.Icon;
            }

            if (countText != null)
            {
                if (_currentData.Count > 1)
                {
                    countText.text = _currentData.Count.ToString();
                }
                else
                {
                    countText.text = "";
                }
            }
        }

        /// <summary>
        /// 设置选中状态
        /// </summary>
        /// <param name="selected">是否选中</param>
        public void SetSelected(bool selected)
        {
            _isSelected = selected;

            if (selectedFrame != null)
            {
                selectedFrame.SetActive(selected);
            }
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 槽位点击
        /// </summary>
        private void OnSlotClicked()
        {
            Debug.Log($"[ItemSlotComponent] 槽位被点击, ID: {ComponentId}, 物品: {_currentData?.ItemName}");

            // 发送组件事件（通过事件总线）
            SendEvent(new ItemSlotClickedEvent
            {
                ItemData = _currentData,
                SlotIndex = GetSiblingIndex()
            });
        }

        /// <summary>
        /// 获取槽位索引（在父对象中的位置）
        /// </summary>
        private int GetSiblingIndex()
        {
            return transform.GetSiblingIndex();
        }

        #endregion

        #region 公开方法

        /// <summary>
        /// 获取当前物品数据
        /// </summary>
        public ItemSlotData GetCurrentData()
        {
            return _currentData;
        }

        /// <summary>
        /// 是否为空槽
        /// </summary>
        public bool IsEmpty()
        {
            return _currentData == null;
        }

        #endregion
    }

    #region 数据类型

    /// <summary>
    /// 物品槽数据
    /// </summary>
    public class ItemSlotData
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public Sprite Icon { get; set; }
        public int Count { get; set; }
        public int MaxStack { get; set; } = 99;

        /// <summary>
        /// 是否可以堆叠
        /// </summary>
        public bool CanStack(ItemSlotData other)
        {
            return other != null && other.ItemId == ItemId && Count < MaxStack;
        }
    }

    /// <summary>
    /// 物品槽点击事件
    /// </summary>
    public struct ItemSlotClickedEvent : IUIComponentEvent
    {
        public ItemSlotData ItemData { get; set; }
        public int SlotIndex { get; set; }
    }

    #endregion
}
