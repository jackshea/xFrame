using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using xFrame.Runtime.UI;
using xFrame.Runtime.EventBus;
using xFrame.Examples.UI.Components;

namespace xFrame.Examples.UI
{
    /// <summary>
    /// 背包面板示例
    /// 展示如何使用组件系统和父子通讯
    /// </summary>
    public class InventoryPanel : UIPanel
    {
        #region UI组件

        [Header("UI引用")]
        [SerializeField] private Transform slotContainer;
        [SerializeField] private ItemSlotComponent slotPrefab;
        [SerializeField] private Button closeButton;

        [Header("配置")]
        [SerializeField] private int slotCount = 20;

        #endregion

        #region 私有字段

        private List<ItemSlotComponent> _itemSlots = new List<ItemSlotComponent>();
        private ItemSlotComponent _selectedSlot;

        #endregion

        #region UI配置

        public override UILayer Layer => UILayer.Normal;
        public override bool UseStack => true;
        public override bool Cacheable => true;

        #endregion

        #region 生命周期

        protected override void OnCreate()
        {
            base.OnCreate();

            // 创建物品槽
            CreateItemSlots();

            // 绑定按钮
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(OnCloseButtonClicked);
            }

            // 订阅组件事件（方式1：通过事件总线）
            SubscribeComponentEvents();

            Debug.Log("[InventoryPanel] 创建完成");
        }

        protected override void OnOpen(object data)
        {
            base.OnOpen(data);

            // 接收背包数据
            if (data is InventoryData inventoryData)
            {
                LoadInventoryData(inventoryData);
            }

            Debug.Log("[InventoryPanel] 打开背包");
        }

        protected override void OnShow()
        {
            base.OnShow();
            Debug.Log("[InventoryPanel] 显示背包");

            // 刷新所有槽位显示
            RefreshAllSlots();
        }

        protected override void OnHide()
        {
            base.OnHide();
            Debug.Log("[InventoryPanel] 隐藏背包");

            // 清除选中状态
            ClearSelection();
        }

        protected override void OnClose()
        {
            base.OnClose();
            Debug.Log("[InventoryPanel] 关闭背包");
        }

        protected override void OnUIDestroy()
        {
            // 取消事件订阅
            UnsubscribeComponentEvents();

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(OnCloseButtonClicked);
            }

