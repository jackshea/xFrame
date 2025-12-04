# Tab系统架构设计

## 系统架构图

```
┌─────────────────────────────────────────────────────────────┐
│                    UITabContainer（容器）                     │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  属性:                                                  │  │
│  │  - List<UITabPage> _pages                             │  │
│  │  - int CurrentPageIndex                               │  │
│  │  - Dictionary<Type, UITabPage> _pagesByType          │  │
│  │  - Dictionary<string, UITabPage> _pagesByName        │  │
│  │                                                         │  │
│  │  方法:                                                  │  │
│  │  + AddPage<T>(page)                                   │  │
│  │  + SwitchPage(index/type/name)                        │  │
│  │  + GetPage(index/type/name)                           │  │
│  │  + NextPage() / PreviousPage()                        │  │
│  └───────────────────────────────────────────────────────┘  │
│                            │                                 │
│           ┌────────────────┼────────────────┐               │
│           ▼                ▼                ▼               │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ UITabPage #1 │  │ UITabPage #2 │  │ UITabPage #3 │      │
│  │   (首页)     │  │   (背包)     │  │   (商店)     │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
│                                                              │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  UIComponentManager（组件管理器）                        │  │
│  │  - 管理TabButton组件                                    │  │
│  └───────────────────────────────────────────────────────┘  │
│                            │                                 │
│           ┌────────────────┼────────────────┐               │
│           ▼                ▼                ▼               │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ UITabButton  │  │ UITabButton  │  │ UITabButton  │      │
│  │    #1        │  │    #2        │  │    #3        │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└─────────────────────────────────────────────────────────────┘
```

---

## 类关系图

```
UIPanel (基类)
    │
    ├─→ UITabPage (标签页基类)
    │       ├─ HomePage
    │       ├─ InventoryPage
    │       └─ ShopPage
    │
    └─→ UITabContainer (容器)
            └─ 持有 → List<UITabPage>

UIComponent (组件基类)
    │
    └─→ UITabButton (标签按钮)
```

---

## 生命周期流程图

### 容器初始化流程

```
1. 创建UITabContainer实例
   ↓
2. Awake()
   ↓
3. OnCreate()
   ├─ 创建所有UITabPage实例
   ├─ AddPage() 添加页面到容器
   │  ├─ 设置 page.IsInContainer = true
   │  ├─ 设置 page.ParentContainer = this
   │  ├─ 调用 page.InternalOnCreate()
   │  └─ 默认隐藏页面
   └─ 创建TabButton（可选）
   ↓
4. OnOpen()
   └─ SwitchPage(defaultPageIndex)
      ├─ Page.InternalOnOpen()
      ├─ Page.InternalOnShow()
      └─ Page.InternalPageEnter()
```

### 页面切换流程

```
SwitchPage(newIndex)
   │
   ├─ 旧页面 (if exists)
   │  ├─ OnPageExit()     ← 退出回调
   │  └─ InternalOnHide()  ← 隐藏
   │
   └─ 新页面
      ├─ InternalOnCreate() ← 如果未创建
      ├─ InternalOnOpen()   ← 如果未打开
      ├─ InternalOnShow()   ← 显示
      └─ InternalPageEnter() ← 进入回调
```

### 容器关闭流程

```
Container.Close()
   │
   ├─ OnHide()
   │  └─ CurrentPage.InternalOnHide()
   │
   ├─ OnClose()
   │  └─ 所有Pages.InternalOnClose()
   │
   └─ OnDestroy()
      └─ 所有Pages.InternalOnDestroy()
```

---

## 数据流向图

### 容器 → 页面（控制）

```
UITabContainer
   │
   ├─ AddPage()          → UITabPage.IsInContainer = true
   ├─ SwitchPage()       → UITabPage.InternalOnShow()
   ├─ OnShow()           → CurrentPage.InternalOnShow()
   ├─ OnHide()           → CurrentPage.InternalOnHide()
   └─ OnClose()          → AllPages.InternalOnClose()
```

### 页面 → 容器（事件）

```
UITabPage
   │
   └─ 不知道容器存在
      （页面可以独立使用）
```

### 按钮 → 容器（切换）

```
UITabButton
   │
   ├─ OnClick()
   │  └─ SendEvent(TabButtonClickedEvent)
   │
   └─ Container监听事件
      └─ SwitchPage(index)
```

---

## 组合模式体现

### 组合结构

```
Component (组件)
   │
   ├─ Leaf (叶节点)
   │  └─ UITabPage（既可独立使用，也可作为容器子节点）
   │
   └─ Composite (复合节点)
      └─ UITabContainer（容器，管理多个UITabPage）
```

### 关键特性

1. **统一接口**
   - UITabPage 和 UITabContainer 都继承自 UIPanel
   - 都有相同的生命周期方法（OnCreate, OnOpen, OnShow等）

2. **递归组合**
   - UITabContainer 可以包含多个 UITabPage
   - UITabPage 也可以独立使用（作为普通UIPanel）

3. **透明性**
   - 对于UITabPage来说，不知道自己是否在容器中
   - IsInContainer 只是内部标记，不影响页面逻辑

