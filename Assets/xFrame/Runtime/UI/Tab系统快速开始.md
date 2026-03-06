# Tab系统 - 快速开始

## 5分钟上手指南

### 第一步：创建页面类

```csharp
using xFrame.Runtime.UI;

// 创建你的页面类，继承UITabPage
public class HomePage : UITabPage
{
    public override string PageName => "首页";
    
    protected override void OnPageEnter()
    {
        base.OnPageEnter();
        // 页面激活时执行的逻辑
        Debug.Log("进入首页");
    }
}

public class InventoryPage : UITabPage
{
    public override string PageName => "背包";
    
    protected override void OnPageEnter()
    {
        base.OnPageEnter();
        Debug.Log("进入背包");
    }
}
```

### 第二步：创建UI预制体

在Unity编辑器中：

1. 创建一个Canvas GameObject，命名为"MainTabContainer"
2. 添加`UITabContainer`组件
3. 创建子对象：
   - `PageContainer` - 用于放置页面
   - `ButtonContainer` - 用于放置Tab按钮
4. 将MainTabContainer保存为预制体

### 第三步：创建页面预制体

1. 在PageContainer下创建页面GameObject，添加`HomePage`组件
2. 设计页面UI
3. 保存为预制体
4. 重复以上步骤创建其他页面预制体

### 第四步：代码中使用

```csharp
using UnityEngine;
using xFrame.Runtime.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] private UITabContainer containerPrefab;
    [SerializeField] private HomePage homePagePrefab;
    [SerializeField] private InventoryPage inventoryPagePrefab;
    
    private UITabContainer _container;
    
    void Start()
    {
        // 1. 实例化容器
        _container = Instantiate(containerPrefab);
        
        // 2. 添加页面
        _container.AddPage(Instantiate(homePagePrefab));
        _container.AddPage(Instantiate(inventoryPagePrefab));
        
        // 3. 打开容器
        _container.InternalOnCreate();
        _container.InternalOnOpen(null);
        
        // 4. 切换页面
        _container.SwitchPage(0); // 切换到首页
    }
}
```

---

## 常用操作

### 切换页面的三种方式

```csharp
// 方式1：通过索引切换
_container.SwitchPage(0);

// 方式2：通过类型切换
_container.SwitchPage<InventoryPage>();

// 方式3：通过名称切换
_container.SwitchPage("背包");
```

### 获取页面

```csharp
// 获取当前页面
var currentPage = _container.CurrentPage;

// 通过索引获取
var page1 = _container.GetPage(0);

// 通过类型获取
var inventoryPage = _container.GetPage<InventoryPage>();

// 通过名称获取
var homePage = _container.GetPage("首页");
```

### 监听页面切换

```csharp
// 订阅页面切换事件
_container.OnPageChanged += (oldIndex, newIndex) =>
{
    Debug.Log($"从页面{oldIndex}切换到页面{newIndex}");
};
```

### 上一页/下一页

```csharp
// 切换到下一页
_container.NextPage();

// 切换到上一页
_container.PreviousPage();
```

---

## 进阶用法

### 使用Builder模式

```csharp
using xFrame.Runtime.UI;

void BuildContainer()
{
    var container = Instantiate(containerPrefab);
    
    // 使用Builder链式调用
    container.CreateBuilder()
        .WithButtonContainer(buttonContainer)
        .WithButtonPrefab(tabButtonPrefab)
        .AddPage(Instantiate(homePagePrefab), "首页", homeIcon)
        .AddPage(Instantiate(inventoryPagePrefab), "背包", inventoryIcon)
        .AddPage(Instantiate(shopPagePrefab), "商店", shopIcon)
        .Build();
    
    container.InternalOnCreate();
    container.InternalOnOpen(null);
}
```

### 页面生命周期回调

