# xFrame MVP框架

基于xFrame框架核心功能实现的Model-View-Presenter架构模式。

## 特性

- ✅ 清晰的职责分离（Model、View、Presenter）
- ✅ 完整的VContainer依赖注入支持
- ✅ EventBus事件驱动通信
- ✅ 异步优先（基于UniTask）
- ✅ 资源管理和对象池集成
- ✅ 完善的日志系统
- ✅ 可独立测试的Presenter
- ✅ 丰富的示例代码

## 目录结构

```
MVP/
├── Core/                   # 核心接口和实现
│   ├── IModel.cs          # Model接口
│   ├── IView.cs           # View接口
│   ├── IPresenter.cs      # Presenter接口
│   ├── IMVPTriple.cs      # MVP三元组接口
│   ├── IMVPManager.cs     # MVP管理器接口
│   ├── BaseModel.cs       # Model基类
│   ├── BaseView.cs        # View基类
│   ├── BasePresenter.cs   # Presenter基类
│   ├── MVPTriple.cs       # MVP三元组实现
│   ├── MVPManager.cs      # MVP管理器实现
│   ├── IMVPViewFactory.cs # View工厂接口
│   ├── IMVPTriplePool.cs  # MVP对象池接口
│   ├── MVPViewFactory.cs  # View工厂实现
│   └── MVPTriplePool.cs   # MVP对象池实现
├── Events/                 # MVP事件
│   ├── MVPEvent.cs        # MVP事件基类
│   ├── MVPShowEvent.cs    # MVP显示事件
│   ├── MVPHideEvent.cs    # MVP隐藏事件
│   └── MVPDataChangedEvent.cs # MVP数据变更事件
├── Extensions/             # 扩展功能
│   └── MVPContainerExtensions.cs # VContainer集成扩展
├── Examples/               # 示例代码
│   ├── UserModel.cs       # 用户Model示例
│   ├── UserView.cs        # 用户View示例
│   ├── UserPresenter.cs   # 用户Presenter示例
│   ├── UserMVPTriple.cs   # 用户MVP三元组
│   └── UserMVPExample.cs  # 使用示例
└── README.md              # 本文件
```

## 快速开始

### 1. 注册MVP模块

```csharp
using VContainer;
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

### 2. 定义MVP组件

```csharp
// Model
public class UserModel : BaseModel, IUserModel
{
    public string UserName { get; set; }
    
    public override async UniTask InitializeAsync()
    {
        await base.InitializeAsync();
        UserName = "Player";
        NotifyDataChanged();
    }
}

// View
public class UserView : BaseView, IUserView
{
    [SerializeField] private Text userNameText;
    
    public void UpdateUserInfo(string userName)
    {
        userNameText.text = userName;
    }
    
    protected override async UniTask OnShowAsync() { }
    protected override async UniTask OnHideAsync() { }
    protected override void OnPresenterBound() { }
    protected override void OnPresenterUnbound() { }
}

// Presenter
public class UserPresenter : BasePresenter
{
    private IUserModel userModel;
    private IUserView userView;
    
    protected override async UniTask OnBindAsync()
    {
        userModel = Model as IUserModel;
        userView = View as IUserView;
        UpdateView();
    }
    
    protected override void OnModelDataChanged(IModel model)
    {
        UpdateView();
    }
    
    private void UpdateView()
    {
        userView?.UpdateUserInfo(userModel?.UserName);
    }
    
    protected override async UniTask OnUnbindAsync() { }
    protected override async UniTask OnShowAsync() { }
    protected override async UniTask OnHideAsync() { }
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
        userMVP = await mvpManager.CreateMVPAsync<UserMVPTriple, UserModel, UserView, UserPresenter>();
        await userMVP.ShowAsync();
    }
    
    private async void OnDestroy()
    {
        if (userMVP != null)
        {
            await mvpManager.DestroyMVPAsync(userMVP);
        }
    }
}
```

## 核心接口

### IModel
- `InitializeAsync()` - 初始化Model
- `OnDataChanged` - 数据变更事件

### IView
- `IsActive` - View是否激活
- `ShowAsync()` - 显示View
- `HideAsync()` - 隐藏View
- `BindPresenter()` - 绑定Presenter
- `UnbindPresenter()` - 解绑Presenter

### IPresenter
- `IsActive` - Presenter是否激活
- `InitializeAsync()` - 初始化Presenter
- `BindAsync()` - 绑定View和Model
- `UnbindAsync()` - 解绑View和Model
- `ShowAsync()` - 显示
- `HideAsync()` - 隐藏

### IMVPManager
- `CreateMVPAsync()` - 创建MVP三元组
- `DestroyMVPAsync()` - 销毁MVP三元组
- `GetActiveMVP()` - 获取活跃的MVP三元组

## 集成功能

### VContainer依赖注入
通过`MVPContainerExtensions`提供便捷的注册方法。

### EventBus事件系统
提供`MVPEvent`、`MVPShowEvent`、`MVPHideEvent`、`MVPDataChangedEvent`等事件。

### 资源管理
通过`IMVPViewFactory`管理View的加载和销毁。

### 对象池
通过`IMVPTriplePool`实现MVP三元组的复用。

## 文档

详细文档请参考：
- [MVP框架设计文档](../../Docs/MVP框架设计文档.md)
- [MVP框架使用指南](../../Docs/MVP/MVP框架使用指南.md)

## 测试

单元测试位于：`Assets/xFrame/Tests/EditMode/MVPTests/`

## 示例

完整示例代码位于：`Assets/xFrame/Runtime/MVP/Examples/`

## 依赖

- VContainer - 依赖注入
- UniTask - 异步操作
- xFrame.Logging - 日志系统
- xFrame.EventBus - 事件总线

## 许可

MIT License
