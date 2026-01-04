# xFrame MVP框架使用指南

## 概述

xFrame MVP框架是一个基于Model-View-Presenter模式的架构实现，充分集成了xFrame框架的核心功能，包括VContainer依赖注入、EventBus事件总线、资源管理器、对象池等。

## 核心概念

### MVP模式

- **Model** - 负责数据管理和业务逻辑
- **View** - 负责UI显示和用户交互
- **Presenter** - 负责协调View和Model的交互

### 核心组件

1. **IModel** - Model接口
2. **IView** - View接口
3. **IPresenter** - Presenter接口
4. **IMVPTriple** - MVP三元组接口
5. **IMVPManager** - MVP管理器接口

## 快速开始

### 1. 注册MVP模块

在VContainer的LifetimeScope中注册MVP模块：

```csharp
using VContainer;
using VContainer.Unity;
using xFrame.MVP;

public class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // 注册MVP模块
        builder.RegisterMVPModule();
        
        // 注册具体的MVP组件
        builder.RegisterMVP<UserModel, UserView, UserPresenter, UserMVPTriple>();
    }
}
```

### 2. 定义Model

```csharp
using Cysharp.Threading.Tasks;
using xFrame.MVP;

public interface IUserModel : IModel
{
    string UserName { get; set; }
    int Level { get; set; }
}

public class UserModel : BaseModel, IUserModel
{
    public string UserName { get; set; }
    public int Level { get; set; }
    
    public override async UniTask InitializeAsync()
    {
        await base.InitializeAsync();
        // 初始化数据
        UserName = "Player";
        Level = 1;
        NotifyDataChanged();
    }
}
```

### 3. 定义View

```csharp
using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using xFrame.MVP;

public interface IUserView : IView
{
    void UpdateUserInfo(string userName, int level);
    event Action OnLevelUpButtonClicked;
}

public class UserView : BaseView, IUserView
{
    [SerializeField] private Text userNameText;
    [SerializeField] private Text levelText;
    [SerializeField] private Button levelUpButton;
    
    public event Action OnLevelUpButtonClicked;
    
    protected override void OnPresenterBound()
    {
        levelUpButton.onClick.AddListener(() => OnLevelUpButtonClicked?.Invoke());
    }
    
    protected override void OnPresenterUnbound()
    {
        levelUpButton.onClick.RemoveAllListeners();
        OnLevelUpButtonClicked = null;
    }
    
    public void UpdateUserInfo(string userName, int level)
    {
        userNameText.text = userName;
        levelText.text = $"Level: {level}";
    }
    
    protected override async UniTask OnShowAsync()
    {
        // 显示动画
        await UniTask.CompletedTask;
    }
    
    protected override async UniTask OnHideAsync()
    {
        // 隐藏动画
        await UniTask.CompletedTask;
    }
}
```

### 4. 定义Presenter

```csharp
using Cysharp.Threading.Tasks;
using xFrame.MVP;

public class UserPresenter : BasePresenter
{
    private IUserModel userModel;
    private IUserView userView;
    
    protected override async UniTask OnBindAsync()
    {
        userModel = Model as IUserModel;
        userView = View as IUserView;
        
        userView.OnLevelUpButtonClicked += OnLevelUpButtonClicked;
        
        UpdateView();
        await UniTask.CompletedTask;
    }
    
    protected override async UniTask OnUnbindAsync()
    {
        if (userView != null)
        {
            userView.OnLevelUpButtonClicked -= OnLevelUpButtonClicked;
        }
        await UniTask.CompletedTask;
    }
    
    protected override void OnModelDataChanged(IModel model)
    {
        UpdateView();
    }
    
    private void UpdateView()
    {
        if (userModel != null && userView != null)
        {
            userView.UpdateUserInfo(userModel.UserName, userModel.Level);
        }
    }
    
    private void OnLevelUpButtonClicked()
    {
        if (userModel != null)
        {
            userModel.Level++;
            Logger.Info($"User leveled up to {userModel.Level}");
        }
    }
    
    protected override async UniTask OnShowAsync()
    {
        Logger.Info("User panel shown");
        await UniTask.CompletedTask;
    }
    
    protected override async UniTask OnHideAsync()
    {
        Logger.Info("User panel hidden");
        await UniTask.CompletedTask;
    }
}
```

### 5. 定义MVP三元组

