# xFrame UI组件复用系统设计文档

## 目录
- [概述](#概述)
- [架构设计](#架构设计)
- [核心组件](#核心组件)
- [父子关系管理](#父子关系管理)
- [通讯机制](#通讯机制)
- [生命周期传递](#生命周期传递)
- [使用指南](#使用指南)
- [最佳实践](#最佳实践)

---

## 概述

UI组件复用系统是xFrame UI框架的扩展，提供了可复用的UI子组件机制。通过清晰的父子关系和事件通讯系统，实现了高度模块化和可维护的UI架构。

### 核心特性

✅ **单向依赖** - 父组件知道子组件，子组件不知道父组件  
✅ **组件复用** - 同一组件类型可创建多个实例  
✅ **事件通讯** - 子组件通过事件总线发送全局事件，父组件订阅接收  
✅ **直接调用** - 父组件可以直接调用子组件的公开方法  
✅ **生命周期传递** - 父组件生命周期自动影响子组件  
✅ **统一管理** - 通过组件管理器统一管理所有子组件

---

## 架构设计

### 系统架构图

```
┌──────────────────────────────────────────┐
│            UIView (父组件)                │
│  ┌────────────────────────────────────┐  │
│  │    UIComponentManager              │  │
│  │  - 组件注册/注销                    │  │
│  │  - 组件查询                         │  │
│  │  - 生命周期传递                     │  │
│  └────────────────────────────────────┘  │
│                    │                      │
│        ┌───────────┼───────────┐         │
│        ▼           ▼           ▼         │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐    │
│  │Component│ │Component│ │Component│    │
│  │  #1     │ │  #2     │ │  #3     │    │
│  └─────────┘ └─────────┘ └─────────┘    │
│        │           │           │         │
└────────┼───────────┼───────────┼─────────┘
         │           │           │
         └───────────┴───────────┘
                     │
              ┌──────▼──────┐
              │ Event Bus   │
              │ (事件总线)   │
              └─────────────┘
```

### 依赖关系

```
父组件 (UIView)
  ├─ 持有: UIComponentManager
  ├─ 持有: 子组件引用列表（直接引用）
  ├─ 订阅: UIComponentEventWrapper<T>（接收事件）
  └─ 可以: 直接调用子组件方法

子组件 (UIComponent)
  ├─ 不持有: 父组件引用
  ├─ 发送: UIComponentEventWrapper<T>（全局事件）
  ├─ 通过: ComponentId区分实例
  └─ 不知道: 谁在监听事件
```

---

## 核心组件

### 1. UIComponent (组件基类)

所有可复用UI组件的基类。

```csharp
/// <summary>
/// UI组件基类
/// 依赖关系：父组件知道子组件，子组件不知道父组件
/// </summary>
public abstract class UIComponent : MonoBehaviour
{
    // 组件唯一ID（自动生成）
    public string ComponentId { get; private set; }
    
    // 组件是否已初始化
    public bool IsInitialized { get; private set; }
    
    // 组件是否可见
    public bool IsVisible { get; private set; }
    
    // 生命周期
    protected virtual void OnInitialize() { }
    protected virtual void OnSetData(object data) { }
    protected virtual void OnShow() { }
    protected virtual void OnHide() { }
    protected virtual void OnRefresh() { }
    protected virtual void OnReset() { }
    protected virtual void OnDestroyComponent() { }
    
    // 事件通讯（发送全局事件）
    protected void SendEvent<T>(T componentEvent);
}
```

**组件生命周期：**
1. **Initialize** - 组件初始化（关联父UI）
2. **SetData** - 设置数据
3. **Show** - 显示组件
4. **Hide** - 隐藏组件
5. **Reset** - 重置状态（对象池回收前）
6. **DestroyComponent** - 销毁组件

### 2. UIComponentManager (组件管理器)

管理UIView下的所有子组件。

```csharp
/// <summary>
/// UI组件管理器
/// </summary>
public class UIComponentManager
{
    // 注册组件
    public void RegisterComponent<T>(T component);
    
    // 自动查找并注册所有子组件
    public void AutoRegisterComponents();
    
    // 通过ID获取组件
    public T GetComponent<T>(string componentId);
    
    // 获取指定类型的所有组件
    public List<T> GetComponentsOfType<T>();
    
    // 生命周期传递
    public void OnParentShow();
    public void OnParentHide();
    public void OnParentClose();
    public void OnParentDestroy();
    
    // 批量操作
    public void ShowAll();
    public void HideAll();
    public void RefreshAll();
}
```

### 3. UIView扩展

UIView增加了组件管理支持。

```csharp
public abstract class UIView : MonoBehaviour
{
    // 组件管理器
    protected UIComponentManager ComponentManager { get; private set; }
    
    // 在生命周期中自动传递给子组件
    // OnShow → ComponentManager.OnParentShow()
    // OnHide → ComponentManager.OnParentHide()
    // OnClose → ComponentManager.OnParentClose()
    // OnDestroy → ComponentManager.OnParentDestroy()
}
```

---

## 父子关系管理

### 单向依赖原则

**✅ 允许：** 父组件 → 子组件
- 父组件通过`ComponentManager`管理子组件
- 父组件可以直接调用子组件的公开方法
- 父组件订阅事件总线来接收子组件事件

**❌ 禁止：** 子组件 → 父组件
- 子组件不持有父组件引用
- 子组件通过事件总线发送全局事件（不直接调用父组件）

### 依赖关系示例

```csharp
// ✅ 正确：子组件通过事件总线发送事件（不知道父组件）
public class ItemSlotComponent : UIComponent
{
    private void OnSlotClicked()
    {
        // 通过事件总线发送全局事件
        SendEvent(new ItemSlotClickedEvent { ... });
        // 子组件不需要知道谁在监听这个事件
    }
}

// ✅ 正确：父组件订阅事件并管理子组件
public class InventoryPanel : UIPanel
{
    private List<ItemSlotComponent> _slots = new List<ItemSlotComponent>();
    
    protected override void OnCreate()
    {
        // 1. 创建并管理子组件
        for (int i = 0; i < 20; i++)
        {
            var slot = Instantiate(slotPrefab);
            ComponentManager.RegisterComponent(slot);
            _slots.Add(slot);  // 父组件持有子组件引用
        }
        
        // 2. 订阅组件事件
        xFrameEventBus.SubscribeTo<UIComponentEventWrapper<ItemSlotClickedEvent>>(
            OnItemSlotClicked);
    }
    
    private void OnItemSlotClicked(ref UIComponentEventWrapper<ItemSlotClickedEvent> evt)
    {
        // 通过ComponentId识别是哪个子组件
        var slot = ComponentManager.GetComponent<ItemSlotComponent>(evt.ComponentId);
        
        // 父组件可以直接调用子组件的公开方法
        slot.SetSelected(true);
    }
}

// ❌ 错误：子组件持有父组件引用
public class ItemSlotComponent : UIComponent
{
    private InventoryPanel _parentPanel; // 不要这样做！
    
    private void OnSlotClicked()
    {
        _parentPanel.HandleSlotClick(); // 不要直接调用父组件
    }
}
```

---

## 通讯机制

### 事件总线通讯（推荐且唯一方式）

**优点：** 完全解耦、支持多个监听者、子组件不需要知道父组件  
**说明：** 子组件通过事件总线发送全局事件，父组件订阅接收

#### 步骤：

1. **定义组件事件**
```csharp
/// <summary>
/// 物品槽点击事件
/// </summary>
public struct ItemSlotClickedEvent : IUIComponentEvent
{
    public ItemSlotData ItemData { get; set; }
    public int SlotIndex { get; set; }
}
```

2. **子组件发送事件**
```csharp
public class ItemSlotComponent : UIComponent
{
    private void OnSlotClicked()
    {
        // 发送全局事件（自动包装ComponentId）
        // 子组件不需要知道谁在监听
        SendEvent(new ItemSlotClickedEvent
        {
            ItemData = _currentData,
            SlotIndex = GetSiblingIndex()
        });
    }
}
```

3. **父组件订阅事件**
```csharp
public class InventoryPanel : UIPanel
{
    private List<ItemSlotComponent> _slots = new List<ItemSlotComponent>();
    
    protected override void OnCreate()
    {
        // 创建并持有子组件引用
        for (int i = 0; i < 20; i++)
        {
            var slot = Instantiate(slotPrefab);
            ComponentManager.RegisterComponent(slot);
            _slots.Add(slot);  // 父组件直接持有引用
        }
        
        // 订阅包装后的事件
        xFrameEventBus.SubscribeTo<UIComponentEventWrapper<ItemSlotClickedEvent>>(
            OnItemSlotClicked);
    }
    
    private void OnItemSlotClicked(
        ref UIComponentEventWrapper<ItemSlotClickedEvent> wrapper)
    {
        // wrapper.ComponentId - 组件ID
        // wrapper.ComponentType - 组件类型
        // wrapper.SourceComponent - 组件实例
        // wrapper.Event - 实际事件数据
        
        Debug.Log($"组件 {wrapper.ComponentId} 发送了事件");
        
        // 方式1：通过ComponentId获取具体组件
        var slot = ComponentManager.GetComponent<ItemSlotComponent>(
            wrapper.ComponentId);
        
        // 方式2：直接使用SourceComponent
        var slot2 = wrapper.SourceComponent as ItemSlotComponent;
        
        // 父组件可以直接调用子组件方法
        slot.SetSelected(true);
        slot.SetData(newData);
    }
    
    protected override void OnDestroy()
    {
        // 取消订阅
        xFrameEventBus.UnsubscribeFrom<UIComponentEventWrapper<ItemSlotClickedEvent>>(
            OnItemSlotClicked);
    }
}
```

### 父组件直接调用子组件（也是推荐方式）

由于父组件持有子组件引用，可以直接调用：

```csharp
public class InventoryPanel : UIPanel
{
    private List<ItemSlotComponent> _slots = new List<ItemSlotComponent>();
    
    // 父组件可以直接操作子组件
    public void SelectSlot(int index)
    {
        if (index >= 0 && index < _slots.Count)
        {
            _slots[index].SetSelected(true);
        }
    }
    
    public void LoadInventoryData(List<ItemData> items)
    {
        for (int i = 0; i < items.Count && i < _slots.Count; i++)
        {
            _slots[i].SetData(items[i]);
            _slots[i].Show();
        }
    }
}
```

### 事件包装器详解

```csharp
/// <summary>
/// UI组件事件包装器
/// 自动包含组件身份信息
/// </summary>
public struct UIComponentEventWrapper<T> : IEvent where T : struct, IUIComponentEvent
{
    /// <summary>
    /// 组件唯一ID - 用于区分同类型的多个组件
    /// 格式: "ComponentTypeName_GUID"
    /// 例如: "ItemSlotComponent_a1b2c3d4"
    /// </summary>
    public string ComponentId { get; set; }
    
    /// <summary>
    /// 组件类型 - 用于类型判断
    /// </summary>
    public Type ComponentType { get; set; }
    
    /// <summary>
    /// 源组件实例 - 可以直接访问组件
    /// </summary>
    public UIComponent SourceComponent { get; set; }
    
    /// <summary>
    /// 实际事件数据
    /// </summary>
    public T Event { get; set; }
}
```

---

## 生命周期传递

父组件的生命周期会自动传递给所有子组件。

### 传递规则

| 父组件生命周期 | 子组件响应 | 说明 |
|--------------|----------|------|
| **OnShow** | 调用所有可见子组件的`Show()` | 父UI显示时，恢复子组件显示状态 |
| **OnHide** | 调用所有可见子组件的`Hide()` | 父UI隐藏时，隐藏所有子组件 |
| **OnClose** | 调用所有子组件的`Reset()` | 父UI关闭时，重置所有子组件状态 |
| **OnDestroy** | 调用所有子组件的`DestroyComponent()` | 父UI销毁时，销毁所有子组件 |

### 生命周期流程图

```
父UI打开
  │
  ├─ OnCreate
  │   └─ 创建并注册子组件
  │
  ├─ OnOpen
  │   └─ 设置子组件数据
  │
  └─ OnShow
      └─ ComponentManager.OnParentShow()
          └─ 所有可见子组件.Show()

父UI被遮挡
  │
  └─ OnHide
      └─ ComponentManager.OnParentHide()
          └─ 所有可见子组件.Hide()

父UI恢复
  │
  └─ OnShow
      └─ ComponentManager.OnParentShow()
          └─ 所有可见子组件.Show()

父UI关闭
  │
  ├─ OnHide
  │   └─ 所有子组件.Hide()
  │
  └─ OnClose
      └─ ComponentManager.OnParentClose()
          └─ 所有子组件.Reset()

父UI销毁
  │
  └─ OnDestroy
      └─ ComponentManager.OnParentDestroy()
          └─ 所有子组件.DestroyComponent()
```

### 代码示例

```csharp
public class InventoryPanel : UIPanel
{
    protected override void OnShow()
    {
        base.OnShow(); // 重要！必须调用base
        
        // 此时ComponentManager会自动调用所有子组件的Show()
        // 你只需要处理父UI自己的显示逻辑
    }
    
    protected override void OnHide()
    {
        base.OnHide(); // 重要！必须调用base
        
        // 此时ComponentManager会自动调用所有子组件的Hide()
    }
}
```

---

## 使用指南

### 1. 创建可复用组件

```csharp
using UnityEngine;
using UnityEngine.UI;
using xFrame.Runtime.UI;

/// <summary>
/// 按钮组件示例
/// </summary>
public class CustomButtonComponent : UIComponent
{
    [SerializeField] private Button button;
    [SerializeField] private Text label;
    
    private ButtonData _data;
    
    protected override void OnInitialize()
    {
        base.OnInitialize();
        
        // 绑定按钮事件
        button.onClick.AddListener(OnButtonClicked);
    }
    
    protected override void OnSetData(object data)
    {
        base.OnSetData(data);
        
        if (data is ButtonData buttonData)
        {
            _data = buttonData;
            label.text = buttonData.Label;
        }
    }
    
    private void OnButtonClicked()
    {
        // 发送事件给父UI
        SendEventToParent(new ButtonClickedEvent
        {
            ButtonId = _data.Id
        });
    }
    
    protected override void OnDestroyComponent()
    {
        button.onClick.RemoveListener(OnButtonClicked);
        base.OnDestroyComponent();
    }
}

public class ButtonData
{
    public int Id { get; set; }
    public string Label { get; set; }
}

public struct ButtonClickedEvent : IUIComponentEvent
{
    public int ButtonId { get; set; }
}
```

### 2. 在父UI中使用组件

```csharp
public class MenuPanel : UIPanel
{
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private CustomButtonComponent buttonPrefab;
    
    private List<CustomButtonComponent> _buttons = new List<CustomButtonComponent>();
    
    protected override void OnCreate()
    {
        base.OnCreate();
        
        // 创建多个按钮
        CreateButtons();
        
        // 订阅按钮事件
        xFrameEventBus.SubscribeTo<UIComponentEventWrapper<ButtonClickedEvent>>(
            OnButtonClicked);
    }
    
    private void CreateButtons()
    {
        string[] labels = { "开始游戏", "设置", "退出" };
        
        for (int i = 0; i < labels.Length; i++)
        {
            // 实例化按钮
            var button = Instantiate(buttonPrefab, buttonContainer);
            
            // 注册到组件管理器
            ComponentManager.RegisterComponent(button);
            
            // 设置数据
            button.SetData(new ButtonData
            {
                Id = i,
                Label = labels[i]
            });
            
            // 显示按钮
            button.Show();
            
            _buttons.Add(button);
        }
    }
    
    private void OnButtonClicked(
        ref UIComponentEventWrapper<ButtonClickedEvent> wrapper)
    {
        Debug.Log($"按钮被点击: ID={wrapper.Event.ButtonId}");
        
        // 根据ButtonId执行不同逻辑
        switch (wrapper.Event.ButtonId)
        {
            case 0: // 开始游戏
                StartGame();
                break;
            case 1: // 设置
                OpenSettings();
                break;
            case 2: // 退出
                QuitGame();
                break;
        }
    }
    
    protected override void OnDestroy()
    {
        xFrameEventBus.UnsubscribeFrom<UIComponentEventWrapper<ButtonClickedEvent>>(
            OnButtonClicked);
        base.OnDestroy();
    }
}
```

### 3. 自动注册组件

如果组件已经在预制体中，可以自动注册：

```csharp
public class ShopPanel : UIPanel
{
    protected override void OnCreate()
    {
        base.OnCreate();
        
        // 自动查找并注册所有子组件
        ComponentManager.AutoRegisterComponents();
        
        // 设置数据
        var slots = ComponentManager.GetComponentsOfType<ShopSlotComponent>();
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].SetData(GetShopItemData(i));
        }
    }
}
```

---

## 最佳实践

### 1. 组件设计原则

#### ✅ 单一职责
```csharp
// ✅ 好的设计：一个组件只做一件事
public class ItemIconComponent : UIComponent
{
    // 只负责显示物品图标
}

public class ItemNameComponent : UIComponent
{
    // 只负责显示物品名称
}

// ❌ 不好的设计：一个组件做太多事
public class ItemEverythingComponent : UIComponent
{
    // 显示图标、名称、描述、价格、按钮...
}
```

#### ✅ 可配置性
```csharp
public class SlotComponent : UIComponent
{
    [Header("配置")]
    [SerializeField] private bool showCount = true;
    [SerializeField] private bool showRarity = true;
    [SerializeField] private Color highlightColor = Color.yellow;
    
    // 通过配置控制行为
}
```

#### ✅ 无状态依赖
```csharp
// ✅ 好的设计：组件不依赖外部状态
public class HealthBarComponent : UIComponent
{
    protected override void OnSetData(object data)
    {
        if (data is HealthData healthData)
        {
            UpdateHealthBar(healthData.Current, healthData.Max);
        }
    }
}

// ❌ 不好的设计：依赖外部静态状态
public class HealthBarComponent : UIComponent
{
    private void Update()
    {
        // 不要直接访问全局状态
        UpdateHealthBar(GameManager.Instance.PlayerHealth);
    }
}
```

### 2. 通讯最佳实践

#### 优先使用事件总线
```csharp
// ✅ 推荐：使用事件总线
SendEventToParent(new ItemClickedEvent { ... });

// ⚠️ 谨慎使用：直接消息（仅用于简单场景）
SendMessageToParent(new SimpleMessage { ... });

// ❌ 避免：直接调用父组件方法
// ParentView.SomeMethod(); // 不推荐！
```

#### 事件命名规范
```csharp
// ✅ 好的命名：描述性强
public struct ItemSlotClickedEvent : IUIComponentEvent { }
public struct SkillButtonPressedEvent : IUIComponentEvent { }
public struct InventoryItemDroppedEvent : IUIComponentEvent { }

// ❌ 不好的命名：含糊不清
public struct ClickEvent : IUIComponentEvent { }
public struct Event1 : IUIComponentEvent { }
```

### 3. 生命周期管理

#### 正确的初始化顺序
```csharp
public class MyComponent : UIComponent
{
    protected override void OnInitialize()
    {
        base.OnInitialize();
        
        // 1. 获取组件引用
        // 2. 绑定事件
        // 3. 初始化状态
        // 不要在这里设置数据！
    }
    
    protected override void OnSetData(object data)
    {
        base.OnSetData(data);
        
        // 在这里设置数据并更新显示
    }
}
```

#### 及时清理资源
```csharp
public class MyComponent : UIComponent
{
    protected override void OnDestroyComponent()
    {
        // 1. 取消事件订阅
        // 2. 释放资源
        // 3. 清空引用
        
        base.OnDestroyComponent();
    }
}
```

### 4. 性能优化

#### 使用对象池
```csharp
// 对于频繁创建/销毁的组件，使用对象池
public class ItemSlotPool
{
    private Stack<ItemSlotComponent> _pool = new Stack<ItemSlotComponent>();
    
    public ItemSlotComponent Get()
    {
        if (_pool.Count > 0)
        {
            var slot = _pool.Pop();
            slot.Show();
            return slot;
        }
        
        return Instantiate(_prefab);
    }
    
    public void Return(ItemSlotComponent slot)
    {
        slot.Reset();
        slot.Hide();
        _pool.Push(slot);
    }
}
```

#### 批量操作
```csharp
// ✅ 好的做法：批量设置数据
public void LoadItems(List<ItemData> items)
{
    var slots = ComponentManager.GetComponentsOfType<ItemSlotComponent>();
    
    for (int i = 0; i < items.Count && i < slots.Count; i++)
    {
        slots[i].SetData(items[i]);
    }
}

// ❌ 不好的做法：逐个查找和设置
public void LoadItems(List<ItemData> items)
{
    foreach (var item in items)
    {
        var slot = ComponentManager.GetComponentOfType<ItemSlotComponent>();
        slot.SetData(item);
    }
}
```

#### 延迟刷新
```csharp
public class InventoryPanel : UIPanel
{
    private bool _isDirty;
    
    public void MarkDirty()
    {
        _isDirty = true;
    }
    
    private void LateUpdate()
    {
        if (_isDirty)
        {
            ComponentManager.RefreshAll();
            _isDirty = false;
        }
    }
}
```

### 5. 调试技巧

#### 添加调试信息
```csharp
public class MyComponent : UIComponent
{
    protected override void OnInitialize()
    {
        base.OnInitialize();
        
        #if UNITY_EDITOR
        gameObject.name = $"{GetType().Name}_{ComponentId}";
        #endif
    }
}
```

#### 组件统计
```csharp
[ContextMenu("打印组件统计")]
private void PrintComponentStats()
{
    Debug.Log($"组件总数: {ComponentManager.GetComponentCount()}");
    
    var types = new Dictionary<Type, int>();
    foreach (var component in ComponentManager.GetAllComponents())
    {
        var type = component.GetType();
        types[type] = types.ContainsKey(type) ? types[type] + 1 : 1;
    }
    
    foreach (var kvp in types)
    {
        Debug.Log($"  {kvp.Key.Name}: {kvp.Value}");
    }
}
```

---

## 总结

### 核心优势

1. **清晰的架构** - 单向依赖，职责分离
2. **高度复用** - 组件可在多个UI中复用
3. **解耦通讯** - 通过事件系统，父子组件完全解耦
4. **组件识别** - 通过ComponentId区分同类型的多个实例
5. **生命周期自动管理** - 父组件生命周期自动传递给子组件

### 适用场景

- ✅ 背包系统（多个物品槽）
- ✅ 商店系统（多个商品卡片）
- ✅ 技能栏（多个技能按钮）
- ✅ 聊天系统（多条聊天气泡）
- ✅ 排行榜（多个排名条目）

### 注意事项

1. **始终调用base** - 在重写生命周期方法时必须调用base方法
2. **避免循环引用** - 子组件不应该引用其他子组件
3. **合理使用事件** - 频繁触发的事件注意性能开销
4. **及时取消订阅** - 避免内存泄漏

---

## 示例代码

完整示例请查看：
- `ItemSlotComponent.cs` - 物品槽组件
- `InventoryPanel.cs` - 背包面板（使用组件）
- `UIComponentUsageExample.cs` - 完整使用示例

---

## 版本历史

- **v1.0.0** - 初始设计
  - UIComponent基类
  - UIComponentManager
  - 事件通讯系统
  - 生命周期传递