```csharp
public class MyPage : UITabPage
{
    // 1. 页面创建（只调用一次）
    protected override void OnCreate()
    {
        base.OnCreate();
        Debug.Log("页面创建");
        // 初始化UI组件
    }
    
    // 2. 页面打开
    protected override void OnOpen(object data)
    {
        base.OnOpen(data);
        Debug.Log("页面打开");
        // 接收数据
    }
    
    // 3. 切换为活动页面
    protected override void OnPageEnter()
    {
        base.OnPageEnter();
        Debug.Log("进入页面");
        // 刷新数据、播放动画
    }
    
    // 4. 页面显示
    protected override void OnShow()
    {
        base.OnShow();
        Debug.Log("页面显示");
        // 开始更新逻辑
    }
    
    // 5. 页面隐藏
    protected override void OnHide()
    {
        base.OnHide();
        Debug.Log("页面隐藏");
        // 停止更新逻辑
    }
    
    // 6. 从活动页面切出
    protected override void OnPageExit()
    {
        base.OnPageExit();
        Debug.Log("退出页面");
        // 保存状态、停止动画
    }
    
    // 7. 页面关闭
    protected override void OnClose()
    {
        base.OnClose();
        Debug.Log("页面关闭");
        // 清理临时数据
    }
    
    // 8. 页面销毁（只调用一次）
    protected override void OnDestroy()
    {
        base.OnDestroy();
        Debug.Log("页面销毁");
        // 释放资源、取消订阅
    }
}
```

### 传递数据给页面

```csharp
// 切换页面时传递数据
var itemData = new ItemData { Id = 123, Count = 5 };
_container.SwitchPage<InventoryPage>(itemData);

// 在页面中接收数据
public class InventoryPage : UITabPage
{
    protected override void OnOpen(object data)
    {
        base.OnOpen(data);
        
        if (data is ItemData itemData)
        {
            Debug.Log($"收到物品数据: ID={itemData.Id}");
            // 使用数据更新UI
        }
    }
}
```

---

## 常见问题

### Q1: 页面可以独立使用吗？

**答：** 可以！UITabPage继承自UIPanel，既可以在容器中使用，也可以独立使用。

```csharp
// 方式1：在容器中使用
var container = Instantiate(containerPrefab);
var page = Instantiate(pagePrefab);
container.AddPage(page);

// 方式2：独立使用（作为普通UIPanel）
var standalonePage = Instantiate(pagePrefab);
standalonePage.InternalOnCreate();
standalonePage.InternalOnOpen(null);
```

### Q2: OnPageEnter和OnShow有什么区别？

**答：**
- `OnPageEnter`: 切换为活动页面时调用（只在Tab容器中有意义）
- `OnShow`: 页面从隐藏变为显示时调用（独立和容器中都会调用）

```csharp
// 使用建议：
OnPageEnter() // 适合：刷新数据、播放进入动画
OnShow()      // 适合：开始更新逻辑、播放背景音乐
OnPageExit()  // 适合：保存状态、停止动画
OnHide()      // 适合：停止更新逻辑、降低音量
```

### Q3: 如何动态添加/移除页面？

**答：** 使用`AddPage`和`RemovePage`方法。

```csharp
// 添加页面
int index = _container.AddPage(Instantiate(newPagePrefab));

// 移除页面
_container.RemovePage(index);
```

### Q4: 如何自定义Tab按钮？

**答：** 继承`UITabButton`并重写相关方法。

```csharp
public class MyTabButton : UITabButton
{
    protected override void OnSetData(object data)
    {
        base.OnSetData(data);
        // 自定义按钮外观
    }
    
    public override void SetSelected(bool selected)
    {
        base.SetSelected(selected);
        // 自定义选中状态
    }
}
```

---

## 完整示例

查看以下文件获取完整示例：
- `TabSystemExample.cs` - 完整使用示例
- `多界面切换系统文档.md` - 详细设计文档

---

## 下一步

- 了解[生命周期管理](多界面切换系统文档.md#生命周期管理)
- 查看[最佳实践](多界面切换系统文档.md#最佳实践)
- 学习[完整示例](多界面切换系统文档.md#完整示例)

祝你使用愉快！