```csharp
using xFrame.MVP;

public class UserMVPTriple : MVPTriple<UserModel, UserView, UserPresenter>
{
    public UserMVPTriple(UserModel model, UserView view, UserPresenter presenter) 
        : base(model, view, presenter)
    {
    }
}
```

### 6. 使用MVP

```csharp
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using xFrame.MVP;

public class GameManager : MonoBehaviour
{
    [Inject] private IMVPManager mvpManager;
    
    private UserMVPTriple userMVP;
    
    private async void Start()
    {
        // 创建MVP
        userMVP = await mvpManager.CreateMVPAsync<UserMVPTriple, UserModel, UserView, UserPresenter>();
        
        // 显示MVP
        await userMVP.ShowAsync();
    }
    
    private async void OnDestroy()
    {
        // 销毁MVP
        if (userMVP != null)
        {
            await mvpManager.DestroyMVPAsync(userMVP);
        }
    }
}
```

## 高级功能

### 使用EventBus进行MVP间通信

```csharp
using xFrame.EventBus;
using xFrame.MVP.Events;

// 发送MVP事件
xFrameEventBus.Raise(new MVPShowEvent("UserPanel"));

// 监听MVP事件
xFrameEventBus.Subscribe<MVPShowEvent>(OnMVPShow);

private void OnMVPShow(MVPShowEvent evt)
{
    Logger.Info($"MVP shown: {evt.MVPId}");
}
```

### 使用View工厂加载View

```csharp
using xFrame.MVP;

public class CustomMVPManager
{
    [Inject] private IMVPViewFactory viewFactory;
    
    public async UniTask<IUserView> CreateViewAsync()
    {
        return await viewFactory.CreateViewAsync<UserView>("Prefabs/UserView");
    }
}
```

### 使用对象池复用MVP

```csharp
using xFrame.MVP;

public class PooledMVPManager
{
    [Inject] private IMVPTriplePool mvpPool;
    
    public UserMVPTriple GetMVP()
    {
        return mvpPool.Get<UserMVPTriple>();
    }
    
    public void ReturnMVP(UserMVPTriple mvp)
    {
        mvpPool.Return(mvp);
    }
}
```

## 最佳实践

### 1. 职责分离

- Model只处理数据和业务逻辑，不依赖Unity
- View只处理UI显示，不包含业务逻辑
- Presenter协调Model和View，可独立测试

### 2. 异步优先

- 所有初始化、显示、隐藏操作都使用UniTask
- 避免阻塞主线程
- 合理使用async/await

### 3. 事件驱动

- 使用EventBus进行MVP间通信
- Model数据变更通过事件通知
- View用户交互通过事件传递

### 4. 资源管理

- View预制体通过资源管理器加载
- 使用对象池复用MVP实例
- 及时释放不需要的资源

### 5. 错误处理

- 在关键操作中添加try-catch
- 使用日志系统记录错误信息
- 提供优雅的降级方案

## 常见问题

### Q: 如何在Model中访问其他服务？

A: 通过VContainer的依赖注入：

```csharp
public class UserModel : BaseModel, IUserModel
{
    [Inject] private IPersistenceManager persistenceManager;
    
    public override async UniTask InitializeAsync()
    {
        await base.InitializeAsync();
        // 使用注入的服务
        var data = await persistenceManager.LoadAsync<UserData>("user");
    }
}
```

### Q: 如何在Presenter中发送事件？

A: 注入EventBus并使用：

```csharp
public class UserPresenter : BasePresenter
{
    [Inject] private IEventBus eventBus;
    
    private void OnLevelUpButtonClicked()
    {
        // 发送事件
        eventBus.Raise(new UserLevelUpEvent(userModel.Level));
    }
}
```

### Q: 如何测试Presenter？

A: 使用Mock对象模拟View和Model：

```csharp
[Test]
public async void TestPresenter()
{
    var mockModel = new MockUserModel();
    var mockView = new MockUserView();
    var presenter = new UserPresenter();
    
    await presenter.BindAsync(mockView, mockModel);
    
    // 测试业务逻辑
    Assert.IsTrue(presenter.IsActive);
}
```

## 总结

xFrame MVP框架提供了一个完整的Model-View-Presenter架构实现，通过清晰的职责分离、强大的依赖注入、灵活的事件通信和高效的资源管理，为Unity项目提供了一个可维护、可测试、可扩展的架构基础。
