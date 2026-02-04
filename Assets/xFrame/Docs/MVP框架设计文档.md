# xFrame MVP框架设计文档

基于xFrame框架核心功能实现的Model-View-Presenter架构模式。

## 目录
- [概述](#概述)
- [设计原则](#设计原则)
- [架构设计](#架构设计)
- [核心组件](#核心组件)
- [集成方案](#集成方案)
- [使用指南](#使用指南)
- [最佳实践](#最佳实践)
- [API参考](#api参考)

---

## 概述

xFrame MVP框架是一个基于Model-View-Presenter模式的架构实现，充分利用xFrame框架的核心功能：
- **VContainer依赖注入** - MVP组件的依赖管理和生命周期控制
- **GenericEventBus事件总线** - MVP组件间的解耦通信
- **资源管理器** - View资源的加载与释放
- **对象池** - View实例的复用管理
- **日志系统** - 统一的日志记录
- **状态机** - Presenter的状态管理

### MVP模式优势
- **关注点分离** - Model处理数据，View处理显示，Presenter处理逻辑
- **可测试性** - Presenter可以独立于Unity进行单元测试
- **可维护性** - 清晰的职责划分，便于维护和扩展
- **可复用性** - Model和Presenter可以在不同View间复用

---

## 设计原则

### 1. 依赖倒置原则
- View和Model都依赖于抽象接口
- Presenter作为中介者，协调View和Model的交互
- 通过VContainer实现依赖注入

### 2. 单一职责原则
- **Model** - 仅负责数据管理和业务逻辑
- **View** - 仅负责UI显示和用户交互
- **Presenter** - 仅负责协调View和Model

### 3. 开闭原则
- 通过接口定义契约，便于扩展
- 支持多种View实现同一接口
- 支持Presenter的继承和扩展

### 4. 接口隔离原则
- 定义细粒度的接口，避免接口污染
- View接口按功能模块划分
- Model接口按数据类型划分

---

## 架构设计

### 整体架构图
```
┌─────────────────────────────────────────────────────────────┐
│                    xFrame MVP Framework                      │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐     │
│  │    View     │◄──►│  Presenter  │◄──►│    Model    │     │
│  │ (Interface) │    │   (Logic)   │    │   (Data)    │     │
│  └─────────────┘    └─────────────┘    └─────────────┘     │
│         │                   │                   │          │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐     │
│  │ VContainer  │    │ EventBus    │    │ Resource    │     │
│  │    (DI)     │    │ (Events)    │    │ Manager     │     │
│  └─────────────┘    └─────────────┘    └─────────────┘     │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐     │
│  │ ObjectPool  │    │   Logging   │    │ StateMachine│     │
│  │ (Pooling)   │    │  (Logging)  │    │  (States)   │     │
│  └─────────────┘    └─────────────┘    └─────────────┘     │
└─────────────────────────────────────────────────────────────┘
```

### 数据流向
```
User Input → View → Presenter → Model → Data Change Event → Presenter → View Update
```

### 生命周期管理
```
Create → Initialize → Bind → Show → Hide → Unbind → Dispose → Destroy
```

---

## 核心组件

### 1. MVP基础接口

#### IModel
```csharp
/// <summary>
/// MVP模式中的Model接口
/// 负责数据管理和业务逻辑
/// </summary>
public interface IModel : IDisposable
{
    /// <summary>
    /// 初始化Model
    /// </summary>
    UniTask InitializeAsync();
    
    /// <summary>
    /// 数据变更事件
    /// </summary>
    event System.Action<IModel> OnDataChanged;
}
```

#### IView
```csharp
/// <summary>
/// MVP模式中的View接口
/// 负责UI显示和用户交互
/// </summary>
public interface IView : IDisposable
{
    /// <summary>
    /// View是否已激活
    /// </summary>
    bool IsActive { get; }
    
    /// <summary>
    /// 显示View
    /// </summary>
    UniTask ShowAsync();
    
    /// <summary>
    /// 隐藏View
    /// </summary>
    UniTask HideAsync();
    
    /// <summary>
    /// 绑定Presenter
    /// </summary>
    void BindPresenter(IPresenter presenter);
    
    /// <summary>
    /// 解绑Presenter
    /// </summary>
    void UnbindPresenter();
}
```

#### IPresenter
```csharp
/// <summary>
/// MVP模式中的Presenter接口
/// 负责协调View和Model的交互
/// </summary>
public interface IPresenter : IDisposable
{
    /// <summary>
    /// Presenter是否已激活
    /// </summary>
    bool IsActive { get; }
    
    /// <summary>
    /// 初始化Presenter
    /// </summary>
    UniTask InitializeAsync();
    
    /// <summary>
    /// 绑定View和Model
    /// </summary>
    UniTask BindAsync(IView view, IModel model);
    
    /// <summary>
    /// 解绑View和Model
    /// </summary>
    UniTask UnbindAsync();
    
    /// <summary>
    /// 显示
    /// </summary>
    UniTask ShowAsync();
    
    /// <summary>
    /// 隐藏
    /// </summary>
    UniTask HideAsync();
}
```

### 2. MVP管理器

#### IMVPManager
```csharp
/// <summary>
/// MVP管理器接口
/// 负责MVP三元组的创建、管理和销毁
/// </summary>
public interface IMVPManager
{
    /// <summary>
    /// 创建MVP三元组
    /// </summary>
    UniTask<TMVPTriple> CreateMVPAsync<TMVPTriple, TModel, TView, TPresenter>()
        where TMVPTriple : class, IMVPTriple
        where TModel : class, IModel
        where TView : class, IView
        where TPresenter : class, IPresenter;
    
    /// <summary>
    /// 销毁MVP三元组
    /// </summary>
    UniTask DestroyMVPAsync<TMVPTriple>(TMVPTriple mvpTriple)
        where TMVPTriple : class, IMVPTriple;
    
    /// <summary>
    /// 获取活跃的MVP三元组
    /// </summary>
    TMVPTriple GetActiveMVP<TMVPTriple>()
        where TMVPTriple : class, IMVPTriple;
}
```

#### IMVPTriple
```csharp
/// <summary>
/// MVP三元组接口
/// 封装Model、View、Presenter的组合
/// </summary>
public interface IMVPTriple : IDisposable
{
    /// <summary>
    /// Model实例
    /// </summary>
    IModel Model { get; }
    
    /// <summary>
    /// View实例
    /// </summary>
    IView View { get; }
    
    /// <summary>
    /// Presenter实例
    /// </summary>
    IPresenter Presenter { get; }
    
    /// <summary>
    /// 是否已激活
    /// </summary>
    bool IsActive { get; }
    
    /// <summary>
    /// 显示MVP
    /// </summary>
    UniTask ShowAsync();
    
    /// <summary>
    /// 隐藏MVP
    /// </summary>
    UniTask HideAsync();
}
```

### 3. 基础实现类

#### BaseModel
```csharp
/// <summary>
/// Model基类
/// 提供通用的数据管理功能
/// </summary>
public abstract class BaseModel : IModel
{
    protected IXLogger Logger { get; private set; }
    
    public event System.Action<IModel> OnDataChanged;
    
    [Inject]
    public void Construct(IXLogger logger)
    {
        Logger = logger;
    }
    
    public virtual async UniTask InitializeAsync()
    {
        Logger.Info($"{GetType().Name} initialized");
    }
    
    protected void NotifyDataChanged()
    {
        OnDataChanged?.Invoke(this);
    }
    
    public virtual void Dispose()
    {
        OnDataChanged = null;
        Logger.Info($"{GetType().Name} disposed");
    }
}
```

#### BaseView
```csharp
/// <summary>
/// View基类
/// 提供通用的UI管理功能
/// </summary>
public abstract class BaseView : MonoBehaviour, IView
{
    protected IPresenter Presenter { get; private set; }
    protected IXLogger Logger { get; private set; }
    
    public bool IsActive => gameObject.activeInHierarchy;
    
    [Inject]
    public void Construct(IXLogger logger)
    {
        Logger = logger;
    }
    
    public virtual async UniTask ShowAsync()
    {
        gameObject.SetActive(true);
        await OnShowAsync();
        Logger.Info($"{GetType().Name} shown");
    }
    
    public virtual async UniTask HideAsync()
    {
        await OnHideAsync();
        gameObject.SetActive(false);
        Logger.Info($"{GetType().Name} hidden");
    }
    
    public void BindPresenter(IPresenter presenter)
    {
        Presenter = presenter;
        OnPresenterBound();
    }
    
    public void UnbindPresenter()
    {
        OnPresenterUnbound();
        Presenter = null;
    }
    
    protected abstract UniTask OnShowAsync();
    protected abstract UniTask OnHideAsync();
    protected abstract void OnPresenterBound();
    protected abstract void OnPresenterUnbound();
    
    public virtual void Dispose()
    {
        UnbindPresenter();
        Logger.Info($"{GetType().Name} disposed");
    }
}
```

#### BasePresenter
```csharp
/// <summary>
/// Presenter基类
/// 提供通用的逻辑协调功能
/// </summary>
public abstract class BasePresenter : IPresenter
{
    protected IView View { get; private set; }
    protected IModel Model { get; private set; }
    protected IXLogger Logger { get; private set; }
    
    public bool IsActive { get; private set; }
    
    [Inject]
    public void Construct(IXLogger logger)
    {
        Logger = logger;
    }
    
    public virtual async UniTask InitializeAsync()
    {
        Logger.Info($"{GetType().Name} initialized");
    }
    
    public virtual async UniTask BindAsync(IView view, IModel model)
    {
        View = view;
        Model = model;
        
        View.BindPresenter(this);
        Model.OnDataChanged += OnModelDataChanged;
        
        await OnBindAsync();
        IsActive = true;
        
        Logger.Info($"{GetType().Name} bound to View and Model");
    }
    
    public virtual async UniTask UnbindAsync()
    {
        IsActive = false;
        
        await OnUnbindAsync();
        
        if (Model != null)
        {
            Model.OnDataChanged -= OnModelDataChanged;
        }
        
        View?.UnbindPresenter();
        
        View = null;
        Model = null;
        
        Logger.Info($"{GetType().Name} unbound from View and Model");
    }
    
    public virtual async UniTask ShowAsync()
    {
        if (View != null)
        {
            await View.ShowAsync();
            await OnShowAsync();
        }
    }
    
    public virtual async UniTask HideAsync()
    {
        if (View != null)
        {
            await OnHideAsync();
            await View.HideAsync();
        }
    }
    
    protected abstract UniTask OnBindAsync();
    protected abstract UniTask OnUnbindAsync();
    protected abstract UniTask OnShowAsync();
    protected abstract UniTask OnHideAsync();
    protected abstract void OnModelDataChanged(IModel model);
    
    public virtual void Dispose()
    {
        if (IsActive)
        {
            UnbindAsync().Forget();
        }
        Logger.Info($"{GetType().Name} disposed");
    }
}
```

---

## 集成方案

### 1. 与VContainer集成

#### MVP模块注册
```csharp
/// <summary>
/// MVP模块的VContainer注册扩展
/// </summary>
public static class MVPContainerExtensions
{
    public static void RegisterMVPModule(this IContainerBuilder builder)
    {
        // 注册MVP管理器
        builder.Register<IMVPManager, MVPManager>(Lifetime.Singleton);
        
        // 注册MVP工厂
        builder.Register<IMVPFactory, MVPFactory>(Lifetime.Singleton);
        
        // 注册MVP三元组池
        builder.Register<IMVPTriplePool, MVPTriplePool>(Lifetime.Singleton);
    }
    
    public static void RegisterMVP<TModel, TView, TPresenter, TMVPTriple>(
        this IContainerBuilder builder)
        where TModel : class, IModel
        where TView : class, IView
        where TPresenter : class, IPresenter
        where TMVPTriple : class, IMVPTriple
    {
        builder.Register<TModel>(Lifetime.Transient);
        builder.Register<TView>(Lifetime.Transient);
        builder.Register<TPresenter>(Lifetime.Transient);
        builder.Register<TMVPTriple>(Lifetime.Transient);
    }
}
```

### 2. 与EventBus集成

#### MVP事件定义
```csharp
/// <summary>
/// MVP相关事件的基类
/// </summary>
public abstract class MVPEvent : IEvent
{
    public string MVPId { get; }
    
    protected MVPEvent(string mvpId)
    {
        MVPId = mvpId;
    }
}

/// <summary>
/// MVP显示事件
/// </summary>
public class MVPShowEvent : MVPEvent
{
    public MVPShowEvent(string mvpId) : base(mvpId) { }
}

/// <summary>
/// MVP隐藏事件
/// </summary>
public class MVPHideEvent : MVPEvent
{
    public MVPHideEvent(string mvpId) : base(mvpId) { }
}

/// <summary>
/// MVP数据变更事件
/// </summary>
public class MVPDataChangedEvent : MVPEvent
{
    public object Data { get; }
    
    public MVPDataChangedEvent(string mvpId, object data) : base(mvpId)
    {
        Data = data;
    }
}
```

### 3. 与资源管理器集成

#### View资源加载
```csharp
/// <summary>
/// MVP View工厂
/// 负责View的创建和资源管理
/// </summary>
public interface IMVPViewFactory
{
    /// <summary>
    /// 创建View实例
    /// </summary>
    UniTask<TView> CreateViewAsync<TView>(string assetKey) 
        where TView : class, IView;
    
    /// <summary>
    /// 销毁View实例
    /// </summary>
    UniTask DestroyViewAsync<TView>(TView view) 
        where TView : class, IView;
}
```

### 4. 与对象池集成

#### MVP三元组池化
```csharp
/// <summary>
/// MVP三元组对象池
/// </summary>
public interface IMVPTriplePool
{
    /// <summary>
    /// 从池中获取MVP三元组
    /// </summary>
    TMVPTriple Get<TMVPTriple>() where TMVPTriple : class, IMVPTriple;
    
    /// <summary>
    /// 将MVP三元组返回池中
    /// </summary>
    void Return<TMVPTriple>(TMVPTriple mvpTriple) where TMVPTriple : class, IMVPTriple;
}
```

---

## 使用指南

### 1. 定义MVP组件

#### 定义Model
```csharp
public interface IUserModel : IModel
{
    string UserName { get; set; }
    int Level { get; set; }
    int Experience { get; set; }
}

public class UserModel : BaseModel, IUserModel
{
    public string UserName { get; set; }
    public int Level { get; set; }
    public int Experience { get; set; }
    
    public override async UniTask InitializeAsync()
    {
        await base.InitializeAsync();
        // 加载用户数据
        await LoadUserDataAsync();
    }
    
    private async UniTask LoadUserDataAsync()
    {
        // 从持久化系统加载数据
        // ...
        NotifyDataChanged();
    }
}
```

#### 定义View
```csharp
public interface IUserView : IView
{
    void UpdateUserInfo(string userName, int level, int experience);
    event System.Action OnLevelUpButtonClicked;
}

public class UserView : BaseView, IUserView
{
    [SerializeField] private Text userNameText;
    [SerializeField] private Text levelText;
    [SerializeField] private Text experienceText;
    [SerializeField] private Button levelUpButton;
    
    public event System.Action OnLevelUpButtonClicked;
    
    protected override void OnPresenterBound()
    {
        levelUpButton.onClick.AddListener(() => OnLevelUpButtonClicked?.Invoke());
    }
    
    protected override void OnPresenterUnbound()
    {
        levelUpButton.onClick.RemoveAllListeners();
        OnLevelUpButtonClicked = null;
    }
    
    public void UpdateUserInfo(string userName, int level, int experience)
    {
        userNameText.text = userName;
        levelText.text = $"Level: {level}";
        experienceText.text = $"EXP: {experience}";
    }
    
    protected override async UniTask OnShowAsync()
    {
        // 显示动画
        await transform.DOScale(1f, 0.3f);
    }
    
    protected override async UniTask OnHideAsync()
    {
        // 隐藏动画
        await transform.DOScale(0f, 0.3f);
    }
}
```

#### 定义Presenter
```csharp
public class UserPresenter : BasePresenter
{
    private IUserModel userModel;
    private IUserView userView;
    
    protected override async UniTask OnBindAsync()
    {
        userModel = Model as IUserModel;
        userView = View as IUserView;
        
        // 订阅View事件
        userView.OnLevelUpButtonClicked += OnLevelUpButtonClicked;
        
        // 初始化显示
        UpdateView();
    }
    
    protected override async UniTask OnUnbindAsync()
    {
        if (userView != null)
        {
            userView.OnLevelUpButtonClicked -= OnLevelUpButtonClicked;
        }
    }
    
    protected override void OnModelDataChanged(IModel model)
    {
        UpdateView();
    }
    
    private void UpdateView()
    {
        if (userModel != null && userView != null)
        {
            userView.UpdateUserInfo(
                userModel.UserName, 
                userModel.Level, 
                userModel.Experience);
        }
    }
    
    private void OnLevelUpButtonClicked()
    {
        if (userModel != null)
        {
            userModel.Level++;
            userModel.Experience = 0;
            
            // 发送升级事件
            xFrameEventBus.Raise(new UserLevelUpEvent(userModel.Level));
        }
    }
    
    protected override async UniTask OnShowAsync()
    {
        Logger.Info("User panel shown");
    }
    
    protected override async UniTask OnHideAsync()
    {
        Logger.Info("User panel hidden");
    }
}
```

### 2. 注册MVP组件

```csharp
// 在xFrameLifetimeScope中注册
protected override void Configure(IContainerBuilder builder)
{
    // ... 其他注册
    
    // 注册MVP模块
    builder.RegisterMVPModule();
    
    // 注册具体的MVP组件
    builder.RegisterMVP<UserModel, UserView, UserPresenter, UserMVPTriple>();
}
```

### 3. 使用MVP

```csharp
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

---

## 最佳实践

### 1. 职责分离
- **Model** 只处理数据和业务逻辑，不依赖Unity
- **View** 只处理UI显示，不包含业务逻辑
- **Presenter** 协调Model和View，可独立测试

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

### 6. 单元测试
- Presenter可以独立于Unity进行测试
- 使用Mock对象模拟View和Model
- 测试业务逻辑的正确性

---

## API参考

### 核心接口
- `IModel` - Model基础接口
- `IView` - View基础接口  
- `IPresenter` - Presenter基础接口
- `IMVPTriple` - MVP三元组接口
- `IMVPManager` - MVP管理器接口

### 基础实现
- `BaseModel` - Model基类
- `BaseView` - View基类
- `BasePresenter` - Presenter基类
- `MVPTriple<TModel, TView, TPresenter>` - MVP三元组实现
- `MVPManager` - MVP管理器实现

### 扩展功能
- `MVPContainerExtensions` - VContainer集成扩展
- `MVPEvent` - MVP事件基类
- `IMVPViewFactory` - View工厂接口
- `IMVPTriplePool` - MVP对象池接口

### 事件类型
- `MVPShowEvent` - MVP显示事件
- `MVPHideEvent` - MVP隐藏事件
- `MVPDataChangedEvent` - MVP数据变更事件

---

## 总结

xFrame MVP框架提供了一个完整的Model-View-Presenter架构实现，充分集成了xFrame框架的核心功能。通过清晰的职责分离、强大的依赖注入、灵活的事件通信和高效的资源管理，为Unity项目提供了一个可维护、可测试、可扩展的架构基础。

框架的设计遵循SOLID原则，支持异步操作，提供了丰富的扩展点，可以满足各种复杂的业务需求。通过合理使用MVP模式，可以显著提高代码质量和开发效率。