---

## 依赖关系图

```
┌─────────────────────────┐
│   UITabContainer        │
│   (容器持有子页面)        │
└───────┬─────────────────┘
        │ 1    持有    *
        ▼
┌─────────────────────────┐
│   UITabPage             │
│   (页面不知道容器)        │
└─────────────────────────┘
        │
        │ 继承
        ▼
┌─────────────────────────┐
│   UIPanel               │
└─────────────────────────┘
        │
        │ 继承
        ▼
┌─────────────────────────┐
│   UIView                │
└─────────────────────────┘
```

**依赖方向：**
- 容器 → 页面：单向依赖
- 页面 ↛ 容器：不依赖

---

## 事件流程图

### 页面切换事件

```
用户点击TabButton
   ↓
Button.OnClick()
   ↓
SendEvent(TabButtonClickedEvent)
   ↓
xFrameEventBus.Raise()
   ↓
Container订阅事件
   ↓
OnTabButtonClicked(evt)
   ↓
Container.SwitchPage(evt.PageIndex)
   ↓
触发 OnPageChanged 事件
   ↓
更新所有Button的选中状态
```

---

## 内存管理图

### 对象持有关系

```
UITabContainer
   │
   ├─ _pages: List<UITabPage>
   │  ├─ UITabPage #1 ────→ GameObject #1
   │  ├─ UITabPage #2 ────→ GameObject #2
   │  └─ UITabPage #3 ────→ GameObject #3
   │
   ├─ _pagesByType: Dictionary<Type, UITabPage>
   │  ├─ typeof(HomePage) → UITabPage #1
   │  └─ ...
   │
   └─ _pagesByName: Dictionary<string, UITabPage>
      ├─ "首页" → UITabPage #1
      └─ ...
```

### 生命周期管理

```
Container创建
   ↓
Pages创建（但隐藏）
   ↓
切换到Page1
   ├─ Page1显示
   └─ Page2,3保持隐藏
   ↓
切换到Page2
   ├─ Page1隐藏（但不销毁）
   └─ Page2显示
   ↓
Container关闭
   ↓
所有Pages关闭
   ↓
Container销毁
   ↓
所有Pages销毁
```

---

## Builder模式流程

```
创建Container
   ↓
container.CreateBuilder()
   ↓
builder.WithButtonContainer(transform)
   ↓
builder.WithButtonPrefab(prefab)
   ↓
builder.AddPage(page1, "首页", icon1)
   ↓
builder.AddPage(page2, "背包", icon2)
   ↓
builder.AddPage(page3, "商店", icon3)
   ↓
builder.Build()
   ├─ 添加所有页面到容器
   ├─ 创建所有TabButton
   ├─ 绑定按钮事件
   └─ 返回配置好的容器
```

---

## 适用场景对比

| 场景 | UIPanel | UITabPage | UITabContainer |
|-----|---------|-----------|----------------|
| 独立使用的单页面 | ✅ | ✅ | ❌ |
| 多个平行页面 | ✅ | ✅ | ❌ |
| 页签切换界面 | ❌ | ❌ | ✅ |
| 需要管理子页面生命周期 | ❌ | ❌ | ✅ |
| 页面可以独立也可以作为一部分 | ❌ | ✅ | ❌ |

---

## 设计模式总结

### 1. 组合模式 (Composite Pattern)
- 容器和页面都继承自相同基类
- 容器可以包含多个页面
- 页面可以独立使用

### 2. 构建器模式 (Builder Pattern)
- UITabContainerBuilder提供流式API
- 简化复杂对象的构建过程

### 3. 观察者模式 (Observer Pattern)
- 页面切换事件通知
- 按钮监听容器事件

### 4. 策略模式 (Strategy Pattern)
- 不同的页面有不同的业务逻辑
- 通过多态实现不同行为

---

## 与其他系统集成

```
UITabContainer
   ├─ 使用 UIComponentManager
   │  └─ 管理 UITabButton 组件
   │
   ├─ 继承 UIPanel
   │  └─ 使用 UIView 的生命周期
   │
   └─ 集成 xFrameEventBus
      └─ 页面切换事件通知
```

---

## 扩展性设计

### 可扩展点

1. **自定义页面类型**
   ```csharp
   public class MyCustomPage : UITabPage
   {
       // 自定义页面逻辑
   }
   ```

2. **自定义按钮样式**
   ```csharp
   public class MyCustomButton : UITabButton
   {
       // 自定义按钮外观
   }
   ```

3. **自定义切换动画**
   ```csharp
   public class AnimatedTabContainer : UITabContainer
   {
       protected override void SwitchPage(int index)
       {
           // 添加切换动画
           PlayTransitionAnimation();
           base.SwitchPage(index);
       }
   }
   ```

4. **自定义Builder**
   ```csharp
   public class MyTabContainerBuilder : UITabContainerBuilder
   {
       // 添加更多配置选项
   }
   ```

---

这个架构设计清晰、灵活、可扩展，符合SOLID原则和常见设计模式。
