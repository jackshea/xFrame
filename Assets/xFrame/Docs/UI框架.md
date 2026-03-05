# xFrame UI框架设计文档

基于xFrame框架核心功能实现的现代化Unity UI管理系统。

## 目录
- [概述](#概述)
- [核心特性](#核心特性)
- [架构设计](#架构设计)
- [核心组件](#核心组件)
- [使用指南](#使用指南)
- [组件复用系统](#组件复用系统)
- [最佳实践](#最佳实践)
- [API参考](#api参考)

---

## 概述

xFrame UI框架是一个轻量级、模块化的Unity UI管理系统，充分集成了xFrame框架的核心功能：
- **VContainer依赖注入** - UI组件的依赖管理
- **资源管理器** - UI预制体的加载与释放
- **对象池** - UI实例的复用
- **事件总线** - UI事件的解耦通信
- **状态机** - UI流程的状态管理

---

## 核心特性

### ✅ 已实现功能
- **分层管理** - 支持多层UI（Background、Normal、Popup、System、Top）
- **栈式管理** - UI面板的堆栈式打开/关闭
- **生命周期** - 完整的UI生命周期回调（OnCreate、OnOpen、OnShow、OnHide、OnClose、OnDestroy）
- **异步加载** - 基于Addressable的异步UI加载
- **对象池复用** - 自动管理UI实例的复用
- **事件通信** - 通过事件总线实现UI间解耦通信
- **依赖注入** - VContainer自动注入UI所需的服务
- **MVVM支持** - 支持Model-View-ViewModel模式
- **组件复用** - 可复用的UI子组件系统，支持父子通讯和生命周期传递

### 🎯 设计目标
1. **易用性** - 简单直观的API，降低学习成本
2. **性能优化** - 对象池复用、异步加载、延迟释放
3. **可扩展** - 易于扩展自定义UI类型
4. **解耦** - UI逻辑与业务逻辑分离

---

## 架构设计

### 系统架构图

```
┌─────────────────────────────────────────────────────────┐
│                    UI Manager Module                     │
│  ┌────────────┐  ┌──────────────┐  ┌─────────────────┐ │
│  │ UI Manager │  │ Layer Manager│  │ Navigation Stack│ │
│  └────────────┘  └──────────────┘  └─────────────────┘ │
└─────────────────────────────────────────────────────────┘
                            │
        ┌───────────────────┼───────────────────┐
        ▼                   ▼                   ▼
┌───────────────┐  ┌────────────────┐  ┌──────────────┐
│ Asset Manager │  │  Object Pool   │  │  Event Bus   │
│   (加载UI)    │  │   (复用UI)     │  │  (UI事件)    │
└───────────────┘  └────────────────┘  └──────────────┘
        │                   │                   │
        └───────────────────┼───────────────────┘
                            ▼
                    ┌───────────────┐
                    │  UI View Base │
                    └───────────────┘
                            │
        ┌───────────────────┼───────────────────┐
        ▼                   ▼                   ▼
┌───────────┐      ┌────────────┐      ┌──────────────┐
│ UI Panel  │      │ UI Window  │      │  UI Widget   │
└───────────┘      └────────────┘      └──────────────┘
```

### 分层结构

UI框架采用分层管理，每层有独立的Canvas和SortOrder：

| 层级 | SortOrder | 说明 | 示例 |
|------|-----------|------|------|
| **Background** | 0-999 | 背景层，永久显示 | 主界面背景 |
| **Normal** | 1000-1999 | 普通UI层 | 主菜单、背包 |
| **Popup** | 2000-2999 | 弹窗层 | 对话框、提示框 |
| **System** | 3000-3999 | 系统层 | 加载界面、网络错误 |
| **Top** | 4000-4999 | 顶层 | 调试工具、GM面板 |

---

## 核心组件

### 1. UIView (UI视图基类)

所有UI组件的基类，提供统一的生命周期管理。

```csharp
/// <summary>
/// UI视图基类
/// </summary>
public abstract class UIView : MonoBehaviour, IPoolable
{
    // UI层级
    public UILayer Layer { get; protected set; } = UILayer.Normal;
    
    // UI是否已打开
    public bool IsOpen { get; private set; }
    
    // 生命周期回调
    protected virtual void OnCreate() { }      // 创建时调用（仅一次）
    protected virtual void OnOpen(object data) { }    // 打开时调用
    protected virtual void OnShow() { }        // 显示时调用（打开时、从栈恢复时）
    protected virtual void OnHide() { }        // 隐藏时调用（被遮挡时、关闭前）
    protected virtual void OnClose() { }       // 关闭时调用
    protected virtual void OnDestroy() { }     // 销毁时调用（仅一次）
    
    // 对象池接口
    public virtual void OnSpawn() { }
    public virtual void OnRecycle() { }
}
```

### 2. UIPanel (UI面板)

继承自UIView，代表一个完整的UI面板。

```csharp
/// <summary>
/// UI面板基类 - 代表一个完整的UI界面
/// </summary>
public abstract class UIPanel : UIView
{
    // 是否支持栈式管理（Back键返回）
    public virtual bool UseStack => true;
    
    // 打开时是否关闭其他同层UI
    public virtual bool CloseOthers => false;
    
    // 是否允许缓存到对象池
    public virtual bool Cacheable => true;
}
```

### 3. UIWindow (UI窗口)

继承自UIView，代表弹出式窗口（模态/非模态）。

```csharp
/// <summary>
/// UI窗口基类 - 代表弹出式对话框
/// </summary>
public abstract class UIWindow : UIView
{
    // 是否模态（阻挡下层UI交互）
    public virtual bool IsModal => true;
    
    // 点击遮罩是否关闭
    public virtual bool CloseOnMaskClick => true;
    
    // 遮罩透明度
    public virtual float MaskAlpha => 0.7f;
}
```

### 4. UIManager (UI管理器)

负责所有UI的加载、显示、隐藏、销毁。

```csharp
/// <summary>
/// UI管理器 - 核心管理类
/// </summary>
public interface IUIManager
{
    // 打开UI
    Task<T> OpenAsync<T>(object data = null) where T : UIView;
    
    // 关闭UI
    void Close<T>() where T : UIView;
    void Close(UIView view);
    
    // 获取UI实例
    T Get<T>() where T : UIView;
    
    // 检查UI是否已打开
    bool IsOpen<T>() where T : UIView;
    
    // 关闭所有UI
    void CloseAll(UILayer? layer = null);
    
    // 返回上一个UI（栈管理）
    void Back();
}
```

### 5. UIManagerModule (UI模块)

集成到xFrame框架的UI模块，遵循模块生命周期。

```csharp
/// <summary>
/// UI管理模块 - 集成到xFrame框架
/// </summary>
public class UIManagerModule : IDisposable
{
    private readonly IUIManager _uiManager;
    private readonly IXLogger _logger;
    
    public UIManagerModule(IUIManager uiManager, IXLogManager logManager)
    {
        _uiManager = uiManager;
        _logger = logManager.GetLogger<UIManagerModule>();
    }
    
    // 模块初始化
    public void OnInit()
    {
        _logger.Info("UI管理模块初始化开始...");
        // 初始化UI层级、Canvas等
        _logger.Info("UI管理模块初始化完成");
    }
    
    // 模块启动
    public void OnStart()
    {
        _logger.Info("UI管理模块启动");
    }
    
    // 模块销毁
    public void OnDestroy()
    {
        _logger.Info("UI管理模块销毁开始...");
        _uiManager.CloseAll();
        _logger.Info("UI管理模块销毁完成");
    }
    
    public void Dispose()
    {
        OnDestroy();
    }
}
```

---

## 使用指南

### 1. 创建UI面板

```csharp
using xFrame.Runtime.UI;
using UnityEngine;

/// <summary>
/// 主菜单UI
/// </summary>
public class MainMenuPanel : UIPanel
{
    // 配置层级
    public override UILayer Layer => UILayer.Normal;
    
    // 打开时关闭其他同层UI
    public override bool CloseOthers => true;
    
    // UI按钮
    [SerializeField] private Button startButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;
    
    protected override void OnCreate()
    {
        // 绑定按钮事件
        startButton.onClick.AddListener(OnStartClicked);
        settingsButton.onClick.AddListener(OnSettingsClicked);
        exitButton.onClick.AddListener(OnExitClicked);
    }
    
    protected override void OnOpen(object data)
    {
        // 打开时的逻辑（只在首次打开时调用）
        Debug.Log("主菜单已打开");
    }
    
    protected override void OnShow()
    {
        // 显示时的逻辑（打开后、从栈恢复时调用）
        // 播放显示动画、恢复音乐、开始更新等
        Debug.Log("主菜单显示");
    }
    
    protected override void OnHide()
    {
        // 隐藏时的逻辑（被遮挡时、关闭前调用）
        // 暂停动画、降低音量、停止更新等
        Debug.Log("主菜单隐藏");
    }
    
    protected override void OnClose()
    {
        // 关闭时的逻辑
        Debug.Log("主菜单已关闭");
    }
    
    private void OnStartClicked()
    {
        // 发送事件
        xFrameEventBus.Raise(new GameStartEvent());
    }
    
    private void OnSettingsClicked()
    {
        // 打开设置窗口
        // UIManager会自动注入
    }
    
    private void OnExitClicked()
    {
        Application.Quit();
    }
}
```

### 2. 创建弹窗

```csharp
using xFrame.Runtime.UI;
using UnityEngine;
using TMPro;

/// <summary>
/// 确认对话框
/// </summary>
public class ConfirmDialog : UIWindow
{
    public override UILayer Layer => UILayer.Popup;
    public override bool IsModal => true;
    public override bool CloseOnMaskClick => false;
    
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    
    private System.Action _onConfirm;
    private System.Action _onCancel;
    
    protected override void OnCreate()
    {
        confirmButton.onClick.AddListener(OnConfirmClicked);
        cancelButton.onClick.AddListener(OnCancelClicked);
    }
    
    protected override void OnOpen(object data)
    {
        if (data is ConfirmDialogData dialogData)
        {
            titleText.text = dialogData.Title;
            messageText.text = dialogData.Message;
            _onConfirm = dialogData.OnConfirm;
            _onCancel = dialogData.OnCancel;
        }
    }
    
    protected override void OnClose()
    {
        _onConfirm = null;
        _onCancel = null;
    }
    
    private void OnConfirmClicked()
    {
        _onConfirm?.Invoke();
        // Close会自动调用
    }
    
    private void OnCancelClicked()
    {
        _onCancel?.Invoke();
        // Close会自动调用
    }
}

// 数据类
public class ConfirmDialogData
{
    public string Title { get; set; }
    public string Message { get; set; }
    public System.Action OnConfirm { get; set; }
    public System.Action OnCancel { get; set; }
}
```

### 3. 在代码中使用UI

```csharp
using VContainer;
using xFrame.Runtime.UI;

public class GameController : MonoBehaviour
{
    private IUIManager _uiManager;
    
    // VContainer自动注入
    [Inject]
    public void Construct(IUIManager uiManager)
    {
        _uiManager = uiManager;
    }
    
    async void Start()
    {
        // 打开主菜单
        await _uiManager.OpenAsync<MainMenuPanel>();
        
        // 打开确认对话框
        var dialogData = new ConfirmDialogData
        {
            Title = "确认",
            Message = "确定要退出游戏吗？",
            OnConfirm = () => Application.Quit(),
            OnCancel = () => Debug.Log("取消退出")
        };
        await _uiManager.OpenAsync<ConfirmDialog>(dialogData);
        
        // 关闭UI
        _uiManager.Close<MainMenuPanel>();
        
        // 返回上一个UI
        _uiManager.Back();
    }
}
```

### 4. 集成到xFrame框架

在`xFrameLifetimeScope`中注册UI模块：

```csharp
using xFrame.Runtime.UI;

public class xFrameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // ... 其他注册 ...
        
        RegisterUISystem(builder);
    }
    
    /// <summary>
    /// 注册UI系统到VContainer
    /// </summary>
    private void RegisterUISystem(IContainerBuilder builder)
    {
        // 注册UI管理器为单例
        builder.Register<IUIManager, UIManager>(Lifetime.Singleton);
        
        // 注册UI模块为单例
        builder.Register<UIManagerModule>(Lifetime.Singleton)
            .AsImplementedInterfaces()
            .AsSelf();
    }
}
```

### 5. 使用事件总线进行UI通信

```csharp
// 定义UI事件
public struct GameStartEvent : IEvent { }
public struct PlayerDeadEvent : IEvent 
{
    public int Score { get; set; }
}

// 在UI中订阅事件
public class GameOverPanel : UIPanel
{
    private IUIManager _uiManager;
    
    [Inject]
    public void Construct(IUIManager uiManager)
    {
        _uiManager = uiManager;
    }
    
    protected override void OnCreate()
    {
        // 订阅玩家死亡事件
        xFrameEventBus.SubscribeTo<PlayerDeadEvent>(OnPlayerDead);
    }
    
    private void OnPlayerDead(ref PlayerDeadEvent evt)
    {
        // 打开游戏结束面板
        _uiManager.OpenAsync<GameOverPanel>(evt.Score);
    }
    
    protected override void OnDestroy()
    {
        // 取消订阅
        xFrameEventBus.UnsubscribeFrom<PlayerDeadEvent>(OnPlayerDead);
    }
}
```

### 6. 结合对象池使用

```csharp
public class UIManager : IUIManager
{
    private readonly IObjectPoolManager _poolManager;
    private readonly IAssetManager _assetManager;
    
    public UIManager(IObjectPoolManager poolManager, IAssetManager assetManager)
    {
        _poolManager = poolManager;
        _assetManager = assetManager;
    }
    
    public async Task<T> OpenAsync<T>(object data = null) where T : UIView
    {
        var typeName = typeof(T).Name;
        
        // 从对象池获取或创建
        var pool = _poolManager.GetOrCreatePool<T>(
            typeName,
            createFunc: async () => 
            {
                var prefab = await _assetManager.LoadAssetAsync<GameObject>($"UI/{typeName}");
                var instance = GameObject.Instantiate(prefab);
                return instance.GetComponent<T>();
            },
            initialCapacity: 1
        );
        
        var view = await pool.GetAsync();
        
        // 调用生命周期
        if (!view.IsCreated)
        {
            view.OnCreate();
            view.IsCreated = true;
        }
        
        view.OnOpen(data);
        view.IsOpen = true;
        
        return view;
    }
    
    public void Close(UIView view)
    {
        if (!view.IsOpen) return;
        
        view.OnClose();
        view.IsOpen = false;
        
        // 返回对象池
        if (view.Cacheable)
        {
            var pool = _poolManager.GetPool(view.GetType().Name);
            pool?.Return(view);
        }
        else
        {
            GameObject.Destroy(view.gameObject);
        }
    }
}
```

---

## 组件复用系统

xFrame UI框架提供了强大的组件复用系统，允许你创建可复用的UI子组件。

### 核心概念

**单向依赖：** 父组件知道子组件，子组件不知道父组件  
**组件识别：** 每个组件实例都有唯一的ComponentId  
**事件通讯：** 子组件通过事件总线发送全局事件，父组件订阅接收  
**直接调用：** 父组件可以直接调用子组件的公开方法  
**生命周期传递：** 父组件生命周期自动影响子组件

### 创建可复用组件

```csharp
using xFrame.Runtime.UI;

/// <summary>
/// 物品槽组件 - 可在背包、仓库、商店等多处复用
/// </summary>
public class ItemSlotComponent : UIComponent
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Text countText;
    [SerializeField] private Button slotButton;
    
    private ItemData _itemData;
    
    // 初始化（只执行一次）
    protected override void OnInitialize()
    {
        base.OnInitialize();
        slotButton.onClick.AddListener(OnSlotClicked);
    }
    
    // 设置数据（可多次调用）
    protected override void OnSetData(object data)
    {
        base.OnSetData(data);
        
        if (data is ItemData item)
        {
            _itemData = item;
            iconImage.sprite = item.Icon;
            countText.text = item.Count.ToString();
        }
    }
    
    // 槽位点击 - 发送全局事件
    private void OnSlotClicked()
    {
        SendEvent(new ItemSlotClickedEvent
        {
            ItemId = _itemData.ItemId,
            SlotIndex = transform.GetSiblingIndex()
        });
    }
    
    // 清理
    protected override void OnDestroyComponent()
    {
        slotButton.onClick.RemoveListener(OnSlotClicked);
        base.OnDestroyComponent();
    }
}

// 定义组件事件
public struct ItemSlotClickedEvent : IUIComponentEvent
{
    public int ItemId { get; set; }
    public int SlotIndex { get; set; }
}
```

### 在父UI中使用组件

```csharp
public class InventoryPanel : UIPanel
{
    [SerializeField] private Transform slotContainer;
    [SerializeField] private ItemSlotComponent slotPrefab;
    
    private List<ItemSlotComponent> _slots = new List<ItemSlotComponent>();
    
    protected override void OnCreate()
    {
        base.OnCreate();
        
        // 1. 创建多个槽位组件并持有引用
        for (int i = 0; i < 20; i++)
        {
            var slot = Instantiate(slotPrefab, slotContainer);
            
            // 2. 注册到组件管理器
            ComponentManager.RegisterComponent(slot);
            
            // 3. 父组件持有子组件引用
            _slots.Add(slot);
            slot.Show();
        }
        
        // 4. 订阅全局事件
        xFrameEventBus.SubscribeTo<UIComponentEventWrapper<ItemSlotClickedEvent>>(
            OnItemSlotClicked);
    }
    
    // 5. 处理组件事件
    private void OnItemSlotClicked(
        ref UIComponentEventWrapper<ItemSlotClickedEvent> wrapper)
    {
        // 通过ComponentId识别是哪个槽位
        Debug.Log($"槽位 {wrapper.ComponentId} 被点击");
        
        // 方式1：通过ComponentId获取具体组件
        var slot = ComponentManager.GetComponent<ItemSlotComponent>(wrapper.ComponentId);
        
        // 方式2：直接使用SourceComponent
        var slot2 = wrapper.SourceComponent as ItemSlotComponent;
        
        // 父组件可以直接调用子组件方法
        slot.SetSelected(true);
        
        // 处理点击逻辑
        HandleSlotClick(slot, wrapper.Event);
    }
    
    // 6. 父组件可以直接操作所有子组件
    public void ClearAllSlots()
    {
        foreach (var slot in _slots)
        {
            slot.SetData(null);
            slot.Hide();
        }
    }
    
    protected override void OnDestroy()
    {
        // 7. 取消订阅
        xFrameEventBus.UnsubscribeFrom<UIComponentEventWrapper<ItemSlotClickedEvent>>(
            OnItemSlotClicked);
        base.OnDestroy();
    }
}
```

### 生命周期传递

父组件的生命周期会自动传递给子组件：

```csharp
父UI.OnShow()    → 所有可见子组件.Show()
父UI.OnHide()    → 所有可见子组件.Hide()
父UI.OnClose()   → 所有子组件.Reset()
父UI.OnDestroy() → 所有子组件.DestroyComponent()
```

**示例：**

```csharp
// 父UI显示时，所有子组件自动显示
protected override void OnShow()
{
    base.OnShow(); // 必须调用！
    // ComponentManager会自动调用所有子组件的Show()
}

// 父UI隐藏时，所有子组件自动隐藏
protected override void OnHide()
{
    base.OnHide(); // 必须调用！
    // ComponentManager会自动调用所有子组件的Hide()
}
```

### 组件管理API

```csharp
// 注册组件
ComponentManager.RegisterComponent(component);

// 自动查找并注册所有子组件
ComponentManager.AutoRegisterComponents();

// 通过ID获取组件
var comp = ComponentManager.GetComponent<ItemSlotComponent>(componentId);

// 获取指定类型的所有组件
var slots = ComponentManager.GetComponentsOfType<ItemSlotComponent>();

// 批量操作
ComponentManager.ShowAll();
ComponentManager.HideAll();
ComponentManager.RefreshAll();
```

### 完整示例

查看以下文件获取完整示例：
- `ItemSlotComponent.cs` - 物品槽组件实现
- `InventoryPanel.cs` - 背包面板（使用组件）
- `UI组件复用设计.md` - 完整设计文档
- `组件系统快速开始.md` - 快速入门指南

---

## 最佳实践

### 1. UI设计原则

- **单一职责** - 每个UI只负责一个功能模块
- **解耦通信** - 使用事件总线而不是直接引用
- **数据驱动** - 通过参数传递数据，而不是直接访问
- **生命周期清晰** - 在正确的生命周期回调中执行操作

### 2. 生命周期使用指南

#### OnCreate vs OnOpen
- **OnCreate**: 只执行一次的初始化（绑定事件、获取组件引用）
- **OnOpen**: 每次打开时执行（接收数据、重置状态）

#### OnShow vs OnHide
- **OnShow**: UI可见时执行（播放动画、恢复更新、刷新数据）
- **OnHide**: UI隐藏时执行（暂停动画、停止更新、保存状态）

**典型使用场景：**

```csharp
public class BattlePanel : UIPanel
{
    private bool _isUpdateActive;
    
    protected override void OnShow()
    {
        base.OnShow();
        
        // ✅ 开始战斗更新逻辑
        _isUpdateActive = true;
        
        // ✅ 播放BGM
        AudioManager.PlayBGM("BattleMusic");
        
        // ✅ 刷新显示（可能在被遮挡期间数据已更新）
        RefreshUI();
    }
    
    protected override void OnHide()
    {
        base.OnHide();
        
        // ✅ 停止战斗更新，节省性能
        _isUpdateActive = false;
        
        // ✅ 降低BGM音量
        AudioManager.SetVolume(0.3f);
    }
    
    private void Update()
    {
        if (!_isUpdateActive) return;
        
        // 战斗逻辑只在显示时更新
        UpdateBattle();
    }
}
```

### 3. 性能优化

```csharp
// ✅ 推荐：使用对象池
public override bool Cacheable => true;

// ✅ 推荐：异步加载
await _uiManager.OpenAsync<HeavyPanel>();

// ✅ 推荐：预加载常用UI
private async void PreloadCommonUI()
{
    await _assetManager.PreloadAssetAsync("UI/MainMenu");
    await _assetManager.PreloadAssetAsync("UI/Settings");
}

// ❌ 避免：频繁创建销毁
for (int i = 0; i < 100; i++)
{
    var item = Instantiate(itemPrefab);  // 应该使用对象池
}
```

### 4. 内存管理

```csharp
public class HeavyUIPanel : UIPanel
{
    // 大纹理资源
    private Texture2D _heavyTexture;
    
    protected override void OnOpen(object data)
    {
        // 加载资源
        _heavyTexture = await _assetManager.LoadAssetAsync<Texture2D>("UI/HeavyTexture");
    }
    
    protected override void OnClose()
    {
        // 及时释放资源
        if (_heavyTexture != null)
        {
            _assetManager.ReleaseAsset(_heavyTexture);
            _heavyTexture = null;
        }
    }
    
    // 不缓存，直接销毁
    public override bool Cacheable => false;
}
```

### 5. UI分层策略

```csharp
// 背景层 - 永久显示
public class BackgroundPanel : UIPanel
{
    public override UILayer Layer => UILayer.Background;
    public override bool Cacheable => true;
}

// 普通层 - 游戏主界面
public class MainGamePanel : UIPanel
{
    public override UILayer Layer => UILayer.Normal;
    public override bool CloseOthers => true;  // 打开时关闭其他
}

// 弹窗层 - 提示对话框
public class TipDialog : UIWindow
{
    public override UILayer Layer => UILayer.Popup;
    public override bool IsModal => false;  // 非模态，可点击下层
}

// 系统层 - 加载界面
public class LoadingPanel : UIPanel
{
    public override UILayer Layer => UILayer.System;
    public override bool IsModal => true;  // 阻挡所有交互
}
```

### 6. MVVM模式集成

```csharp
// ViewModel
public class PlayerInfoViewModel
{
    public string PlayerName { get; set; }
    public int Level { get; set; }
    public int Health { get; set; }
    public int MaxHealth { get; set; }
}

// View
public class PlayerInfoPanel : UIPanel
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Slider healthSlider;
    
    private PlayerInfoViewModel _viewModel;
    
    protected override void OnOpen(object data)
    {
        if (data is PlayerInfoViewModel viewModel)
        {
            _viewModel = viewModel;
            UpdateView();
        }
    }
    
    private void UpdateView()
    {
        nameText.text = _viewModel.PlayerName;
        levelText.text = $"Lv.{_viewModel.Level}";
        healthSlider.value = (float)_viewModel.Health / _viewModel.MaxHealth;
    }
}
```

### 7. UI动画

```csharp
public class AnimatedPanel : UIPanel
{
    [SerializeField] private CanvasGroup canvasGroup;
    
    protected override async void OnOpen(object data)
    {
        // 淡入动画
        canvasGroup.alpha = 0;
        await FadeIn();
    }
    
    protected override async void OnClose()
    {
        // 淡出动画
        await FadeOut();
        base.OnClose();
    }
    
    private async Task FadeIn()
    {
        float duration = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0, 1, elapsed / duration);
            await Task.Yield();
        }
        
        canvasGroup.alpha = 1;
    }
    
    private async Task FadeOut()
    {
        float duration = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1, 0, elapsed / duration);
            await Task.Yield();
        }
        
        canvasGroup.alpha = 0;
    }
}
```

---

## API参考

### IUIManager

| 方法 | 说明 | 返回值 |
|------|------|--------|
| `OpenAsync<T>(data)` | 异步打开UI | `Task<T>` |
| `Close<T>()` | 关闭指定类型的UI | `void` |
| `Close(view)` | 关闭指定实例的UI | `void` |
| `Get<T>()` | 获取已打开的UI实例 | `T` |
| `IsOpen<T>()` | 检查UI是否已打开 | `bool` |
| `CloseAll(layer)` | 关闭所有UI（可指定层级） | `void` |
| `Back()` | 返回上一个UI | `void` |

### UIView 生命周期

| 回调 | 触发时机 | 调用次数 |
|------|----------|----------|
| `OnCreate()` | UI实例首次创建时 | 1次 |
| `OnOpen(data)` | UI打开时 | 多次 |
| `OnShow()` | UI显示时（打开后、从栈恢复时） | 多次 |
| `OnHide()` | UI隐藏时（被遮挡时、关闭前） | 多次 |
| `OnClose()` | UI关闭时 | 多次 |
| `OnDestroy()` | UI实例销毁时 | 1次 |
| `OnSpawn()` | 从对象池取出时 | 多次 |
| `OnRecycle()` | 返回对象池时 | 多次 |

**生命周期调用顺序：**
1. **首次打开**: OnCreate → OnOpen → OnShow
2. **被其他UI遮挡**: OnHide
3. **从栈中恢复**: OnShow
4. **关闭**: OnHide → OnClose
5. **销毁**: OnDestroy

### UILayer 枚举

```csharp
public enum UILayer
{
    Background = 0,  // 背景层 (0-999)
    Normal = 1,      // 普通层 (1000-1999)
    Popup = 2,       // 弹窗层 (2000-2999)
    System = 3,      // 系统层 (3000-3999)
    Top = 4          // 顶层 (4000-4999)
}
```

### UI事件

```csharp
// UI打开事件
public struct UIOpenedEvent : IEvent
{
    public Type UIType { get; set; }
    public UIView View { get; set; }
}

// UI关闭事件
public struct UIClosedEvent : IEvent
{
    public Type UIType { get; set; }
}

// UI层级改变事件
public struct UILayerChangedEvent : IEvent
{
    public UILayer Layer { get; set; }
    public int ActiveCount { get; set; }
}
```

---

## 注意事项

1. **资源路径** - UI预制体需要配置为Addressable资源，路径格式为`UI/{UIName}`
2. **Canvas设置** - UI预制体应该使用独立的Canvas组件
3. **EventSystem** - 确保场景中有EventSystem组件
4. **依赖注入** - UI脚本必须在VContainer的生命周期范围内才能自动注入
5. **线程安全** - UI操作必须在主线程进行

---

## 示例场景结构

```
Scene
├── EventSystem
├── UIRoot
│   ├── Canvas_Background (SortOrder: 0)
│   ├── Canvas_Normal (SortOrder: 1000)
│   ├── Canvas_Popup (SortOrder: 2000)
│   ├── Canvas_System (SortOrder: 3000)
│   └── Canvas_Top (SortOrder: 4000)
└── xFrameLifetimeScope
```

---

## 扩展性

### 自定义UI类型

```csharp
/// <summary>
/// 自定义HUD类型
/// </summary>
public abstract class UIHud : UIView
{
    public override UILayer Layer => UILayer.Normal;
    public override bool Cacheable => true;
    
    // HUD特有的功能
    public virtual void UpdateHud(float deltaTime) { }
}
```

### 自定义UI加载器

```csharp
public interface IUILoader
{
    Task<GameObject> LoadAsync(string address);
    void Unload(GameObject go);
}

// 自定义加载器实现
public class CustomUILoader : IUILoader
{
    private readonly IAssetManager _assetManager;
    
    public CustomUILoader(IAssetManager assetManager)
    {
        _assetManager = assetManager;
    }
    
    public async Task<GameObject> LoadAsync(string address)
    {
        return await _assetManager.LoadAssetAsync<GameObject>($"CustomPath/{address}");
    }
    
    public void Unload(GameObject go)
    {
        _assetManager.ReleaseAsset(go);
    }
}
```

---

## 许可证

MIT License

---

## 版本历史

- **v1.0.0** - 初始设计文档
  - 核心UI管理功能
  - 分层管理
  - 对象池集成
  - 事件总线集成
  - VContainer依赖注入集成