            base.OnUIDestroy();
            Debug.Log("[InventoryPanel] 销毁完成");
        }

        #endregion

        #region 组件管理

        /// <summary>
        /// 创建物品槽组件
        /// </summary>
        private void CreateItemSlots()
        {
            if (slotPrefab == null || slotContainer == null)
            {
                Debug.LogError("[InventoryPanel] 缺少必要的引用");
                return;
            }

            for (int i = 0; i < slotCount; i++)
            {
                // 实例化槽位
                var slotInstance = Instantiate(slotPrefab, slotContainer);

                // 注册到组件管理器
                ComponentManager.RegisterComponent(slotInstance);

                // 添加到本地列表
                _itemSlots.Add(slotInstance);

                // 初始显示
                slotInstance.Show();
            }

            Debug.Log($"[InventoryPanel] 创建了 {slotCount} 个物品槽");
        }

        /// <summary>
        /// 加载背包数据
        /// </summary>
        private void LoadInventoryData(InventoryData data)
        {
            if (data == null || data.Items == null) return;

            for (int i = 0; i < data.Items.Count && i < _itemSlots.Count; i++)
            {
                _itemSlots[i].SetData(data.Items[i]);
            }

            Debug.Log($"[InventoryPanel] 加载了 {data.Items.Count} 个物品");
        }

        /// <summary>
        /// 刷新所有槽位
        /// </summary>
        private void RefreshAllSlots()
        {
            foreach (var slot in _itemSlots)
            {
                slot.Refresh();
            }
        }

        #endregion

        #region 组件事件处理（方式1：事件总线）

        /// <summary>
        /// 订阅组件事件
        /// </summary>
        private void SubscribeComponentEvents()
        {
            // 订阅物品槽点击事件
            xFrameEventBus.SubscribeTo<UIComponentEventWrapper<ItemSlotClickedEvent>>(OnItemSlotClicked);
        }

        /// <summary>
        /// 取消订阅组件事件
        /// </summary>
        private void UnsubscribeComponentEvents()
        {
            xFrameEventBus.UnsubscribeFrom<UIComponentEventWrapper<ItemSlotClickedEvent>>(OnItemSlotClicked);
        }

        /// <summary>
        /// 物品槽点击事件处理
        /// </summary>
        private void OnItemSlotClicked(ref UIComponentEventWrapper<ItemSlotClickedEvent> wrapper)
        {
            // 通过ComponentId区分是哪个槽位
            Debug.Log($"[InventoryPanel] 槽位被点击: ComponentId={wrapper.ComponentId}, " +
                      $"SlotIndex={wrapper.Event.SlotIndex}, " +
                      $"Item={wrapper.Event.ItemData?.ItemName}");

            // 获取点击的槽位组件
            var clickedSlot = ComponentManager.GetComponent<ItemSlotComponent>(wrapper.ComponentId);
            if (clickedSlot == null)
            {
                Debug.LogWarning("[InventoryPanel] 找不到点击的槽位组件");
                return;
            }

            // 处理选中逻辑
            HandleSlotSelection(clickedSlot);
        }

        #endregion

        #region 业务逻辑

        /// <summary>
        /// 处理槽位选中
        /// </summary>
        private void HandleSlotSelection(ItemSlotComponent slot)
        {
            if (slot == null) return;

            // 如果已有选中槽位
            if (_selectedSlot != null)
            {
                if (_selectedSlot == slot)
                {
                    // 点击同一个槽位，取消选中
                    _selectedSlot.SetSelected(false);
                    _selectedSlot = null;
                    Debug.Log("[InventoryPanel] 取消选中");
                }
                else
                {
                    // 点击不同槽位，尝试交换
                    SwapSlots(_selectedSlot, slot);
                    _selectedSlot.SetSelected(false);
                    _selectedSlot = null;
                }
            }
            else
            {
                // 选中新槽位
                if (!slot.IsEmpty())
                {
                    _selectedSlot = slot;
                    _selectedSlot.SetSelected(true);
                    Debug.Log($"[InventoryPanel] 选中槽位: {slot.GetCurrentData()?.ItemName}");
                }
            }
        }

        /// <summary>
        /// 交换两个槽位的物品
        /// </summary>
        private void SwapSlots(ItemSlotComponent slot1, ItemSlotComponent slot2)
        {
            if (slot1 == null || slot2 == null) return;

            var data1 = slot1.GetCurrentData();
            var data2 = slot2.GetCurrentData();

            slot1.SetData(data2);
            slot2.SetData(data1);

            Debug.Log($"[InventoryPanel] 交换物品: {data1?.ItemName} <-> {data2?.ItemName}");
        }

        /// <summary>
        /// 清除选中状态
        /// </summary>
        private void ClearSelection()
        {
            if (_selectedSlot != null)
            {
                _selectedSlot.SetSelected(false);
                _selectedSlot = null;
            }
        }

        /// <summary>
        /// 添加物品到背包
        /// </summary>
        /// <param name="itemData">物品数据</param>
        /// <returns>是否添加成功</returns>
        public bool AddItem(ItemSlotData itemData)
        {
            if (itemData == null) return false;

            // 查找空槽位
            foreach (var slot in _itemSlots)
            {
                if (slot.IsEmpty())
                {
                    slot.SetData(itemData);
                    Debug.Log($"[InventoryPanel] 添加物品: {itemData.ItemName}");
                    return true;
                }
            }

            Debug.LogWarning("[InventoryPanel] 背包已满，无法添加物品");
            return false;
        }

        /// <summary>
        /// 移除物品
        /// </summary>
        /// <param name="itemId">物品ID</param>
        /// <param name="count">数量</param>
        /// <returns>是否移除成功</returns>
        public bool RemoveItem(int itemId, int count = 1)
        {
            foreach (var slot in _itemSlots)
            {
                var data = slot.GetCurrentData();
                if (data != null && data.ItemId == itemId)
                {
                    if (data.Count > count)
                    {
                        // 减少数量
                        data.Count -= count;
                        slot.Refresh();
                    }
                    else
                    {
                        // 清空槽位
                        slot.SetData(null);
                    }

                    Debug.Log($"[InventoryPanel] 移除物品: ID={itemId}, Count={count}");
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region 按钮事件

        private void OnCloseButtonClicked()
        {
            // 通过UIManager关闭
            // _uiManager.Close(this);
            Debug.Log("[InventoryPanel] 点击关闭按钮");
        }

        #endregion

        #region 调试方法

        /// <summary>
        /// 获取组件统计信息
        /// </summary>
        [ContextMenu("打印组件统计")]
        private void PrintComponentStats()
        {
            Debug.Log($"=== 背包面板组件统计 ===");
            Debug.Log($"总组件数: {ComponentManager.GetComponentCount()}");
            Debug.Log($"ItemSlotComponent数量: {ComponentManager.GetComponentCountOfType<ItemSlotComponent>()}");

            var slots = ComponentManager.GetComponentsOfType<ItemSlotComponent>();
            Debug.Log($"槽位列表:");
            foreach (var slot in slots)
            {
                Debug.Log($"  - ID: {slot.ComponentId}, Empty: {slot.IsEmpty()}");
            }
        }

        #endregion
    }

    #region 数据类型

    /// <summary>
    /// 背包数据
    /// </summary>
    public class InventoryData
    {
        public List<ItemSlotData> Items { get; set; } = new List<ItemSlotData>();

        /// <summary>
        /// 创建测试数据
        /// </summary>
        public static InventoryData CreateTestData()
        {
            var data = new InventoryData();

            // 添加一些测试物品
            data.Items.Add(new ItemSlotData
            {
                ItemId = 1,
                ItemName = "生命药水",
                Count = 5
            });

            data.Items.Add(new ItemSlotData
            {
                ItemId = 2,
                ItemName = "魔法药水",
                Count = 3
            });

            data.Items.Add(new ItemSlotData
            {
                ItemId = 3,
                ItemName = "铁剑",
                Count = 1
            });

            return data;
        }
    }

    #endregion
}
